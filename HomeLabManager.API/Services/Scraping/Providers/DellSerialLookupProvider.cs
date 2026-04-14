using System.Net.Http.Headers;
using System.Text.Json;
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
                return new ScrapeResult
                {
                    Success = false,
                    Message = "Dell serial lookup URL is not configured."
                };
            }

            var requestUrl = BuildLookupUrl(lookupUrl, query);
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

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

            try
            {
                using var document = JsonDocument.Parse(responseBody);
                var root = document.RootElement;

                var productName = ReadString(root, "productName", "ProductName", "name", "Name", "title", "Title");
                var manufacturer = ReadString(root, "manufacturer", "Manufacturer", "vendor", "Vendor");
                var modelNumber = ReadString(root, "modelNumber", "ModelNumber", "model", "Model");
                var description = ReadString(root, "description", "Description");
                var imageUrl = ReadString(root, "imageUrl", "ImageUrl", "image", "Image");
                var sourceUrl = ReadString(root, "sourceUrl", "SourceUrl", "url", "Url");

                if (string.IsNullOrWhiteSpace(productName)
                    && string.IsNullOrWhiteSpace(manufacturer)
                    && string.IsNullOrWhiteSpace(modelNumber)
                    && string.IsNullOrWhiteSpace(description))
                {
                    return new ScrapeResult
                    {
                        Success = false,
                        Message = "Dell response did not contain recognized product fields."
                    };
                }

                return new ScrapeResult
                {
                    Success = true,
                    Message = "Dell match found.",
                    DeviceInfo = new ScrapedDeviceInfo
                    {
                        ProductName = productName,
                        Manufacturer = manufacturer,
                        ModelNumber = modelNumber,
                        SerialNumber = query,
                        Description = description,
                        ImageUrl = imageUrl,
                        SourceUrl = string.IsNullOrWhiteSpace(sourceUrl) ? requestUrl : sourceUrl,
                        SourceType = ScrapeSourceType.VendorWebsite
                    }
                };
            }
            catch (JsonException)
            {
                return new ScrapeResult
                {
                    Success = false,
                    Message = "Dell lookup response could not be parsed as JSON."
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
            return $"{lookupUrl}{separator}serial={Uri.EscapeDataString(serial)}";
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