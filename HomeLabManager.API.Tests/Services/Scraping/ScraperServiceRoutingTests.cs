using HomeLabManager.API.Services.Scraping;
using HomeLabManager.API.Services.Scraping.Interfaces;
using HomeLabManager.Core.Scraping.Models;
using Xunit;

namespace HomeLabManager.API.Tests.Services.Scraping
{
    /// <summary>
    /// Test implementation of IHardwareLookupProvider for controlled routing behavior.
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

            var result = await service.LookupDeviceAsync(dellSerial, "SerialNumber");

            Assert.True(result.Success);
            Assert.Equal("Dell", result.DetectedVendor);
        }

        [Fact]
        public async Task LookupDeviceAsync_WithCiscoSerialNumber_RoutesToCiscoProvider()
        {
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

            var result = await service.LookupDeviceAsync(ciscoSerial, "SerialNumber");

            Assert.False(result.Success);
            Assert.Equal("Cisco", result.DetectedVendor);
            Assert.Equal("manual_lookup_required", result.LookupStatus);
        }

        [Fact]
        public async Task LookupDeviceAsync_UnknownVendor_DoesNotStopOnManualLookupRequired()
        {
            var unknownSerial = "CISCO2811V07";

            var dellLikeManualProvider = new TestProvider(
                "SerialNumber",
                null,
                (serial, vendor) => Task.FromResult(new ScrapeResult
                {
                    Success = false,
                    Message = "Manual lookup required",
                    LookupStatus = "manual_lookup_required",
                    SuggestedLookupUrl = "https://www.dell.com/support"
                })
            );

            var fallbackProvider = new TestProvider(
                "SerialNumber",
                null,
                (serial, vendor) => Task.FromResult(new ScrapeResult
                {
                    Success = false,
                    Message = "Not found via fallback",
                    LookupStatus = "not_found",
                    SuggestedLookupUrl = "https://fallback.example/search?q=CISCO2811V07"
                })
            );

            var providers = new List<IHardwareLookupProvider> { dellLikeManualProvider, fallbackProvider };
            var service = new ScraperService(providers);

            var result = await service.LookupDeviceAsync(unknownSerial, "SerialNumber");

            Assert.False(result.Success);
            Assert.Equal("not_found", result.LookupStatus);
            Assert.NotNull(result.SuggestedLookupUrl);
            Assert.Contains("fallback.example", result.SuggestedLookupUrl);
        }

        [Fact]
        public async Task LookupDeviceAsync_WithUnknownSerial_FallsBackToNextProvider()
        {
            var unknownSerial = "MXQ2160G6X";

            var dellProvider = new TestProvider(
                "SerialNumber",
                "Dell",
                (serial, vendor) => Task.FromResult(new ScrapeResult())
            );

            var hpeProvider = new TestProvider(
                "SerialNumber",
                null,
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

            var result = await service.LookupDeviceAsync(unknownSerial, "SerialNumber");

            Assert.False(result.Success);
            Assert.NotNull(result.SuggestedLookupUrl);
            Assert.Contains("partsurfer.hpe.com", result.SuggestedLookupUrl);
        }

        [Fact]
        public async Task LookupDeviceAsync_ReturnFirstSuccess()
        {
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

            var result = await service.LookupDeviceAsync(serialNumber, "SerialNumber");

            Assert.True(result.Success);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task LookupDeviceAsync_PreferResultWithSuggestedUrl()
        {
            var unknownSerial = "XYZ123456789";

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
                })
            );

            var providers = new List<IHardwareLookupProvider> { hpeProvider, fallbackProvider };
            var service = new ScraperService(providers);

            var result = await service.LookupDeviceAsync(unknownSerial, "SerialNumber");

            Assert.False(result.Success);
            Assert.NotNull(result.SuggestedLookupUrl);
            Assert.Contains("partsurfer.hpe.com", result.SuggestedLookupUrl);
        }

        [Fact]
        public async Task LookupDeviceAsync_WithUpcCode_SkipsSerialProviders()
        {
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

            var result = await service.LookupDeviceAsync(upcCode, "Upc");

            Assert.True(result.Success);
        }

        [Fact]
        public async Task LookupDeviceAsync_NoProvidersCanHandle_ReturnsNotSupported()
        {
            var provider = new TestProvider(
                "UnsupportedType",
                null,
                (serial, vendor) => Task.FromResult(new ScrapeResult())
            );

            var providers = new List<IHardwareLookupProvider> { provider };
            var service = new ScraperService(providers);

            var result = await service.LookupDeviceAsync("anything", "UnknownType");

            Assert.False(result.Success);
            Assert.Equal("not_supported", result.LookupStatus);
        }

        [Fact]
        public async Task LookupDeviceAsync_PreserveProviderDetectedVendor()
        {
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

            var result = await service.LookupDeviceAsync(serial, "SerialNumber");

            Assert.Equal("Dell", result.DetectedVendor);
        }

        [Fact]
        public async Task LookupDeviceAsync_FillInDetectedVendorIfMissing()
        {
            var serial = "ABC1234";
            var provider = new TestProvider(
                "SerialNumber",
                "Dell",
                (serial, vendor) => Task.FromResult(new ScrapeResult
                {
                    Success = true,
                    Message = "Found"
                })
            );

            var providers = new List<IHardwareLookupProvider> { provider };
            var service = new ScraperService(providers);

            var result = await service.LookupDeviceAsync(serial, "SerialNumber");

            Assert.Equal("Dell", result.DetectedVendor);
        }

        [Fact]
        public async Task LookupDeviceAsync_MultipleFailures_SelectBestFallback()
        {
            var unknownSerial = "ABC1234567890";

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

            var result = await service.LookupDeviceAsync(unknownSerial, "SerialNumber");

            Assert.False(result.Success);
            Assert.NotNull(result.SuggestedLookupUrl);
            Assert.Equal("manual_lookup_required", result.LookupStatus);
            Assert.Contains("partsurfer.hpe.com", result.SuggestedLookupUrl);
        }
    }
}
