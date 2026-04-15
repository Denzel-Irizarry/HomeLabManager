using HomeLabManager.API.Infrastructure;
using HomeLabManager.API.Interfaces;
using HomeLabManager.API.Models;
using HomeLabManager.API.Services;
using HomeLabManager.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HomeLabManager.API.Tests.Services.Devices
{
    // These tests focus specifically on issue #24:
    // repeated saves should reuse the same vendor row after normalization.
    public class DeviceServiceVendorDedupTests
    {
        private static DeviceService CreateService(
            ApplicationDBContext dbContext,
            ScanServiceInterface scanService,
            VendorLookupInterface vendorLookup)
        {
            // Use real repository + in-memory EF context so we validate actual persistence behavior.
            var deviceRepository = new DeviceRepository(dbContext);
            return new DeviceService(scanService, vendorLookup, deviceRepository, dbContext);
        }

        [Fact]
        public async Task RegisterManualDeviceAsync_ReusesExistingVendor_ForNormalizedName()
        {
            // Arrange: seed one canonical vendor row.
            var options = new DbContextOptionsBuilder<ApplicationDBContext>()
                .UseInMemoryDatabase($"manual-vendor-dedup-{Guid.NewGuid()}")
                .Options;

            await using var dbContext = new ApplicationDBContext(options);
            var existingVendor = new Vendor
            {
                Id = Guid.NewGuid(),
                VendorName = "HPE"
            };
            dbContext.Vendors.Add(existingVendor);
            await dbContext.SaveChangesAsync();

            var service = CreateService(
                dbContext,
                new StubScanService(string.Empty),
                new StubVendorLookup(new Product()));

            var firstRequest = new ManualDeviceRegisterRequest
            {
                SerialNumber = "SERIAL-001",
                NickName = "First",
                ProductName = "Server One",
                ModelNumber = "DL360",
                VendorName = "HPE"
            };

            var secondRequest = new ManualDeviceRegisterRequest
            {
                SerialNumber = "SERIAL-002",
                NickName = "Second",
                ProductName = "Server Two",
                ModelNumber = "DL360",
                VendorName = "  hpe  "
            };

            // Act: save two devices with logically same vendor but different formatting.
            await service.RegisterManualDeviceAsync(firstRequest);
            await service.RegisterManualDeviceAsync(secondRequest);

            var vendors = await dbContext.Vendors.ToListAsync();
            var devices = await dbContext.Devices.Include(device => device.Product).ThenInclude(product => product.Vendor).ToListAsync();

            // Assert: only one vendor row exists and both devices reference it.
            Assert.Single(vendors);
            Assert.Equal("HPE", vendors[0].VendorName);
            Assert.Equal(2, devices.Count);
            Assert.All(devices, device => Assert.Equal(existingVendor.Id, device.Product!.VendorId));
        }

        [Fact]
        public async Task RegisterDeviceAsync_ReusesExistingVendor_ForScannedProduct()
        {
            // Arrange: existing canonical vendor and a scanned product carrying a padded/lowercase vendor name.
            var options = new DbContextOptionsBuilder<ApplicationDBContext>()
                .UseInMemoryDatabase($"scan-vendor-dedup-{Guid.NewGuid()}")
                .Options;

            await using var dbContext = new ApplicationDBContext(options);
            var existingVendor = new Vendor
            {
                Id = Guid.NewGuid(),
                VendorName = "Dell"
            };
            dbContext.Vendors.Add(existingVendor);
            await dbContext.SaveChangesAsync();

            var scannedProduct = new Product
            {
                Id = Guid.NewGuid(),
                ProductName = "PowerEdge",
                ModelNumber = "R640",
                VendorId = Guid.NewGuid(),
                Vendor = new Vendor
                {
                    Id = Guid.NewGuid(),
                    VendorName = "  dell  "
                }
            };

            var service = CreateService(
                dbContext,
                new StubScanService("SERIAL-SCAN-001"),
                new StubVendorLookup(scannedProduct));

            // Act: run scan registration path.
            await service.RegisterDeviceAsync(new MemoryStream(new byte[] { 1, 2, 3 }));

            var vendors = await dbContext.Vendors.ToListAsync();
            var device = await dbContext.Devices.Include(item => item.Product).ThenInclude(product => product.Vendor).SingleAsync();

            // Assert: product points to existing vendor row instead of creating a duplicate vendor.
            Assert.Single(vendors);
            Assert.Equal("Dell", vendors[0].VendorName);
            Assert.Equal(existingVendor.Id, device.Product!.VendorId);
            Assert.Equal(existingVendor.Id, device.Product.Vendor!.Id);
        }

        private sealed class StubScanService : ScanServiceInterface
        {
            private readonly string _serial;

            public StubScanService(string serial)
            {
                _serial = serial;
            }

            public Task<string> ExtractSerialAsync(ScanRequest request)
            {
                return Task.FromResult(_serial);
            }
        }

        private sealed class StubVendorLookup : VendorLookupInterface
        {
            private readonly Product _product;

            public StubVendorLookup(Product product)
            {
                _product = product;
            }

            public Task<Product> GetProductBySerialAsync(string serial)
            {
                return Task.FromResult(_product);
            }
        }
    }
}
