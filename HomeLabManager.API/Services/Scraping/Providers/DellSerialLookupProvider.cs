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

            var manualLookupUrl = "https://www.dell.com/support/home/en-us";

            // Try both service-tag page variants because Dell frequently changes route behavior.
            var candidateUrls = new[]
            {
                BuildLookupUrl(lookupUrl, query),
                BuildLookupUrl("https://www.dell.com/support/home/en-us/product-support/servicetag/{serial}", query)
            }.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            var lastFailure = new ScrapeResult
            {
                Success = false,
                Message = "Dell lookup did not return a parseable result.",
                LookupStatus = "not_found",
                SuggestedLookupUrl = manualLookupUrl
            };

            foreach (var candidateUrl in candidateUrls)
            {
                var request = CreateDellRequest(candidateUrl);
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var statusCode = (int)response.StatusCode;
                    lastFailure = new ScrapeResult
                    {
                        Success = false,
                        Message = response.StatusCode == HttpStatusCode.Forbidden
                            ? "Dell blocked direct service-tag scraping. Open Dell Support and enter the service tag manually."
                            : $"Dell lookup request failed with status code {statusCode}.",
                        LookupStatus = response.StatusCode == HttpStatusCode.Forbidden ? "manual_lookup_required" : "failed_http",
                        SuggestedLookupUrl = manualLookupUrl
                    };

                    continue;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseBody))
                {
                    lastFailure = new ScrapeResult
                    {
                        Success = false,
                        Message = "Dell lookup returned an empty response.",
                        LookupStatus = "empty",
                        SuggestedLookupUrl = manualLookupUrl
                    };

                    continue;
                }

                if (ContainsBlockedMarker(responseBody))
                {
                    lastFailure = new ScrapeResult
                    {
                        Success = false,
                        Message = "Dell blocked automated lookup from this endpoint. Open Dell Support and enter the service tag manually.",
                        LookupStatus = "manual_lookup_required",
                        SuggestedLookupUrl = manualLookupUrl
                    };

                    continue;
                }

                var parsedResult = TryParseDellResponse(responseBody, query, candidateUrl, response.RequestMessage?.RequestUri?.ToString());
                if (parsedResult.Success)
                {
                    return parsedResult;
                }

                lastFailure = parsedResult;
            }

            // Final fallback for Dell-specific flow.
            return new ScrapeResult
            {
                Success = false,
                Message = "Dell did not return parseable product details. Open Dell Support and enter the service tag manually.",
                LookupStatus = "manual_lookup_required",
                SuggestedLookupUrl = manualLookupUrl
            };
        }

        private static HttpRequestMessage CreateDellRequest(string requestUrl)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xhtml+xml"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml", 0.9));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/avif"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/webp"));
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36");
            request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
            request.Headers.Referrer = new Uri("https://www.dell.com/support/home/en-us");
            return request;
        }

        private static bool ContainsBlockedMarker(string responseBody)
        {
            return responseBody.Contains("Access Denied", StringComparison.OrdinalIgnoreCase)
                || responseBody.Contains("errors.edgesuite.net", StringComparison.OrdinalIgnoreCase)
                || responseBody.Contains("Request blocked", StringComparison.OrdinalIgnoreCase)
                || responseBody.Contains("akamai", StringComparison.OrdinalIgnoreCase);
        }

        private static ScrapeResult TryParseDellResponse(string responseBody, string serial, string requestedUrl, string? responseUrl)
        {
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
                    LookupStatus = "success",
                    SuggestedLookupUrl = string.IsNullOrWhiteSpace(canonicalUrl) ? requestedUrl : canonicalUrl,
                    DeviceInfo = new ScrapedDeviceInfo
                    {
                        ProductName = title,
                        Manufacturer = string.IsNullOrWhiteSpace(manufacturer) ? "Dell" : manufacturer,
                        ModelNumber = string.IsNullOrWhiteSpace(modelNumber) ? serial : modelNumber,
                        SerialNumber = serial,
                        Description = description,
                        ImageUrl = imageUrl,
                        SourceUrl = string.IsNullOrWhiteSpace(canonicalUrl) ? responseUrl ?? requestedUrl : canonicalUrl,
                        SourceType = ScrapeSourceType.VendorWebsite
                    }
                };
            }

            // If JSON-LD is missing but a specific Dell page title exists, keep a best-effort result.
            if (!string.IsNullOrWhiteSpace(title)
                && !title.Contains("support home", StringComparison.OrdinalIgnoreCase)
                && !title.Contains("access denied", StringComparison.OrdinalIgnoreCase))
            {
                return new ScrapeResult
                {
                    Success = true,
                    Message = "Dell support page returned a best-effort match.",
                    LookupStatus = "partial_success",
                    SuggestedLookupUrl = string.IsNullOrWhiteSpace(canonicalUrl) ? requestedUrl : canonicalUrl,
                    DeviceInfo = new ScrapedDeviceInfo
                    {
                        ProductName = title,
                        Manufacturer = "Dell",
                        ModelNumber = serial,
                        SerialNumber = serial,
                        Description = description,
                        SourceUrl = string.IsNullOrWhiteSpace(canonicalUrl) ? responseUrl ?? requestedUrl : canonicalUrl,
                        SourceType = ScrapeSourceType.VendorWebsite
                    }
                };
            }

            return new ScrapeResult
            {
                Success = false,
                Message = "Dell support page did not contain recognized product information.",
                LookupStatus = "not_found",
                SuggestedLookupUrl = requestedUrl
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