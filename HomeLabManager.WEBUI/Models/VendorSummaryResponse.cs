namespace HomeLabManager.WEBUI.Models
{
    public class VendorSummaryResponse
    {
        public Guid Id { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public string? VendorBaseUrl { get; set; }
        public int ProductCount { get; set; }
        public int DeviceCount { get; set; }
        public DateTime? LastSeenUtc { get; set; }
    }
}