using System;

namespace PLTour.Shared.Models.DTO
{
    public class AnalyticsEventDto
    {
        public string? SessionId { get; set; }
        public string? DeviceId { get; set; }

        // Các loại event gợi ý: view_location, listen_onsite, listen_remote, location_ping, listen_duration
        public string? EventType { get; set; }

        public int? LocationId { get; set; }
        public int? TourId { get; set; }
        public string? LanguageCode { get; set; }
        public int? Duration { get; set; } // Tính bằng giây
        public string? Keyword { get; set; }
        public string? Platform { get; set; } // iOS, Android...
        public bool? HasAudio { get; set; }

        // Bổ sung 2 trường này cho tính năng Heatmap và Tuyến di chuyển
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}