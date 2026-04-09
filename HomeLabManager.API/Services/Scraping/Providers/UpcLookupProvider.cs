using HomeLabManager.API.Services.Scraping.Interfaces;
using HomeLabManager.API.Models;
using HomeLabManager.Core.Scraping.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using HomeLabManager.Core.Scraping.DTOs;
using HomeLabManager.Core.Scraping.Enums;

namespace HomeLabManager.API.Services.Scraping.Providers
{
    // This is a hardware lookup provider that uses the UPC Database API to look up devices based on their UPC code
    public class UpcLookupProvider : IHardwareLookupProvider
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        public UpcLookupProvider(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }   
        
        public async Task<ScrapeResult> SearchAsync(string query)
        {
            var apiKey = _configuration["UpcDatabase:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return new ScrapeResult
                {
                    Success = false,
                    Message = "UPC Database API key is not configured."
                };
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                return new ScrapeResult
                {
                    Success = false,
                    Message = "Query cannot be empty."
                };
            }

        
            var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.upcdatabase.org/product/{query}");

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return new ScrapeResult
                {
                    Success = false,
                    Message = $"UPC Database request failed with status code {(int)response.StatusCode}."
                };
            }

            var responseBody = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return new ScrapeResult
                {
                    Success = false,
                    Message = "UPC Database returned an empty response."
                };
            }
            
            // Deserialize the response into our UpcDatabaseResponse model
            var upcResponse = JsonSerializer.Deserialize<UpcDatabaseResponse>(responseBody);

            // If we couldn't parse the response, return a failed result
            if (upcResponse == null)
            {
                return new ScrapeResult
                {
                    Success = false,
                    Message = "UPC Database response could not be parsed."
                };
            }

            // If the title is empty, it means we didn't get a valid product back, so we can treat that as a failed search
            if (string.IsNullOrWhiteSpace(upcResponse.Title))
            {
                return new ScrapeResult
                {
                    Success = false,
                    Message = "UPC Database returned no product title."
                };
            }

            // If we got here, we have a successful response with product information, so we can return that in our ScrapeResult
            return new ScrapeResult
            {
                Success = true,
                Message = "UPC Database match found.",
                DeviceInfo = new ScrapedDeviceInfo
                {
                    ProductName = upcResponse.Title,
                    Manufacturer = !string.IsNullOrWhiteSpace(upcResponse.Manufacturer)
                    ? upcResponse.Manufacturer : upcResponse.Brand,
                    Description = upcResponse.Description,
                    UPC = upcResponse.Barcode,
                    Category = upcResponse.Category,
                    SourceUrl = $"https://api.upcdatabase.org/product/{query}",
                    SourceType = ScrapeSourceType.UpcDatabase
                }
            };

        }
    }
}



