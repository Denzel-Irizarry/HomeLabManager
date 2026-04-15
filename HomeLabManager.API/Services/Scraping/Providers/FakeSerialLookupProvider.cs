using HomeLabManager.API.Services.Scraping.Interfaces;
using HomeLabManager.Core.Scraping.DTOs;
using HomeLabManager.Core.Scraping.Enums;
using HomeLabManager.Core.Scraping.Models;

namespace HomeLabManager.API.Services.Scraping.Providers
{
    public class FakeSerialLookupProvider : IHardwareLookupProvider
    {
        public bool CanHandle(string codeType, string? vendor = null)
        {
            return string.Equals(codeType, "SerialNumber", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(vendor);
        }

        public Task<ScrapeResult> SearchAsync(string query, string? vendor = null)
        {
            if(string.Equals(query, "TEST-SERIAL-001", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new ScrapeResult
                {
                    Success = true,
                    Message = "Fake serial provider found a match.",
                    DeviceInfo = new ScrapedDeviceInfo
                    {
                        ProductName = "Test Router",
                        Manufacturer = "FakeVendor",
                        ModelNumber = "FAKE-1000",
                        SerialNumber = "TEST-SERIAL-001",
                        UPC = "123456789012",
                        Category = "Networking",
                        Description = "Fake test device returned by the fake provider.",
                        ImageUrl = string.Empty,
                        SourceUrl = "https://example.com/fake-device",
                        SourceType = ScrapeSourceType.ManualEntry
                    }
                });
            }
            return Task.FromResult(new ScrapeResult
            {
                Success = false,
                Message = "Fake serial provider found no match."
            });
        }
    }
}