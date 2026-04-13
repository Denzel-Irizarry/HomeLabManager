namespace HomeLabManager.WEBUI.Models
{
    public class ManualDeviceRegisterRequest
    {
        public string? SerialNumber { get; set; }
        public string? NickName { get; set; }
        public string? Location { get; set; }
        public string? VendorName { get; set; }
        public string? ProductName { get; set; }
        public string? ModelNumber { get; set; }
    }
}
