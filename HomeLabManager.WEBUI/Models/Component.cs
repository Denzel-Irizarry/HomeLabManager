using System.ComponentModel.DataAnnotations;

namespace HomeLabManager.WEBUI.Models
{
    //model to match the Component entity in the database, used for data transfer between the UI and the backend
    public class Component
    {
        public Guid Id { get; set; }
        [Required(ErrorMessage = "Component name is required.")]
        public string Name { get; set; }= string.Empty;

        public string? ComponentType { get; set; }
        public string? Manufacturer { get; set; }
        public string? ModelNumber { get; set; }
        public string? Specifications { get; set; }
        
        [Range(0.001, 999999.99, ErrorMessage = "Price must be between $0.01 and $999,999.99.")]
        public decimal? UnitPrice { get; set; }
        public Guid? VendorId { get; set; }
        public DateTime CreatedAtUtc { get; set; }

    }
}
