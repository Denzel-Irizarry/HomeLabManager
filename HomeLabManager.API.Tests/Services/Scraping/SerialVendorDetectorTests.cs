using HomeLabManager.API.Services.Scraping;
using Xunit;

namespace HomeLabManager.API.Tests.Services.Scraping
{
    public class SerialVendorDetectorTests
    {
        [Fact]
        public void DetectVendor_WithCiscoPrefixedLabel_ReturnsCisco()
        {
            var result = SerialVendorDetector.DetectVendor("CISCO2811 V07");
            Assert.Equal("Cisco", result);
        }

        [Fact]
        public void DetectVendor_WithDellServiceTag_ReturnsDell()
        {
            var result = SerialVendorDetector.DetectVendor("5d34gt2");
            Assert.Equal("Dell", result);
        }
    }
}
