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

            if (IsLikelyCiscoSerialNumber(normalizedSerial))
            {
                return "Cisco";
            }

            return string.Empty;
        }

        private static bool IsLikelyDellServiceTag(string serial)
        {
            return serial.Length == 7 && serial.All(char.IsLetterOrDigit);
        }

        private static bool IsLikelyCiscoSerialNumber(string serial)
        {
            if (serial.Length != 11 || !serial.All(char.IsLetterOrDigit))
            {
                return false;
            }

            return char.IsLetter(serial[0])
                && char.IsLetter(serial[1])
                && char.IsLetter(serial[2])
                && char.IsDigit(serial[3])
                && char.IsDigit(serial[4])
                && char.IsDigit(serial[5])
                && char.IsDigit(serial[6]);
        }
    }
}