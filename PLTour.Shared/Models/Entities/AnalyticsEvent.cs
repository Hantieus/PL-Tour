using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PLTour.Shared.Models.Entities
{
    [Table("analytics_events")]
    public class AnalyticsEvent
    {
        [Key]
        public int id { get; set; }

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