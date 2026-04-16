namespace HomeLabManager.API.Models
{
    public class VendorDeduplicationResponse
    {
        public int DuplicateGroupsFound { get; set; }
        public int VendorsRemoved { get; set; }
        public int ProductReferencesUpdated { get; set; }
        public int CanonicalUrlsUpdated { get; set; }
    }
}
