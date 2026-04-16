using System;

namespace PLTour.Share.Models
{
    public class AnalyticsEventDto
    {
        public string SessionId { get; set; }
        public string DeviceId { get; set; }
        public string EventType { get; set; } // view_location, listen_narration, start_tour...

        public int? LocationId { get; set; }
        public int? TourId { get; set; }
        public string LanguageCode { get; set; }
        public int? Duration { get; set; } // Tính bằng giây
        public string Keyword { get; set; }
        public string Platform { get; set; } // iOS, Android...
        public bool? HasAudio { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}