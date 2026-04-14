using HomeLabManager.Core.Scraping.Models;

namespace HomeLabManager.API.Services.Scraping.Interfaces
{
    public interface IHardwareLookupProvider
    {
        bool CanHandle(string codeType, string? vendor = null);
        Task<ScrapeResult> SearchAsync(string query, string? vendor = null);
    }
}
