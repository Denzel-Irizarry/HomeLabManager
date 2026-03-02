namespace HomeLabManager.WEBUI.Models
{
    public class DeviceResponseDataTransferObject
    {
        public Guid Id { get; set; }
        public string? SerialNumber { get; set; }
        public string? NickName { get; set; }
        public string? Location { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string? ProductName { get; set; }
        public string? ModelNumber { get; set; }
        public string? VendorName { get; set; }
    }
}
