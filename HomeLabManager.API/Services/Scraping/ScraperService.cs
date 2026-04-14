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
            foreach (var provider in _providers.Where(provider => provider.CanHandle(codeType)))
            {
                var result = await provider.SearchAsync(query);

                if (result.Success)
                {
                    return result;
                }
            }

            return new ScrapeResult
            {
                Success = false,
                Message = "No providers returned a match."
            };
        }

    }
}
