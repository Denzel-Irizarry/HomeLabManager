namespace HomeLabManager.WEBUI.Models;

public class ScrapeResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string DetectedVendor { get; set; } = string.Empty;
    public string LookupStatus { get; set; } = string.Empty;
    public string SuggestedLookupUrl { get; set; } = string.Empty;
    public ScrapedDeviceInfo? DeviceInfo { get; set; }
}