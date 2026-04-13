namespace HomeLabManager.API.Models
{
    public class ImageScrapePreviewResponse
    {
        // These fields will be populated by the image processing and code extraction logic, they will indicate what code was extracted from the image and whether it is a UPC, QR code, or some other type of code, this will help the frontend determine how to display the results to the user and whether to attempt a lookup with the extracted code
        public string ExtractedCode { get; set; } = string.Empty;
        public string ExtractedCodeType { get; set; } = string.Empty;
        public bool CanAttemptLookup { get; set; }

        // These fields will indicate whether the lookup was successful and any messages from the providers, this will help the frontend determine how to display the results to the user, for example if the lookup failed it can show the extracted code and message to the user so they can try again with a clearer image or manually enter the code
        public bool LookupSucceeded { get; set; }
        public string Message { get; set; } = string.Empty;

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
