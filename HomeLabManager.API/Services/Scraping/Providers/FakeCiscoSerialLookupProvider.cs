using HomeLabManager.API.Services.Scraping.Interfaces;
using HomeLabManager.Core.Scraping.DTOs;
using HomeLabManager.Core.Scraping.Enums;
using HomeLabManager.Core.Scraping.Models;

namespace HomeLabManager.API.Services.Scraping.Providers
{
    public class FakeCiscoSerialLookupProvider : IHardwareLookupProvider
    {
        public bool CanHandle(string codeType, string? vendor = null)
        {
            return string.Equals(codeType, "SerialNumber", StringComparison.OrdinalIgnoreCase)
                && string.Equals(vendor, "Cisco", StringComparison.OrdinalIgnoreCase);
        }

        public Task<ScrapeResult> SearchAsync(string query, string? vendor = null)
        {
            // Example Cisco serial numbers for testing
            // Format: 3 letters + 8 digits (e.g., FCW2621A40F)
            if (string.Equals(query, "FCW2621A40F", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new ScrapeResult
                {
                    Success = true,
                    Message = "Fake Cisco provider found a match.",
                    LookupStatus = "success",
                    DetectedVendor = "Cisco",
                    SuggestedLookupUrl = "https://www.cisco.com/c/en/us/support/all-products.html",
                    DeviceInfo = new ScrapedDeviceInfo
                    {
                        ProductName = "Catalyst 2960-X Series Switch",
                        Manufacturer = "Cisco",
                        ModelNumber = "WS-C2960X-48TS-L",
                        SerialNumber = "FCW2621A40F",
                        Category = "Networking",
                        Description = "Cisco Catalyst 2960-X 48 Port GigabitEthernet Switch",
                        SourceUrl = "https://www.cisco.com/c/en/us/support/all-products.html",
                        SourceType = ScrapeSourceType.VendorWebsite
                    }
                });
            }

            if (string.Equals(query, "JAE17260H8Z", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new ScrapeResult
                {
                    Success = true,
                    Message = "Fake Cisco provider found a match.",
                    LookupStatus = "success",
                    DetectedVendor = "Cisco",
                    SuggestedLookupUrl = "https://www.cisco.com/c/en/us/support/all-products.html",
                    DeviceInfo = new ScrapedDeviceInfo
                    {
                        ProductName = "Cisco ASR 1002-X Router",
                        Manufacturer = "Cisco",
                        ModelNumber = "ASR1002-X",
                        SerialNumber = "JAE17260H8Z",
                        Category = "Networking",
                        Description = "Aggregation Services Router with 10 GbE throughput",
                        SourceUrl = "https://www.cisco.com/c/en/us/support/all-products.html",
                        SourceType = ScrapeSourceType.VendorWebsite
                    }
                });
            }

            return Task.FromResult(new ScrapeResult
            {
                Success = false,
                Message = "Fake Cisco provider found no match.",
                LookupStatus = "not_found",
                DetectedVendor = "Cisco"
            });
        }
    }
}
