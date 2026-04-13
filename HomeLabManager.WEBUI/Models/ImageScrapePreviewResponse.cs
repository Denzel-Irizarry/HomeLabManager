namespace HomeLabManager.WEBUI.Models
{
    public class ImageScrapePreviewResponse
    {
        // These fields will be populated by the image processing and code extraction logic, they will indicate what code was extracted from the image and whether it is a UPC, QR code, or some other type of code, this will help the frontend determine how to display the results to the user and whether to attempt a lookup with the extracted code
        public string ExtractedCode { get; set; }
        public string ExtractedCodeType { get; set; }
        public bool CanAttemptLookup { get; set; }
        public bool LookupSucceeded { get; set; }
        public string Message { get; set; }

        // Device information fields that may be returned from the lookup, these will be populated if the lookup was successful and the provider was able to extract this information, otherwise they will be empty strings
        public string ProductName { get; set; }
        public string Manufacturer { get; set; }
        public string ModelNumber { get; set; }
        public string UPC { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string SourceUrl { get; set; }

    }
}
