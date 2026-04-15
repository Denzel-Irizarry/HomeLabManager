using HomeLabManager.API.Services.Scraping.Interfaces;
using HomeLabManager.Core.Scraping.Models;
using System.Collections.Generic;
using System.Linq;

namespace HomeLabManager.API.Services.Scraping
{
    public class ScraperService : IScraperService
    {
        private readonly IEnumerable<IHardwareLookupProvider> _providers;

        public ScraperService(IEnumerable<IHardwareLookupProvider> providers)
        {
            _providers = providers;
        }
        public async Task<ScrapeResult> LookupDeviceAsync(string query, string codeType)
        {
            var detectedVendor = codeType.Equals("SerialNumber", StringComparison.OrdinalIgnoreCase)
                ? SerialVendorDetector.DetectVendor(query)
                : string.Empty;

            ScrapeResult? lastFailure = null;

            foreach (var provider in _providers.Where(provider => provider.CanHandle(codeType, detectedVendor)))
            {
                var result = await provider.SearchAsync(query, detectedVendor);

                if (result.Success)
                {
                    if (string.IsNullOrWhiteSpace(result.DetectedVendor))
                    {
                        result.DetectedVendor = detectedVendor;
                    }

                    result.LookupStatus = string.IsNullOrWhiteSpace(result.LookupStatus)
                        ? "success"
                        : result.LookupStatus;
                    return result;
                }

                if (lastFailure == null
                    || (!string.IsNullOrWhiteSpace(result.SuggestedLookupUrl) && string.IsNullOrWhiteSpace(lastFailure.SuggestedLookupUrl)))
                {
                    lastFailure = result;
                }
            }

            if (lastFailure != null)
            {
                return new ScrapeResult
                {
                    Success = false,
                    Message = string.IsNullOrWhiteSpace(lastFailure.Message)
                        ? "No providers returned a match."
                        : lastFailure.Message,
                    DetectedVendor = string.IsNullOrWhiteSpace(lastFailure.DetectedVendor)
                        ? detectedVendor
                        : lastFailure.DetectedVendor,
                    LookupStatus = string.IsNullOrWhiteSpace(lastFailure.LookupStatus)
                        ? "not_found"
                        : lastFailure.LookupStatus,
                    SuggestedLookupUrl = lastFailure.SuggestedLookupUrl,
                    DeviceInfo = lastFailure.DeviceInfo
                };
            }

            return new ScrapeResult
            {
                Success = false,
                Message = "No providers returned a match.",
                DetectedVendor = detectedVendor,
                LookupStatus = "not_supported"
            };
        }

    }
}
