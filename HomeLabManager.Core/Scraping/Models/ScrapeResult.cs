using HomeLabManager.Core.Scraping.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace HomeLabManager.Core.Scraping.Models
{
    public class ScrapeResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public ScrapedDeviceInfo? DeviceInfo { get; set; }
    }
}
