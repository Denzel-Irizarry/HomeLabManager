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
    public class DellSerialLookupProvider : IHardwareLookupProvider
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public DellSerialLookupProvider(IConfiguration configuration, HttpClient httpClient)
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

            return string.IsNullOrWhiteSpace(vendor)
                || string.Equals(vendor, "Dell", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<ScrapeResult> SearchAsync(string query, string? vendor = null)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new ScrapeResult
                {
                    Success = false,
                    Message = "Serial number cannot be empty."
                };
            }

            var isEnabled = _configuration.GetValue<bool?>("DellSupport:Enabled") ?? false;
            if (!isEnabled)
            {
                return new ScrapeResult
                {
                    Success = false,
                    Message = "Dell serial lookup is not enabled in configuration."
                };
            }

            var lookupUrl = _configuration["DellSupport:LookupUrl"];
            if (string.IsNullOrWhiteSpace(lookupUrl))
            {
                lookupUrl = "https://www.dell.com/support/home/en-us/product-support/servicetag/{serial}/overview";
            }

            var requestUrl = BuildLookupUrl(lookupUrl, query);
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xhtml+xml"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml", 0.9));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/avif"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/webp"));
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36");
            request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
            request.Headers.Referrer = new Uri("https://www.dell.com/support/home/en-us");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return new ScrapeResult
                {
                    Success = false,
                    Message = $"Dell lookup request failed with status code {(int)response.StatusCode}."
                };
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return new ScrapeResult
                {
                    Success = false,
                    Message = "Dell lookup returned an empty response."
                };
            }

            var html = WebUtility.HtmlDecode(responseBody);
            var title = ExtractMetaContent(html, "property", "og:title");
            var description = ExtractMetaContent(html, "name", "description");
            var canonicalUrl = ExtractLinkHref(html, "canonical");
            var pageTitle = ExtractTitle(html);

            if (string.IsNullOrWhiteSpace(title))
            {
                title = pageTitle;
            }

            var structuredData = ExtractJsonLdDocuments(html);
            foreach (var document in structuredData)
            {
                var root = document.RootElement;

                title = FirstNonEmpty(title, ReadString(root, "productName", "ProductName", "name", "Name", "title", "Title"));
                description = FirstNonEmpty(description, ReadString(root, "description", "Description"));
                canonicalUrl = FirstNonEmpty(canonicalUrl, ReadString(root, "url", "Url", "sourceUrl", "SourceUrl"));

                var manufacturer = ReadString(root, "manufacturer", "Manufacturer", "brand", "Brand", "vendor", "Vendor");
                var modelNumber = ReadString(root, "modelNumber", "ModelNumber", "model", "Model", "sku", "Sku");
                var imageUrl = ReadString(root, "imageUrl", "ImageUrl", "image", "Image", "thumbnailUrl", "ThumbnailUrl");

                if (string.IsNullOrWhiteSpace(title)
                    && string.IsNullOrWhiteSpace(manufacturer)
                    && string.IsNullOrWhiteSpace(modelNumber)
                    && string.IsNullOrWhiteSpace(description))
                {
                    continue;
                }

                return new ScrapeResult
                {
                    Success = true,
                    Message = "Dell support page parsed successfully.",
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

            if (responseBody.Contains("Access Denied", StringComparison.OrdinalIgnoreCase)
                || responseBody.Contains("403", StringComparison.OrdinalIgnoreCase))
            {
                return new ScrapeResult
                {
                    Success = false,
                    Message = "Dell support page blocked the request."
                };
            }

            return new ScrapeResult
            {
                Success = false,
                Message = "Dell support page did not contain recognized product information."
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