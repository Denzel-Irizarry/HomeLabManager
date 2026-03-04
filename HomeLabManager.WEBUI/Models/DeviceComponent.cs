namespace HomeLabManager.WEBUI.Models
{
    public class DeviceComponent
    {
        public Guid Id { get; set; }
        public Guid DeviceId { get; set; }
        public Guid ComponentId { get; set; }
        public string? SerialNumber { get; set; }
        public DateTime? InstalledDate { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAtUtc { get; set; }

        // For display purposes this will be populated by joining with Components
        public string? ComponentName { get; set; }
        public string? ComponentType { get; set; }
        public string? Manufacturer { get; set; }
    }
}
