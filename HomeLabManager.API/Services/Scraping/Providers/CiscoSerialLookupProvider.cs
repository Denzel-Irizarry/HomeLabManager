using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using HomeLabManager.API.Services.Scraping.Interfaces;
using HomeLabManager.Core.Scraping.DTOs;
using HomeLabManager.Core.Scraping.Enums;
using HomeLabManager.Core.Scraping.Models;
using Microsoft.Extensions.Configuration;

namespace HomeLabManager.API.Services.Scraping.Providers
{
    public class CiscoSerialLookupProvider : IHardwareLookupProvider
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public CiscoSerialLookupProvider(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public bool CanHandle(string codeType, string? vendor = null)
        {
            if (!string.Equals(codeType, "SerialNumber", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return string.Equals(vendor, "Cisco", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<ScrapeResult> SearchAsync(string query, string? vendor = null)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new ScrapeResult
                {
                    Success = false,
                    Message = "Serial number cannot be empty.",
                    LookupStatus = "failed_validation"
                };
            }

            var isEnabled = _configuration.GetValue<bool?>("CiscoSupport:Enabled") ?? false;
            if (!isEnabled)
            {
                return new ScrapeResult
                {
                    Success = false,
                    Message = "Cisco serial lookup is not enabled in configuration.",
                    LookupStatus = "not_enabled"
                };
            }

            var lookupUrl = _configuration["CiscoSupport:LookupUrl"];
            if (string.IsNullOrWhiteSpace(lookupUrl))
            {
                lookupUrl = "https://sn2info.cisco.com/serial/{serial}";
            }

            var requestUrl = BuildLookupUrl(lookupUrl, query);
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xhtml+xml"));
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36");
            request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
            request.Headers.Referrer = new Uri("https://www.cisco.com/");

            var apiKeyHeader = _configuration["CiscoSupport:ApiKeyHeader"];
            var apiKey = _configuration["CiscoSupport:ApiKey"];
            if (!string.IsNullOrWhiteSpace(apiKeyHeader) && !string.IsNullOrWhiteSpace(apiKey))
            {
                request.Headers.TryAddWithoutValidation(apiKeyHeader, apiKey);
            }

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var statusCode = (int)response.StatusCode;
                return new ScrapeResult
                {
                    Success = false,
                    Message = $"Cisco lookup request failed with status code {statusCode}.",
                    LookupStatus = response.StatusCode switch
                    {
                        HttpStatusCode.Unauthorized => "auth_required",
                        HttpStatusCode.Forbidden => "auth_required",
                        HttpStatusCode.TooManyRequests => "rate_limited",
                        HttpStatusCode.NotFound => "not_found",
                        _ => "failed_http"
                    },
                    SuggestedLookupUrl = requestUrl
                };
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return new ScrapeResult
                {
                    Success = false,
                    Message = "Cisco lookup returned an empty response.",
                    LookupStatus = "empty",
                    SuggestedLookupUrl = requestUrl
                };
            }

            var html = WebUtility.HtmlDecode(responseBody);
            var title = ExtractMetaContent(html, "property", "og:title");
            var description = ExtractMetaContent(html, "name", "description");
            var canonicalUrl = ExtractLinkHref(html, "canonical");

            if (string.IsNullOrWhiteSpace(title))
            {
                title = ExtractTitle(html);
            }

            var structuredData = ExtractJsonLdDocuments(html);
            foreach (var document in structuredData)
            {
                var root = document.RootElement;
                title = FirstNonEmpty(title, ReadString(root, "productName", "ProductName", "name", "Name", "title", "Title"));
                description = FirstNonEmpty(description, ReadString(root, "description", "Description"));
                canonicalUrl = FirstNonEmpty(canonicalUrl, ReadString(root, "url", "Url", "sourceUrl", "SourceUrl"));

                var manufacturer = FirstNonEmpty(
                    ReadString(root, "manufacturer", "Manufacturer", "brand", "Brand", "vendor", "Vendor"),
                    "Cisco");
                var modelNumber = ReadString(root, "modelNumber", "ModelNumber", "model", "Model", "sku", "Sku");
                var imageUrl = ReadString(root, "imageUrl", "ImageUrl", "image", "Image", "thumbnailUrl", "ThumbnailUrl");

                if (string.IsNullOrWhiteSpace(title)
                    && string.IsNullOrWhiteSpace(modelNumber)
                    && string.IsNullOrWhiteSpace(description))
                {
                    continue;
                }

                return new ScrapeResult
                {
                    Success = true,
                    Message = "Cisco serial lookup parsed successfully.",
                    LookupStatus = "success",
                    SuggestedLookupUrl = string.IsNullOrWhiteSpace(canonicalUrl) ? requestUrl : canonicalUrl,
                    DeviceInfo = new ScrapedDeviceInfo
                    {
                        ProductName = title,
                        Manufacturer = manufacturer,
                        ModelNumber = modelNumber,
                        SerialNumber = query,
                        Description = description,
                        ImageUrl = imageUrl,
                        SourceUrl = string.IsNullOrWhiteSpace(canonicalUrl) ? response.RequestMessage?.RequestUri?.ToString() ?? requestUrl : canonicalUrl,
                        SourceType = ScrapeSourceType.VendorWebsite
                    }
                };
            }

            if (responseBody.Contains("not found", StringComparison.OrdinalIgnoreCase)
                || responseBody.Contains("no records", StringComparison.OrdinalIgnoreCase)
                || responseBody.Contains("invalid serial", StringComparison.OrdinalIgnoreCase))
            {
                return new ScrapeResult
                {
                    Success = false,
                    Message = "Cisco lookup did not find a matching serial number.",
                    LookupStatus = "not_found",
                    SuggestedLookupUrl = requestUrl
                };
            }

            return new ScrapeResult
            {
                Success = false,
                Message = "Cisco lookup response did not contain recognized product information.",
                LookupStatus = "not_found",
                SuggestedLookupUrl = requestUrl
            };
        }

        private static string BuildLookupUrl(string lookupUrl, string serial)
        {
            if (lookupUrl.Contains("{serial}", StringComparison.OrdinalIgnoreCase))
            {
                return lookupUrl.Replace("{serial}", Uri.EscapeDataString(serial), StringComparison.OrdinalIgnoreCase);
            }

            var separator = lookupUrl.Contains('?') ? "&" : "?";
            return $"{lookupUrl}{separator}serial={Uri.EscapeDataString(serial)}";
        }

        private static string ExtractTitle(string html)
        {
            var match = Regex.Match(html, @"<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value).Trim() : string.Empty;
        }

        private static string ExtractMetaContent(string html, string attributeName, string attributeValue)
        {
            var pattern = $"<meta[^>]*{Regex.Escape(attributeName)}\\s*=\\s*[\"']{Regex.Escape(attributeValue)}[\"'][^>]*content\\s*=\\s*[\"'](?<content>.*?)[\"'][^>]*>|<meta[^>]*content\\s*=\\s*[\"'](?<content2>.*?)[\"'][^>]*{Regex.Escape(attributeName)}\\s*=\\s*[\"']{Regex.Escape(attributeValue)}[\"'][^>]*>";
            var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!match.Success)
            {
                return string.Empty;
            }

            var content = match.Groups["content"].Success ? match.Groups["content"].Value : match.Groups["content2"].Value;
            return WebUtility.HtmlDecode(content).Trim();
        }

        private static string ExtractLinkHref(string html, string relValue)
        {
            var pattern = $"<link[^>]*rel\\s*=\\s*[\"']{Regex.Escape(relValue)}[\"'][^>]*href\\s*=\\s*[\"'](?<href>.*?)[\"'][^>]*>|<link[^>]*href\\s*=\\s*[\"'](?<href2>.*?)[\"'][^>]*rel\\s*=\\s*[\"']{Regex.Escape(relValue)}[\"'][^>]*>";
            var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!match.Success)
            {
                return string.Empty;
            }

            var href = match.Groups["href"].Success ? match.Groups["href"].Value : match.Groups["href2"].Value;
            return WebUtility.HtmlDecode(href).Trim();
        }

        private static IEnumerable<JsonDocument> ExtractJsonLdDocuments(string html)
        {
            var documents = new List<JsonDocument>();
            var matches = Regex.Matches(html, "<script[^>]*type=[\"']application/ld\\+json[\"'][^>]*>(?<json>.*?)</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (Match match in matches)
            {
                var jsonText = WebUtility.HtmlDecode(match.Groups["json"].Value).Trim();
                if (string.IsNullOrWhiteSpace(jsonText))
                {
                    continue;
                }

                try
                {
                    documents.Add(JsonDocument.Parse(jsonText));
                }
                catch (JsonException)
                {
                    continue;
                }
            }

            return documents;
        }

        private static string FirstNonEmpty(params string[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
        }

        private static string ReadString(JsonElement root, params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                if (TryReadProperty(root, propertyName, out var value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private static bool TryReadProperty(JsonElement element, string propertyName, out string value)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in element.EnumerateObject())
                {
                    if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        value = property.Value.ValueKind == JsonValueKind.String
                            ? property.Value.GetString() ?? string.Empty
                            : property.Value.ToString();
                        return true;
                    }

                    if (TryReadProperty(property.Value, propertyName, out value))
                    {
                        return true;
                    }
                }
            }

            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var child in element.EnumerateArray())
                {
                    if (TryReadProperty(child, propertyName, out value))
                    {
                        return true;
                    }
                }
            }

            value = string.Empty;
            return false;
        }
    }
}