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

            var attemptedProvider = false;
            var lastFailureMessage = "No providers returned a match.";

            foreach (var provider in _providers.Where(provider => provider.CanHandle(codeType, detectedVendor)))
            {
                attemptedProvider = true;
                var result = await provider.SearchAsync(query, detectedVendor);

                if (result.Success)
                {
                    result.DetectedVendor = detectedVendor;
                    return result;
                }

                if (!string.IsNullOrWhiteSpace(result.Message))
                {
                    lastFailureMessage = result.Message;
                }
            }

            return new ScrapeResult
            {
                Success = false,
                Message = attemptedProvider ? lastFailureMessage : "No providers returned a match.",
                DetectedVendor = detectedVendor
            };
        }

    }
}
