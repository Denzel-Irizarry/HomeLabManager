using HomeLabManager.Core.Scraping.Enums;
using System;
using System.Collections.Generic;
using System.Text;


namespace HomeLabManager.Core.Scraping.DTOs
{
    public class ScrapedDeviceInfo
    {
        public string ProductName { get; set; } = string.Empty;

        public string Manufacturer { get; set; } = string.Empty;

        public string ModelNumber { get; set; } = string.Empty;

        public string SerialNumber { get; set; } = string.Empty;

        public string UPC { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        public string SourceUrl { get; set; } = string.Empty;

        public ScrapeSourceType SourceType { get; set; } = ScrapeSourceType.Unknown; 
    }
}
