using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using HomeLabManager.API.Services.Scraping.Interfaces;
using HomeLabManager.Core.Scraping.DTOs;
using HomeLabManager.Core.Scraping.Enums;
using HomeLabManager.Core.Scraping.Models;
using Microsoft.Extensions.Configuration;

namespace HomeLabManager.API.Services.Scraping.Providers
{
    public class HpeSerialLookupProvider : IHardwareLookupProvider
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public HpeSerialLookupProvider(IConfiguration configuration, HttpClient httpClient)
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
                || string.Equals(vendor, "HPE", StringComparison.OrdinalIgnoreCase)
                || string.Equals(vendor, "HP", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<ScrapeResult> SearchAsync(string query, string? vendor = null)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new ScrapeResult
                {
                    Success = false,
                    Message = "Serial number cannot be empty.",
                    LookupStatus = "failed_validation",
                    DetectedVendor = "HPE"
                };
            }

            var isEnabled = _configuration.GetValue<bool?>("HpeSupport:Enabled") ?? false;
            if (!isEnabled)
            {
                return new ScrapeResult
                {
                    Success = false,
                    Message = "HPE serial lookup is not enabled in configuration.",
                    LookupStatus = "not_enabled",
                    DetectedVendor = "HPE"
                };
            }

            var lookupUrl = _configuration["HpeSupport:LookupUrl"];
            if (string.IsNullOrWhiteSpace(lookupUrl))
            {
                lookupUrl = "https://partsurfer.hpe.com/Search.aspx?type=SERIAL&SearchText={serial}";
            }

            var requestUrl = BuildLookupUrl(lookupUrl, query);
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xhtml+xml"));
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36");
            request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
            request.Headers.Referrer = new Uri("https://partsurfer.hpe.com/Search.aspx");

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var statusCode = (int)response.StatusCode;
                    return new ScrapeResult
                    {
                        Success = false,
                        Message = $"HPE PartSurfer request failed with status code {statusCode}.",
                        LookupStatus = response.StatusCode switch
                        {
                            HttpStatusCode.Unauthorized => "auth_required",
                            HttpStatusCode.Forbidden => "auth_required",
                            HttpStatusCode.TooManyRequests => "rate_limited",
                            HttpStatusCode.NotFound => "not_found",
                            _ => "failed_http"
                        },
                        DetectedVendor = "HPE",
                        SuggestedLookupUrl = requestUrl
                    };
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseBody))
                {
                    return new ScrapeResult
                    {
                        Success = false,
                        Message = "HPE PartSurfer returned an empty response.",
                        LookupStatus = "empty",
                        DetectedVendor = "HPE",
                        SuggestedLookupUrl = requestUrl
                    };
                }

                var html = WebUtility.HtmlDecode(responseBody);

                if (ContainsNoDataMarker(html))
                {
                    return new ScrapeResult
                    {
                        Success = false,
                        Message = "HPE PartSurfer did not find a matching part or serial number.",
                        LookupStatus = "not_found",
                        DetectedVendor = "HPE",
                        SuggestedLookupUrl = requestUrl
                    };
                }

                if (ContainsCountrySelectionPrompt(html))
                {
                    return new ScrapeResult
                    {
                        Success = false,
                        Message = "HPE PartSurfer needs a country or region selection before it can continue.",
                        LookupStatus = "manual_lookup_required",
                        DetectedVendor = "HPE",
                        SuggestedLookupUrl = requestUrl
                    };
                }

                var title = ExtractTitle(html);
                var description = ExtractMetaContent(html, "name", "description");
                var canonicalUrl = ExtractLinkHref(html, "canonical");

                var productName = FirstNonEmpty(
                    ExtractLabeledValue(html, "Product Name"),
                    ExtractLabeledValue(html, "Product"),
                    title);

                var modelNumber = FirstNonEmpty(
                    ExtractLabeledValue(html, "Product Number"),
                    ExtractLabeledValue(html, "Spare Part Number"),
                    ExtractLabeledValue(html, "Part Number"),
                    ExtractLabeledValue(html, "Model Number"));

                var serialNumber = FirstNonEmpty(
                    ExtractLabeledValue(html, "Serial Number"),
                    query);

                var category = FirstNonEmpty(
                    ExtractLabeledValue(html, "Category"),
                    ExtractLabeledValue(html, "Product Category"));

                var imageUrl = ExtractLabeledValue(html, "Image") ?? string.Empty;

                if (string.IsNullOrWhiteSpace(productName)
                    && string.IsNullOrWhiteSpace(modelNumber)
                    && string.IsNullOrWhiteSpace(description))
                {
                    return new ScrapeResult
                    {
                        Success = false,
                        Message = "HPE PartSurfer returned a page but no specific product details were extracted.",
                        LookupStatus = "manual_lookup_required",
                        DetectedVendor = "HPE",
                        SuggestedLookupUrl = requestUrl
                    };
                }

                return new ScrapeResult
                {
                    Success = true,
                    Message = "HPE PartSurfer parsed successfully.",
                    LookupStatus = "success",
                    DetectedVendor = "HPE",
                    SuggestedLookupUrl = string.IsNullOrWhiteSpace(canonicalUrl) ? requestUrl : canonicalUrl,
                    DeviceInfo = new ScrapedDeviceInfo
                    {
                        ProductName = productName,
                        Manufacturer = "HPE",
                        ModelNumber = modelNumber,
                        SerialNumber = serialNumber,
                        Category = category,
                        Description = description,
                        ImageUrl = imageUrl,
                        SourceUrl = string.IsNullOrWhiteSpace(canonicalUrl) ? requestUrl : canonicalUrl,
                        SourceType = ScrapeSourceType.VendorWebsite
                    }
                };
            }
            catch (HttpRequestException ex)
            {
                return new ScrapeResult
                {
                    Success = false,
                    Message = $"HPE PartSurfer endpoint unreachable: {ex.Message}",
                    LookupStatus = "connection_error",
                    DetectedVendor = "HPE",
                    SuggestedLookupUrl = requestUrl
                };
            }
            catch (OperationCanceledException)
            {
                return new ScrapeResult
                {
                    Success = false,
                    Message = "HPE PartSurfer request timed out.",
                    LookupStatus = "timeout",
                    DetectedVendor = "HPE",
                    SuggestedLookupUrl = requestUrl
                };
            }
            catch (Exception ex)
            {
                return new ScrapeResult
                {
                    Success = false,
                    Message = $"HPE PartSurfer lookup failed with error: {ex.Message}",
                    LookupStatus = "error",
                    DetectedVendor = "HPE",
                    SuggestedLookupUrl = requestUrl
                };
            }
        }

        private static string BuildLookupUrl(string lookupUrl, string serial)
        {
            if (lookupUrl.Contains("{serial}", StringComparison.OrdinalIgnoreCase))
            {
                return lookupUrl.Replace("{serial}", Uri.EscapeDataString(serial), StringComparison.OrdinalIgnoreCase);
            }

            var separator = lookupUrl.Contains('?') ? "&" : "?";
            return $"{lookupUrl}{separator}SearchText={Uri.EscapeDataString(serial)}";
        }

        private static bool ContainsNoDataMarker(string html)
        {
            return html.Contains("lblNoDataFound", StringComparison.OrdinalIgnoreCase)
                || html.Contains("not found in PartSurfer", StringComparison.OrdinalIgnoreCase)
                || html.Contains("no data found", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ContainsCountrySelectionPrompt(string html)
        {
            return html.Contains("must first select your country or region", StringComparison.OrdinalIgnoreCase)
                || html.Contains("select your country or region before accessing data", StringComparison.OrdinalIgnoreCase);
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

        private static string? ExtractLabeledValue(string html, string label)
        {
            var patterns = new[]
            {
                $"<td[^>]*>\\s*{Regex.Escape(label)}\\s*</td>\\s*<td[^>]*>(?<value>.*?)</td>",
                $"<th[^>]*>\\s*{Regex.Escape(label)}\\s*</th>\\s*<td[^>]*>(?<value>.*?)</td>",
                $"{Regex.Escape(label)}\\s*</[^>]+>\\s*<[^>]+>(?<value>.*?)</",
                $"{Regex.Escape(label)}\\s*[:：]\\s*(?<value>[^<\\r\\n]+)"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (match.Success)
                {
                    return WebUtility.HtmlDecode(match.Groups["value"].Value).Trim();
                }
            }

            return null;
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
        }
    }
}