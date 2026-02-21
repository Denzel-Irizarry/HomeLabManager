using HomeLabManager.API.Interfaces;

namespace HomeLabManager.API.Infrastructure
{
    //need to set up as actual classes for testing or it will not build because nothing happens
    public class FakeScanServiceTesting:ScanServiceInterface
    {
        public Task<string> ExtractSerialAsync(Stream imageStream)
        {
            //simulates that we scanned a barcode for testing
            return Task.FromResult("DemoSerialNumber1234");
        }
    }
}
