using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PLTour.Shared.Models.Entities
{
    [Table("VendorImages")]
    public class VendorImage
    {
        [Key]
        public int ImageId { get; set; }

        [Required]
        public int VendorId { get; set; }

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Title { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; } = 0;

        [StringLength(50)]
        public string? ImageType { get; set; } // "logo", "menu", "dish", "restaurant"

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [ForeignKey("VendorId")]
        public virtual Vendor? Vendor { get; set; }
    }
}