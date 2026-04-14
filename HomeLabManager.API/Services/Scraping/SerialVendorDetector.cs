namespace HomeLabManager.API.Services.Scraping
{
    public static class SerialVendorDetector
    {
        public static string DetectVendor(string serial)
        {
            if (string.IsNullOrWhiteSpace(serial))
            {
                return string.Empty;
            }

            var normalizedSerial = serial.Trim();

            if (IsLikelyDellServiceTag(normalizedSerial))
            {
                return "Dell";
            }

            return string.Empty;
        }

        private static bool IsLikelyDellServiceTag(string serial)
        {
            return serial.Length == 7 && serial.All(char.IsLetterOrDigit);
        }
    }
}