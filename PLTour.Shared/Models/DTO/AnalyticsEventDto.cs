namespace PLTour.Shared.Models.DTO
{
    public class AnalyticsEventDto
    {
        public string? SessionId { get; set; }
        public string? DeviceId { get; set; }
        public string? EventType { get; set; }
        public int? LocationId { get; set; }
        public int? TourId { get; set; }
        public string? LanguageCode { get; set; }
        public int? Duration { get; set; }
        public string? Keyword { get; set; }
        public string? Platform { get; set; }
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