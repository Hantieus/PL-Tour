using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PLTour.Shared.Models.Entities
{
    [Table("analytics_events")]
    public class AnalyticsEvent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Column("session_id")]
        [StringLength(255)]
        public string? SessionId { get; set; }

        [Column("device_id")]
        [StringLength(255)]
        public string? DeviceId { get; set; }

        [Column("event_type")]
        [StringLength(50)]
        public string? EventType { get; set; }

        [Column("location_id")]
        public int? LocationId { get; set; }

        [Column("tour_id")]
        public int? TourId { get; set; }

        [Column("language_code")]
        [StringLength(10)]
        public string? LanguageCode { get; set; }

        [Column("duration")]
        public int? Duration { get; set; }

        [Column("keyword")]
        [StringLength(255)]
        public string? Keyword { get; set; }

        [Column("platform")]
        [StringLength(50)]
        public string? Platform { get; set; }

        [Column("has_audio")]
        public bool? HasAudio { get; set; }

        [Column("latitude")]
        public double? Latitude { get; set; }

        [Column("longitude")]
        public double? Longitude { get; set; }

        [Column("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}