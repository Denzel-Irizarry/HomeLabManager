using HomeLabManager.API.Models;

namespace HomeLabManager.API.Interfaces
{
    //make sure to reference as interface or will have issues accessing
    public interface ScanServiceInterface
    {
        //helpful info about async to reference 
        // https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/
        //task is represents the work being done for async processes 
        Task<string> ExtractSerialAsync(ScanRequest request);
    }
}
