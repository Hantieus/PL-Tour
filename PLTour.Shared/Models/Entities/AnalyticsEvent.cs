using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PLTour.Shared.Models.Entities
{
    [Table("analytics_events")]
    public class AnalyticsEvent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string? SessionId { get; set; }

        [Required]
        public string? DeviceId { get; set; }

        [Required]
        public string? EventType { get; set; }

        public int? LocationId { get; set; }
        public int? TourId { get; set; }

        public string? LanguageCode { get; set; }

        public int? Duration { get; set; }

        public string? Keyword { get; set; }

        public string? Platform { get; set; }

        public bool? HasAudio { get; set; }

        // 2 trường này phục vụ vẽ Heatmap
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}