using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PLTour.Shared.Models.Entities;

public class TourAudio
{
    [Key]
    public int TourAudioId { get; set; }

    [Required]
    public int TourId { get; set; }

    [Required]
    public int LanguageId { get; set; }

    [Required]
    [StringLength(64)]
    public string TextHash { get; set; } = string.Empty;

    [StringLength(500)]
    public string AudioUrl { get; set; } = string.Empty;

    public int Duration { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedDate { get; set; }

    [ForeignKey(nameof(TourId))]
    public virtual Tour? Tour { get; set; }

    [ForeignKey(nameof(LanguageId))]
    public virtual Language? Language { get; set; }
}
