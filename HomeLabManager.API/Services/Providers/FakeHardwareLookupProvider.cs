using HomeLabManager.API.Services.Scraping.Interfaces;
using HomeLabManager.Core.Scraping.DTOs;
using HomeLabManager.Core.Scraping.Enums;
using HomeLabManager.Core.Scraping.Models;

namespace HomeLabManager.API.Services.Providers
{
    public class FakeHardwareLookupProvider : IHardwareLookupProvider
    {
        public Task<ScrapeResult> SearchAsync(string query)
        {
             if (query == "test-device")
            {
                return Task.FromResult(new ScrapeResult
                {
                    Success = true,
                    Message = "Fake provider found a match.",
                    DeviceInfo = new ScrapedDeviceInfo
                    {
                        ProductName = "Test Router",
                        Manufacturer = "FakeVendor",
                        ModelNumber = "FAKE-1000",
                        SerialNumber = "SN-TEST-001",
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
                Message = "Fake provider found no match."
            });
        } 
    }
}
