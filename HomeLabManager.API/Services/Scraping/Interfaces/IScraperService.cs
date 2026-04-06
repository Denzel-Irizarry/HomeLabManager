using HomeLabManager.Core.Scraping.Models;

namespace HomeLabManager.API.Services.Scraping.Interfaces
{
    public interface IScraperService
    {
        Task<ScrapeResult> LookupDeviceAsync(string query);
    }
}
