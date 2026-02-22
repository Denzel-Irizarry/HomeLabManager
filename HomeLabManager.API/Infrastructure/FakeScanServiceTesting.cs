using HomeLabManager.API.Interfaces;
using HomeLabManager.API.Models;

namespace HomeLabManager.API.Infrastructure
{
    //need to set up as actual classes for testing or it will not build because nothing happens
    public class FakeScanServiceTesting:ScanServiceInterface
    {
        public Task<string> ExtractSerialAsync(ScanRequest scanRequest)
        {
            //simulates that we scanned a barcode for testing
            return Task.FromResult("DemoSerialNumber1234");
        }
    }
}
