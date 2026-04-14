namespace HomeLabManager.WEBUI.Models
{
    public class ImageScrapePreviewResponse
    {
        // These fields will be populated by the image processing and code extraction logic, they will indicate what code was extracted from the image and whether it is a UPC, QR code, or some other type of code, this will help the frontend determine how to display the results to the user and whether to attempt a lookup with the extracted code
        public string ExtractedCode { get; set; } = string.Empty;
        public string ExtractedCodeType { get; set; } = string.Empty;
        public bool CanAttemptLookup { get; set; }
        public bool LookupSucceeded { get; set; }
        public string Message { get; set; } = string.Empty;
        public string DetectedVendor { get; set; } = string.Empty;
        public string LookupStatus { get; set; } = string.Empty;
        public string SupportLookupUrl { get; set; } = string.Empty;

        // Device information fields that may be returned from the lookup, these will be populated if the lookup was successful and the provider was able to extract this information, otherwise they will be empty strings
        public string ProductName { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string ModelNumber { get; set; } = string.Empty;
        public string UPC { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string SourceUrl { get; set; } = string.Empty;

    }
}
