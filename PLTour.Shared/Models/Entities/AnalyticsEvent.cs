using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PLTour.Shared.Models.Entities
{
    [Table("analytics_events")]
    public class AnalyticsEvent
    {
        [Key]
        public int id { get; set; }

<<<<<<< HEAD
        [Column("session_id")]
        [StringLength(255)]
        public string? session_id { get; set; }

        [Column("device_id")]
        [StringLength(255)]
        public string? device_id { get; set; }

        [Column("event_type")]
        [StringLength(50)]
        public string? event_type { get; set; }

        [Column("location_id")]
        public int? location_id { get; set; }
=======
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
>>>>>>> a6942460c2252f506ca518bb1a3d19e8baf2c802

        [Column("tour_id")]
        public int? tour_id { get; set; }

        [Column("language_code")]
        [StringLength(10)]
        public string? language_code { get; set; }

        [Column("duration")]
        public int? duration { get; set; }

        [Column("keyword")]
        [StringLength(255)]
        public string? keyword { get; set; }

        [Column("platform")]
        [StringLength(50)]
        public string? platform { get; set; }

        [Column("has_audio")]
        public bool? has_audio { get; set; }

        [Column("latitude")]
        public double? latitude { get; set; }

        [Column("longitude")]
        public double? longitude { get; set; }

        [Column("timestamp")]
        public DateTime timestamp { get; set; }
    }
}