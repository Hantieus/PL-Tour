using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PLTour.Shared.Models;

public class Narration
{
    [Key]
    public int NarrationId { get; set; }

    [Required]
    public int LocationId { get; set; }

    [Required]
    public int LanguageId { get; set; }

    [Required]
    [StringLength(500)]
    public string Title { get; set; } = string.Empty;

    public string? Content { get; set; }

    [StringLength(500)]
    public string? AudioUrl { get; set; }

    public int Duration { get; set; }

    public bool IsDefault { get; set; }

    public int Version { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    public DateTime? UpdatedDate { get; set; }

    // Navigation properties
    [ForeignKey("LocationId")]
    public virtual Location? Location { get; set; }

    [ForeignKey("LanguageId")]
    public virtual Language? Language { get; set; }
}