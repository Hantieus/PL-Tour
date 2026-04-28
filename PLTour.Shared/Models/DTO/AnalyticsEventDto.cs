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
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class TimelinePointDto
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }

    public class BreakdownDto
    {
        public string EventType { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class TopLocationDetailDto
    {
        public int LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public int ListenCount { get; set; }
        public int TotalDuration { get; set; }
        public double AvgDuration { get; set; }
        public string TopLanguage { get; set; } = "N/A";
    }

    public class OverviewDto
    {
        public int TotalEvents { get; set; }
        public int UniqueDevices { get; set; }
        public int UniqueSessions { get; set; }
        public int TotalListens { get; set; }
        public double AvgDurationSeconds { get; set; }
        public int OnsiteCount { get; set; }
        public int RemoteCount { get; set; }
    }

    public class HeatmapPointDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Weight { get; set; } = 1;
    }
}