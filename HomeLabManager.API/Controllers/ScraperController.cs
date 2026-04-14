using HomeLabManager.API.Models;
using HomeLabManager.API.Services.Scraping.Interfaces;
using Microsoft.AspNetCore.Mvc;
using HomeLabManager.API.Interfaces;
using HomeLabManager.API.ExceptionsAPI;
using System.Linq;

namespace HomeLabManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScraperController : ControllerBase
    {
        private readonly IScraperService _scraperService;
        private readonly ScanServiceInterface _scanService;

        public ScraperController(IScraperService scraperService, ScanServiceInterface scanService)
        {
            this._scraperService = scraperService;
            this._scanService = scanService;
        }

        //maps to POST api/scraper/search, this is the endpoint that the frontend will call when the user wants to search for a device, it will pass the search term in the body of the request as a ScraperSearchRequest object which contains a single property Query that is the search term, this will then be passed to the scraper service which will then pass it to the providers to search for a match and return the result back to the frontend
        [HttpPost("search")]
        public async Task<ActionResult> Search([FromBody] ScraperSearchRequest request)
        {
            var codeType = AnalyzeSearchQuery(request.Query);
            var result = await _scraperService.LookupDeviceAsync(request.Query, codeType);
            return Ok(result);
        }
        
        [HttpPost("from-image")]
        public async Task<ActionResult<ImageScrapePreviewResponse>> FromImage(IFormFile file)
        {
            try
            {
            
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            
            using var stream = file.OpenReadStream();

            // Use the ScanService to extract the serial number or identifier from the image, this will return the extracted code as a string which we will then pass to the scraper service to look up the device information based on that code
            var extractedCode = await _scanService.ExtractSerialAsync( new ScanRequest{
                ImageStream = stream
            });

            // Analyze the extracted code to determine if it's a UPC, URL, Serial Number, or Unknown, and whether we can attempt a lookup based on that type. This will help us provide more informative feedback to the user in the response.
            var analysis = AnalyzeExtractedCode(extractedCode);

            // If the code type is not suitable for lookup, return a response indicating that we cannot attempt a lookup, along with the extracted code and the reason why.
            if (!analysis.CanAttemptLookup)
            {
                return Ok(new ImageScrapePreviewResponse
                {
                    ExtractedCode = extractedCode,
                    ExtractedCodeType = analysis.CodeType,
                    CanAttemptLookup = false,
                    LookupSucceeded = false,
                    Message = analysis.Message 
                });
            }

            var scrapeResult = await _scraperService.LookupDeviceAsync(extractedCode, analysis.CodeType);

            // Map the ScrapeResult to the ImageScrapePreviewResponse
            var response = new ImageScrapePreviewResponse
            {
                
                ExtractedCode = extractedCode,
                LookupSucceeded = scrapeResult.Success,
                Message = scrapeResult.Message,
                DetectedVendor = scrapeResult.DetectedVendor,
                ProductName = scrapeResult.DeviceInfo?.ProductName ?? string.Empty,
                Manufacturer = scrapeResult.DeviceInfo?.Manufacturer ?? string.Empty,
                ModelNumber = scrapeResult.DeviceInfo?.ModelNumber ?? string.Empty,
                UPC = scrapeResult.DeviceInfo?.UPC ?? string.Empty,
                Category = scrapeResult.DeviceInfo?.Category ?? string.Empty,
                Description = scrapeResult.DeviceInfo?.Description ?? string.Empty,
                ImageUrl = scrapeResult.DeviceInfo?.ImageUrl ?? string.Empty,
                SourceUrl = scrapeResult.DeviceInfo?.SourceUrl ?? string.Empty
            };

            return Ok(response);
            }
            catch (BarcodeNotFoundException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (FileScanningUploadException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (SerialNumberMissingException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while processing the image preview request.");
            }
        }
        
        //helper method to analyze the extracted code and determine if it's a UPC, URL, Serial Number, or Unknown, and whether we can attempt a lookup based on that type. 
        private static (string CodeType, bool CanAttemptLookup, string Message) AnalyzeExtractedCode(string extractedCode)
        {
            if (string.IsNullOrWhiteSpace(extractedCode))
            {
                return ("Unknown", false, "No code was extracted from the image.");
            }

            if (Uri.IsWellFormedUriString(extractedCode, UriKind.Absolute))
            {
                return ("Url", false, "Scanned code is a URL and no lookup provider is available for URLs.");
            }

            if (extractedCode.All(char.IsDigit))
            {
                return ("Upc", true, string.Empty);
            }

            if (extractedCode.All(char.IsLetterOrDigit))
            {
                return ("SerialNumber", true, string.Empty);
            }

            return ("Unknown", false, "Scanned code type is not supported for lookup.");
        }

        private static string AnalyzeSearchQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return "Unknown";
            }

            if (query.All(char.IsDigit))
            {
                return "Upc";
            }

            return "SerialNumber";
        }



    }
    
}
