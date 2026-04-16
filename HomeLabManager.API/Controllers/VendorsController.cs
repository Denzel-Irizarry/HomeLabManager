using HomeLabManager.API.Infrastructure;
using HomeLabManager.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HomeLabManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VendorsController : ControllerBase
    {
        private readonly ApplicationDBContext _dbContext;
        private readonly ILogger<VendorsController> _logger;

        public VendorsController(ApplicationDBContext dbContext, ILogger<VendorsController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<List<VendorSummaryResponse>>> GetVendors()
        {
            try
            {
                // Step 1: count how many products reference each vendor.
                var productCountsByVendor = await _dbContext.Products
                    .AsNoTracking()
                    .GroupBy(product => product.VendorId)
                    .Select(group => new
                    {
                        VendorId = group.Key,
                        ProductCount = group.Count()
                    })
                    .ToDictionaryAsync(item => item.VendorId, item => item.ProductCount);

                // Step 2: count devices and compute "last seen" timestamp for each vendor.
                // Devices point to Products, and Products point to Vendors, so this is a join.
                var deviceStatsByVendor = await _dbContext.Devices
                    .AsNoTracking()
                    .Join(
                        _dbContext.Products.AsNoTracking(),
                        device => device.ProductId,
                        product => product.Id,
                        (device, product) => new
                        {
                            product.VendorId,
                            device.CreatedAtUtc
                        })
                    .GroupBy(item => item.VendorId)
                    .Select(group => new
                    {
                        VendorId = group.Key,
                        DeviceCount = group.Count(),
                        LastSeenUtc = group.Max(item => (DateTime?)item.CreatedAtUtc)
                    })
                    .ToDictionaryAsync(
                        item => item.VendorId,
                        item => new
                        {
                            item.DeviceCount,
                            item.LastSeenUtc
                        });

                // Step 3: load the base vendor list and sort for stable UI ordering.
                var vendors = await _dbContext.Vendors
                    .AsNoTracking()
                    .OrderBy(vendor => vendor.VendorName)
                    .ToListAsync();

                // Step 4: merge all stats into one response per vendor.
                var response = vendors.Select(vendor =>
                {
                    productCountsByVendor.TryGetValue(vendor.Id, out var productCount);
                    var hasDeviceStats = deviceStatsByVendor.TryGetValue(vendor.Id, out var deviceStats);

                    return new VendorSummaryResponse
                    {
                        Id = vendor.Id,
                        VendorName = vendor.VendorName,
                        VendorBaseUrl = vendor.VendorBaseUrl,
                        ProductCount = productCount,
                        DeviceCount = hasDeviceStats ? deviceStats!.DeviceCount : 0,
                        LastSeenUtc = hasDeviceStats ? deviceStats!.LastSeenUtc : null
                    };
                }).ToList();

                // Return card/table-ready data to the WebUI vendors page.
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving vendors.");
                return StatusCode(500, "An error occurred while retrieving vendors.");
            }
        }

        [HttpPost("deduplicate")]
        public async Task<ActionResult<VendorDeduplicationResponse>> DeduplicateVendors()
        {
            try
            {
                var vendors = await _dbContext.Vendors.ToListAsync();
                var products = await _dbContext.Products.ToListAsync();

                var vendorGroups = vendors
                    .GroupBy(vendor => NormalizeVendorName(vendor.VendorName))
                    .Where(group => group.Count() > 1)
                    .ToList();

                var result = new VendorDeduplicationResponse
                {
                    DuplicateGroupsFound = vendorGroups.Count
                };

                if (vendorGroups.Count == 0)
                {
                    return Ok(result);
                }

                await using var transaction = await _dbContext.Database.BeginTransactionAsync();

                foreach (var group in vendorGroups)
                {
                    var orderedGroup = group.OrderBy(vendor => vendor.Id).ToList();
                    var canonicalVendor = orderedGroup[0];
                    var duplicateVendors = orderedGroup.Skip(1).ToList();

                    if (string.IsNullOrWhiteSpace(canonicalVendor.VendorBaseUrl))
                    {
                        var bestUrl = duplicateVendors
                            .Select(vendor => vendor.VendorBaseUrl)
                            .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));

                        if (!string.IsNullOrWhiteSpace(bestUrl))
                        {
                            canonicalVendor.VendorBaseUrl = bestUrl;
                            result.CanonicalUrlsUpdated++;
                        }
                    }

                    var duplicateIds = duplicateVendors.Select(vendor => vendor.Id).ToHashSet();

                    foreach (var product in products.Where(product => duplicateIds.Contains(product.VendorId)))
                    {
                        product.VendorId = canonicalVendor.Id;
                        result.ProductReferencesUpdated++;
                    }

                    _dbContext.Vendors.RemoveRange(duplicateVendors);
                    result.VendorsRemoved += duplicateVendors.Count;
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deduplicating vendors.");
                return StatusCode(500, "An error occurred while deduplicating vendors.");
            }
        }

        private static string NormalizeVendorName(string? vendorName)
        {
            if (string.IsNullOrWhiteSpace(vendorName))
            {
                return "UNKNOWN VENDOR";
            }

            return vendorName.Trim().ToUpperInvariant();
        }
    }
}
