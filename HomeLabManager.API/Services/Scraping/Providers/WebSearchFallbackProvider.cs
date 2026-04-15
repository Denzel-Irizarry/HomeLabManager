using System.Net;
using System.Text.RegularExpressions;
using HomeLabManager.API.Services.Scraping.Interfaces;
using HomeLabManager.Core.Scraping.DTOs;
using HomeLabManager.Core.Scraping.Enums;
using HomeLabManager.Core.Scraping.Models;
using Microsoft.Extensions.Configuration;

namespace HomeLabManager.API.Services.Scraping.Providers
{
    // Last-resort provider that does a best-effort web search when vendor/UPC providers fail.
    public class WebSearchFallbackProvider : IHardwareLookupProvider
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public WebSearchFallbackProvider(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public bool CanHandle(string codeType, string? vendor = null)
        {
            return !string.IsNullOrWhiteSpace(codeType);
        }

        public async Task<ScrapeResult> SearchAsync(string query, string? vendor = null)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new ScrapeResult
                {
                    Success = false,
                    Message = "Search query cannot be empty.",
                    LookupStatus = "failed_validation",
                    DetectedVendor = vendor ?? string.Empty
                };
            }

            var isEnabled = _configuration.GetValue<bool?>("WebFallback:Enabled") ?? true;
            var searchTemplate = _configuration["WebFallback:SearchUrlTemplate"]
                ?? "https://duckduckgo.com/html/?q={query}";

            if (!isEnabled)
            {
                return new ScrapeResult
                {
                    Success = false,
                    Message = "Web fallback lookup is disabled in configuration.",
                    LookupStatus = "not_enabled",
                    DetectedVendor = vendor ?? string.Empty,
                    SuggestedLookupUrl = BuildSearchUrl(searchTemplate, query, vendor)
                };
            }

            var requestUrl = BuildSearchUrl(searchTemplate, query, vendor);
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
            request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return new ScrapeResult
                    {
                        Success = false,
                        Message = $"Web fallback request failed with status code {(int)response.StatusCode}.",
                        LookupStatus = "failed_http",
                        DetectedVendor = vendor ?? string.Empty,
                        SuggestedLookupUrl = requestUrl
                    };
                }

                var html = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(html))
                {
                    return new ScrapeResult
                    {
                        Success = false,
                        Message = "Web fallback returned an empty response.",
                        LookupStatus = "empty",
                        DetectedVendor = vendor ?? string.Empty,
                        SuggestedLookupUrl = requestUrl
                    };
                }

                var (title, description, sourceUrl) = ExtractTopResult(html);
                if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(description))
                {
                    return new ScrapeResult
                    {
                        Success = false,
                        Message = "No useful web search result could be extracted.",
                        LookupStatus = "manual_lookup_required",
                        DetectedVendor = vendor ?? string.Empty,
                        SuggestedLookupUrl = requestUrl
                    };
                }

                return new ScrapeResult
                {
                    Success = true,
                    Message = "Web fallback found a possible device match.",
                    LookupStatus = "web_fallback",
                    DetectedVendor = vendor ?? string.Empty,
                    SuggestedLookupUrl = string.IsNullOrWhiteSpace(sourceUrl) ? requestUrl : sourceUrl,
                    DeviceInfo = new ScrapedDeviceInfo
                    {
                        ProductName = title,
                        Manufacturer = vendor ?? string.Empty,
                        ModelNumber = query,
                        Description = description,
                        SourceUrl = string.IsNullOrWhiteSpace(sourceUrl) ? requestUrl : sourceUrl,
                        SourceType = ScrapeSourceType.RetailerWebsite
                    }
                };
            }
            catch (HttpRequestException ex)
            {
                return new ScrapeResult
                {
                    Success = false,
                    Message = $"Web fallback endpoint unreachable: {ex.Message}",
                    LookupStatus = "connection_error",
                    DetectedVendor = vendor ?? string.Empty,
                    SuggestedLookupUrl = requestUrl
                };
            }
            catch (OperationCanceledException)
            {
                return new ScrapeResult
                {
                    Success = false,
                    Message = "Web fallback request timed out.",
                    LookupStatus = "timeout",
                    DetectedVendor = vendor ?? string.Empty,
                    SuggestedLookupUrl = requestUrl
                };
            }
            catch (Exception ex)
            {
                return new ScrapeResult
                {
                    Success = false,
                    Message = $"Web fallback lookup failed: {ex.Message}",
                    LookupStatus = "error",
                    DetectedVendor = vendor ?? string.Empty,
                    SuggestedLookupUrl = requestUrl
                };
            }
        }

        private static string BuildSearchUrl(string template, string query, string? vendor)
        {
            var searchText = string.IsNullOrWhiteSpace(vendor)
                ? $"{query} device model specifications"
                : $"{vendor} {query} device model specifications";

            return template.Replace("{query}", Uri.EscapeDataString(searchText), StringComparison.OrdinalIgnoreCase);
        }

        private static (string title, string description, string sourceUrl) ExtractTopResult(string html)
        {
            var title = string.Empty;
            var description = string.Empty;
            var sourceUrl = string.Empty;

            var titleMatch = Regex.Match(
                html,
                "<a[^>]*class=\"result__a\"[^>]*href=\"(?<href>[^\"]+)\"[^>]*>(?<title>.*?)</a>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (titleMatch.Success)
            {
                sourceUrl = NormalizeSearchResultUrl(WebUtility.HtmlDecode(titleMatch.Groups["href"].Value));
                title = CleanupHtmlText(titleMatch.Groups["title"].Value);
            }

            var snippetMatch = Regex.Match(
                html,
                "<(a|div)[^>]*class=\"result__snippet\"[^>]*>(?<snippet>.*?)</(a|div)>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (snippetMatch.Success)
            {
                description = CleanupHtmlText(snippetMatch.Groups["snippet"].Value);
            }

            return (title, description, sourceUrl);
        }

        private static string NormalizeSearchResultUrl(string rawUrl)
        {
            if (string.IsNullOrWhiteSpace(rawUrl))
            {
                return string.Empty;
            }

            if (rawUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || rawUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return rawUrl;
            }

            if (!rawUrl.Contains("uddg=", StringComparison.OrdinalIgnoreCase))
            {
                return rawUrl;
            }

            var marker = rawUrl.IndexOf("uddg=", StringComparison.OrdinalIgnoreCase);
            if (marker < 0)
            {
                return rawUrl;
            }

            var encoded = rawUrl[(marker + 5)..];
            var ampIndex = encoded.IndexOf('&');
            if (ampIndex >= 0)
            {
                encoded = encoded[..ampIndex];
            }

            try
            {
                return Uri.UnescapeDataString(encoded);
            }
            catch (UriFormatException)
            {
                return rawUrl;
            }
        }

        private static string CleanupHtmlText(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }

            var noTags = Regex.Replace(html, "<.*?>", string.Empty, RegexOptions.Singleline);
            var decoded = WebUtility.HtmlDecode(noTags);
            return Regex.Replace(decoded, "\\s+", " ").Trim();
        }
    }
}
