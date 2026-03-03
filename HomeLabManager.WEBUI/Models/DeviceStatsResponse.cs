namespace HomeLabManager.WEBUI.Models
{
    public class DeviceStatsResponse
    {
        // This class represents the response model for device statistics, this is where the api controller will return the data to the frontend
        public int TotalDevices { get; set; }
        public int WithSerialNumber { get; set; }
        public int WithoutSerialNumber { get; set; }
        public int WithNickName { get; set; }
        public int WithoutNickName { get; set; }
        public int WithLocation { get; set; }
        public int WithoutLocation { get; set; }
        public DateTime? LastAddedUtc { get; set; }
    }
}
