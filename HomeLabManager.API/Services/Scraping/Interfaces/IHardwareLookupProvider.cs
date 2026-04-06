using HomeLabManager.Core.Scraping.Models;

namespace HomeLabManager.API.Services.Scraping.Interfaces
{
    public interface IHardwareLookupProvider
    {
        Task<ScrapeResult> SearchAsync(string query);
    }
}
