namespace HomeLabManager.API.Models
{
    public class DeviceStatsResponse
    {
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
