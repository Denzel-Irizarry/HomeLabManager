namespace HomeLabManager.API.Models
{
    public class UpdateDeviceRequest
    {
        public string? SerialNumber { get; set; }
        public string? NickName { get; set; }
        public string? Location { get; set; }

        public string? ProductName { get; set; }
        public string? ModelNumber { get; set; }
        public string? VendorName { get; set; }
    }
}
