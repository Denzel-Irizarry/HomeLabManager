using HomeLabManager.API.Services.Scraping;
using HomeLabManager.API.Services.Scraping.Interfaces;
using HomeLabManager.Core.Scraping.Models;
using Xunit;

namespace HomeLabManager.API.Tests.Services.Scraping
{
    /// <summary>
    /// Test implementations of IHardwareLookupProvider for controlled testing
    /// </summary>
    public class TestProvider : IHardwareLookupProvider
    {
        private readonly string? _handledCodeType;
        private readonly string? _handledVendor;
        private readonly Func<string, string?, Task<ScrapeResult>> _searchFunc;

        public TestProvider(string? handledCodeType, string? handledVendor, Func<string, string?, Task<ScrapeResult>> searchFunc)
        {
            _handledCodeType = handledCodeType;
            _handledVendor = handledVendor;
            _searchFunc = searchFunc;
        }

        public bool CanHandle(string codeType, string? vendor = null)
        {
            if (_handledCodeType != null && !codeType.Equals(_handledCodeType, StringComparison.OrdinalIgnoreCase))
                return false;

            if (_handledVendor != null && !string.Equals(vendor, _handledVendor, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        public Task<ScrapeResult> SearchAsync(string query, string? vendor = null)
        {
            return _searchFunc(query, vendor);
        }
    }

    public class ScraperServiceRoutingTests
    {

        [Fact]
        public async Task LookupDeviceAsync_WithDellSerialNumber_RoutesToDellProvider()
        {
            // Arrange: Dell serial format (7 alphanumeric)
            var dellSerial = "ABC1234";
            
            var dellProvider = new TestProvider(
                "SerialNumber", 
                "Dell",
                (serial, vendor) => Task.FromResult(new ScrapeResult
                {
                    Success = true,
                    Message = "Found Dell device",
                    DetectedVendor = "Dell",
                    LookupStatus = "success"
                })
            );

            var providers = new List<IHardwareLookupProvider> { dellProvider };
            var service = new ScraperService(providers);

            // Act
            var result = await service.LookupDeviceAsync(dellSerial, "SerialNumber");

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Dell", result.DetectedVendor);
        }

        [Fact]
        public async Task LookupDeviceAsync_WithCiscoSerialNumber_RoutesToCiscoProvider()
        {
            // Arrange: Cisco serial format (11 chars: 3 letters + 8 digits)
            var ciscoSerial = "ABC12345678";
            
            var ciscoProvider = new TestProvider(
                "SerialNumber",
                "Cisco",
                (serial, vendor) => Task.FromResult(new ScrapeResult
                {
                    Success = false,
                    Message = "Generic support page",
                    DetectedVendor = "Cisco",
                    LookupStatus = "manual_lookup_required",
                    SuggestedLookupUrl = "https://www.cisco.com/support"
                })
            );

            var providers = new List<IHardwareLookupProvider> { ciscoProvider };
            var service = new ScraperService(providers);

            // Act
            var result = await service.LookupDeviceAsync(ciscoSerial, "SerialNumber");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Cisco", result.DetectedVendor);
            Assert.Equal("manual_lookup_required", result.LookupStatus);
        }

        [Fact]
        public async Task LookupDeviceAsync_WithUnknownSerial_FallsBackToNextProvider()
        {
            // Arrange: Unknown serial that doesn't match Dell/Cisco format (10 chars)
            var unknownSerial = "MXQ2160G6X";
            
            // First provider (e.g., Dell) can't handle unknown serial
            var dellProvider = new TestProvider(
                "SerialNumber",
                "Dell",
                (serial, vendor) => Task.FromResult(new ScrapeResult())
            );

            // Second provider (e.g., HPE) handles unknown serials
            var hpeProvider = new TestProvider(
                "SerialNumber",
                null,  // handles any vendor (null or otherwise)
                (serial, vendor) => Task.FromResult(new ScrapeResult
                {
                    Success = false,
                    Message = "Not found in PartSurfer",
                    DetectedVendor = "HPE",
                    LookupStatus = "not_found",
                    SuggestedLookupUrl = "https://partsurfer.hpe.com/Search.aspx?SearchText=MXQ2160G6X"
                })
            );

            var providers = new List<IHardwareLookupProvider> { dellProvider, hpeProvider };
            var service = new ScraperService(providers);

            // Act
            var result = await service.LookupDeviceAsync(unknownSerial, "SerialNumber");

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.SuggestedLookupUrl);
            StringAssert.Contains("partsurfer.hpe.com", result.SuggestedLookupUrl);
        }

        [Fact]
        public async Task LookupDeviceAsync_ReturnFirstSuccess()
        {
            // Arrange: Multiple providers, first one succeeds
            var serialNumber = "ABC1234";
            
            var callCount = 0;
            var firstProvider = new TestProvider(
                "SerialNumber",
                "Dell",
                (serial, vendor) =>
                {
                    callCount++;
                    return Task.FromResult(new ScrapeResult
                    {
                        Success = true,
                        Message = "Found",
                        DetectedVendor = "Dell",
                        LookupStatus = "success"
                    });
                }
            );

            var secondProvider = new TestProvider(
                "SerialNumber",
                "Dell",
                (serial, vendor) => Task.FromResult(new ScrapeResult())
            );

            var providers = new List<IHardwareLookupProvider> { firstProvider, secondProvider };
            var service = new ScraperService(providers);

            // Act
            var result = await service.LookupDeviceAsync(serialNumber, "SerialNumber");

            // Assert
            Assert.True(result.Success);
            Assert.Equal(1, callCount); // First provider should only be called once
        }

        [Fact]
        public async Task LookupDeviceAsync_PreferResultWithSuggestedUrl()
        {
            // Arrange: Multiple failures, one with manual-lookup URL should win (URL takes priority)
            var unknownSerial = "XYZ123456789";  // 12 chars, won't match Dell/Cisco
            
            var hpeProvider = new TestProvider(
                "SerialNumber",
                null,
                (serial, vendor) => Task.FromResult(new ScrapeResult
                {
                    Success = false,
                    Message = "Not found",
                    DetectedVendor = "HPE",
                    LookupStatus = "not_found",
                    SuggestedLookupUrl = "https://partsurfer.hpe.com/Search.aspx?SearchText=XYZ123456789"
                })
            );

            var fallbackProvider = new TestProvider(
                "SerialNumber",
                null,
                (serial, vendor) => Task.FromResult(new ScrapeResult
                {
                    Success = false,
                    Message = "No match",
                    DetectedVendor = "FallbackVendor",
                    LookupStatus = "not_found"
                    // No SuggestedLookupUrl
                })
            );

            var providers = new List<IHardwareLookupProvider> { hpeProvider, fallbackProvider };
            var service = new ScraperService(providers);

            // Act
            var result = await service.LookupDeviceAsync(unknownSerial, "SerialNumber");

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.SuggestedLookupUrl);
            StringAssert.Contains("partsurfer.hpe.com", result.SuggestedLookupUrl);
        }

        [Fact]
        public async Task LookupDeviceAsync_WithUpcCode_SkipsSerialProviders()
        {
            // Arrange: UPC query should only use UPC providers
            var upcCode = "012345678901";
            
            var upcProvider = new TestProvider(
                "Upc",
                null,
                (serial, vendor) => Task.FromResult(new ScrapeResult
                {
                    Success = true,
                    Message = "Found by UPC",
                    LookupStatus = "success"
                })
            );

            var serialProvider = new TestProvider(
                "SerialNumber",
                null,
                (serial, vendor) => Task.FromResult(new ScrapeResult())
            );

            var providers = new List<IHardwareLookupProvider> { upcProvider, serialProvider };
            var service = new ScraperService(providers);

            // Act
            var result = await service.LookupDeviceAsync(upcCode, "Upc");

            // Assert
            Assert.True(result.Success);
        }

        [Fact]
        public async Task LookupDeviceAsync_NoProvidersCanHandle_ReturnsNotSupported()
        {
            // Arrange: No providers support the code type
            var provider = new TestProvider(
                "UnsupportedType",
                null,
                (serial, vendor) => Task.FromResult(new ScrapeResult())
            );

            var providers = new List<IHardwareLookupProvider> { provider };
            var service = new ScraperService(providers);

            // Act
            var result = await service.LookupDeviceAsync("anything", "UnknownType");

            // Assert
            Assert.False(result.Success);
            Assert.Equal("not_supported", result.LookupStatus);
        }

        [Fact]
        public async Task LookupDeviceAsync_PreserveProviderDetectedVendor()
        {
            // Arrange: Provider explicitly sets DetectedVendor, ScraperService should preserve it
            var serial = "ABC1234";
            var provider = new TestProvider(
                "SerialNumber",
                "Dell",
                (serial, vendor) => Task.FromResult(new ScrapeResult
                {
                    Success = true,
                    Message = "Found",
                    DetectedVendor = "Dell",
                    LookupStatus = "success"
                })
            );

            var providers = new List<IHardwareLookupProvider> { provider };
            var service = new ScraperService(providers);

            // Act
            var result = await service.LookupDeviceAsync(serial, "SerialNumber");

            // Assert
            Assert.Equal("Dell", result.DetectedVendor);
        }

        [Fact]
        public async Task LookupDeviceAsync_FillInDetectedVendorIfMissing()
        {
            // Arrange: Provider doesn't set DetectedVendor, ScraperService fills it in
            var serial = "ABC1234";
            var provider = new TestProvider(
                "SerialNumber",
                "Dell",
                (serial, vendor) => Task.FromResult(new ScrapeResult
                {
                    Success = true,
                    Message = "Found"
                    // DetectedVendor is null/empty
                })
            );

            var providers = new List<IHardwareLookupProvider> { provider };
            var service = new ScraperService(providers);

            // Act
            var result = await service.LookupDeviceAsync(serial, "SerialNumber");

            // Assert
            Assert.Equal("Dell", result.DetectedVendor);
        }

        [Fact]
        public async Task LookupDeviceAsync_MultipleFailures_SelectBestFallback()
        {
            // Arrange: Three failing providers, prefer the one with manual-lookup URL
            var unknownSerial = "ABC1234567890";  // 13 chars, won't match Dell/Cisco
            
            // Provider 1: Generic failure without URL (will be initial lastFailure)
            var provider1 = new TestProvider(
                "SerialNumber",
                null,
                (serial, vendor) => Task.FromResult(new ScrapeResult
                {
                    Success = false,
                    Message = "Not found",
                    DetectedVendor = "Provider1",
                    LookupStatus = "not_found"
                })
            );

            // Provider 2: Failure with manual-lookup URL (will replace lastFailure)
            var provider2 = new TestProvider(
                "SerialNumber",
                null,
                (serial, vendor) => Task.FromResult(new ScrapeResult
                {
                    Success = false,
                    Message = "Manual lookup required",
                    DetectedVendor = "HPE",
                    LookupStatus = "manual_lookup_required",
                    SuggestedLookupUrl = "https://partsurfer.hpe.com/Search.aspx"
                })
            );

            // Provider 3: Another failure without URL (won't replace because provider2 has URL)
            var provider3 = new TestProvider(
                "SerialNumber",
                null,
                (serial, vendor) => Task.FromResult(new ScrapeResult
                {
                    Success = false,
                    Message = "No match",
                    DetectedVendor = "Provider3"
                })
            );

            var providers = new List<IHardwareLookupProvider> { provider1, provider2, provider3 };
            var service = new ScraperService(providers);

            // Act
            var result = await service.LookupDeviceAsync(unknownSerial, "SerialNumber");

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.SuggestedLookupUrl);
            Assert.Equal("manual_lookup_required", result.LookupStatus);
            StringAssert.Contains("partsurfer.hpe.com", result.SuggestedLookupUrl);
        }
    }

    /// <summary>
    /// Helper class for string assertions
    /// </summary>
    public static class StringAssert
    {
        public static void Contains(string expectedSubstring, string? actualString)
        {
            Assert.NotNull(actualString);
            Assert.Contains(expectedSubstring, actualString);
        }
    }
}
