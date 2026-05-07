using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PLTour.Shared.Models.Entities;

public class Category
{
    [Key]
    public int CategoryId { get; set; }

    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    // Giữ lại để tương thích code cũ nhưng không map ra database
    [NotMapped]
    public string CategoryName
    {
        get => Name;
        set => Name = value;
    }

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [StringLength(50)]
    public string Icon { get; set; } = string.Empty; // Class icon (FontAwesome, etc.)

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation property
    public virtual ICollection<Location>? Locations { get; set; }
    public virtual ICollection<Vendor>? Vendors { get; set; }
}