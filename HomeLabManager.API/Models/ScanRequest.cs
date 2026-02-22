namespace HomeLabManager.API.Models
{
    public class ScanRequest
    {
        //https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/init
        //init is immutable so it can't be change once it is initialized
        public Stream? ImageStream { get; init;}
        public string? ManualSerial { get; init; }

        //used to verify that we are getting actual input
        public bool IsManual
        {
            get
            {
                return !string.IsNullOrWhiteSpace(ManualSerial);
            }
        }
    }
}
