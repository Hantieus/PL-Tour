namespace PLTour.Shared.Models.DTO
{
    public class AnalyticsEventDto
    {
        public string? SessionId { get; set; }
        public string? DeviceId { get; set; }
<<<<<<< HEAD
        public string? EventType { get; set; }
        public int? LocationId { get; set; }
        public int? TourId { get; set; }
        public string? LanguageCode { get; set; }
        public int? Duration { get; set; }
        public string? Keyword { get; set; }
        public string? Platform { get; set; }
=======

        // Các loại event gợi ý: view_location, listen_onsite, listen_remote, location_ping, listen_duration
        public string? EventType { get; set; }

        public int? LocationId { get; set; }
        public int? TourId { get; set; }
        public string? LanguageCode { get; set; }
        public int? Duration { get; set; } // Tính bằng giây
        public string? Keyword { get; set; }
        public string? Platform { get; set; } // iOS, Android...
>>>>>>> a6942460c2252f506ca518bb1a3d19e8baf2c802
        public bool? HasAudio { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class TopLocationDto
    {
        public int? LocationId { get; set; }
        public int PlayCount { get; set; }
    }

    public class AvgDurationDto
    {
        public int? LocationId { get; set; }
        public double AverageSeconds { get; set; }
    }

    public class HeatmapPointDto
    {
        public string? SessionId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime Timestamp { get; set; }
    }
}