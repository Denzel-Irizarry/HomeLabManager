using HomeLabManager.API.Services.Scraping.Interfaces;
using HomeLabManager.Core.Scraping.DTOs;
using HomeLabManager.Core.Scraping.Enums;
using HomeLabManager.Core.Scraping.Models;

namespace HomeLabManager.API.Services.Scraping.Providers
{
    public class FakeHpeSerialLookupProvider : IHardwareLookupProvider
    {
        public bool CanHandle(string codeType, string? vendor = null)
        {
            return string.Equals(codeType, "SerialNumber", StringComparison.OrdinalIgnoreCase)
                && (string.IsNullOrWhiteSpace(vendor)
                    || string.Equals(vendor, "HPE", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(vendor, "HP", StringComparison.OrdinalIgnoreCase));
        }

        public Task<ScrapeResult> SearchAsync(string query, string? vendor = null)
        {
            if (string.Equals(query, "CN1234A1BC", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new ScrapeResult
                {
                    Success = true,
                    Message = "Fake HPE provider found a match.",
                    LookupStatus = "success",
                    DetectedVendor = "HPE",
                    SuggestedLookupUrl = "https://partsurfer.hpe.com/Search.aspx?type=SERIAL&SearchText=CN1234A1BC",
                    DeviceInfo = new ScrapedDeviceInfo
                    {
                        ProductName = "HPE ProLiant DL360 Gen10",
                        Manufacturer = "HPE",
                        ModelNumber = "DL360 Gen10",
                        SerialNumber = "CN1234A1BC",
                        Category = "Servers",
                        Description = "HPE ProLiant rack server used for deterministic provider testing.",
                        SourceUrl = "https://partsurfer.hpe.com/Search.aspx?type=SERIAL&SearchText=CN1234A1BC",
                        SourceType = ScrapeSourceType.VendorWebsite
                    }
                });
            }

            if (string.Equals(query, "SGH9876XYZ", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new ScrapeResult
                {
                    Success = true,
                    Message = "Fake HPE provider found a match.",
                    LookupStatus = "success",
                    DetectedVendor = "HPE",
                    SuggestedLookupUrl = "https://partsurfer.hpe.com/Search.aspx?type=SERIAL&SearchText=SGH9876XYZ",
                    DeviceInfo = new ScrapedDeviceInfo
                    {
                        ProductName = "HPE Aruba 2930F Switch",
                        Manufacturer = "HPE",
                        ModelNumber = "JL256A",
                        SerialNumber = "SGH9876XYZ",
                        Category = "Networking",
                        Description = "HPE Aruba switch used for deterministic provider testing.",
                        SourceUrl = "https://partsurfer.hpe.com/Search.aspx?type=SERIAL&SearchText=SGH9876XYZ",
                        SourceType = ScrapeSourceType.VendorWebsite
                    }
                });
            }

            return Task.FromResult(new ScrapeResult
            {
                Success = false,
                Message = "Fake HPE provider found no match.",
                LookupStatus = "not_found",
                DetectedVendor = "HPE"
            });
        }
    }
}