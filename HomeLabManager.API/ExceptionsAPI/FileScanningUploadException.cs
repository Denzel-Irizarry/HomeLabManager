namespace HomeLabManager.API.ExceptionsAPI
{
    public class FileScanningUploadException : Exception
    {
        public FileScanningUploadException() : base("File not Found.") { }
        public FileScanningUploadException(string message) : base(message) { }
        public FileScanningUploadException(string message, Exception innerException) : base(message, innerException) { }
    }
}
