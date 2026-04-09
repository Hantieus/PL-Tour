using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PLTour.Shared.Models.Entities;

public class TourLocation
{
    [Key, Column(Order = 0)]
    public int TourId { get; set; }

    [Key, Column(Order = 1)]
    public int LocationId { get; set; }

    public int OrderIndex { get; set; } // Thứ tự điểm đến trong Tour (1, 2, 3...)

    // Navigation properties
    [ForeignKey("TourId")]  
    public virtual Tour Tour { get; set; }

    [ForeignKey("LocationId")]
    public virtual Location Location { get; set; }
}