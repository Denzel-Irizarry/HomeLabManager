using HomeLabManager.Core.Scraping.Models;

namespace HomeLabManager.API.Services.Scraping.Interfaces
{
    public interface IHardwareLookupProvider
    {
        bool CanHandle(string codeType);
        Task<ScrapeResult> SearchAsync(string query);
    }
}
