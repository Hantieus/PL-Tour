using System.ComponentModel.DataAnnotations;

namespace PLTour.Shared.Models;

public class Category
{
    [Key]
    public int CategoryId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [StringLength(500)]
    public string Description { get; set; }

    [StringLength(50)]
    public string Icon { get; set; } // Class icon (FontAwesome, etc.)

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation property
    public virtual ICollection<Location> Locations { get; set; }
    public virtual ICollection<Vendor> Vendors { get; set; }
}