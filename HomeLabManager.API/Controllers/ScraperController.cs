using HomeLabManager.API.Models;
using HomeLabManager.API.Services.Scraping.Interfaces;
using HomeLabManager.Core.Scraping.Models;
using Microsoft.AspNetCore.Mvc;

namespace HomeLabManager.API.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class ScraperController : ControllerBase
    {
        private readonly IScraperService _scraperService;

        public ScraperController(IScraperService scraperService)
        {
            _scraperService = scraperService;
        }

        //maps to POST api/scraper/search, this is the endpoint that the frontend will call when the user wants to search for a device, it will pass the search term in the body of the request as a ScraperSearchRequest object which contains a single property Query that is the search term, this will then be passed to the scraper service which will then pass it to the providers to search for a match and return the result back to the frontend
        [HttpPost("search")]
        public async Task<ActionResult> Search([FromBody] ScraperSearchRequest request)
        {
            var result = await _scraperService.LookupDeviceAsync(request.Query);
            return Ok(result);
        }
        
    }
    
}
