using HomeLabManager.API.Interfaces;
using HomeLabManager.Core.Entities;

namespace HomeLabManager.API.Infrastructure
{
    //need to set up as actual classes for testing or it will not build because nothing happens
    public class FakeVendorLookupTest:VendorLookupInterface
    {
        //simulating we got the product information from vendor
        public Task<Product> GetProductBySerialAsync(string serial)
        {
            var vendor = new Vendor
            {
                Id = Guid.NewGuid(),
                VendorName = "DemoVendor"
            };

            var product = new Product
            {
                Id = Guid.NewGuid(),
                ModelNumber = "Model-X",
                ProductName = "Demo Server",
                VendorId = vendor.Id,
                Vendor = vendor
            };

            return Task.FromResult(product);
        }
    }
}
