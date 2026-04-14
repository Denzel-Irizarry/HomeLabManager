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

            foreach (var provider in _providers.Where(provider => provider.CanHandle(codeType, detectedVendor)))
            {
                var result = await provider.SearchAsync(query, detectedVendor);

                if (result.Success)
                {
                    result.DetectedVendor = detectedVendor;
                    return result;
                }
            }

            return new ScrapeResult
            {
                Success = false,
                Message = "No providers returned a match.",
                DetectedVendor = detectedVendor
            };
        }

    }
}
