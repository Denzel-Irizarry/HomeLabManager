using HomeLabManager.API.Services.Scraping.Interfaces;
using HomeLabManager.Core.Scraping.Models;
using System.Collections.Generic;

namespace HomeLabManager.API.Services.Scraping
{
    public class ScraperService : IScraperService
    {
        private readonly IEnumerable<IHardwareLookupProvider> _providers;

        public ScraperService(IEnumerable<IHardwareLookupProvider> providers)
        {
            _providers = providers;
        }
        public async Task<ScrapeResult> LookupDeviceAsync(string query)
        {
            foreach (var provider in _providers)
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
