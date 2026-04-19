using System.Collections.Generic;

namespace PLTour.App.Models;

public class TourModel
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;

    // 1. Lưu tổng số phút bằng số nguyên
    public int Duration { get; init; }

    // 2. TỰ ĐỘNG TÍNH TOÁN HIỂN THỊ GIỜ / PHÚT
    public string DurationDisplay
    {
        get
        {
            if (Duration <= 0) return "Đang cập nhật";

            int hours = Duration / 60;
            int minutes = Duration % 60;

            if (hours > 0 && minutes > 0)
                return $"{hours} tiếng {minutes} phút";
            else if (hours > 0)
                return $"{hours} tiếng";
            else
                return $"{minutes} phút";
        }
    }

    public string IntroText { get; init; } = string.Empty;
    public List<PoiModel> Pois { get; init; } = new List<PoiModel>();

    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string ImageUrl { get; init; } = "tour_thumb.jpg";

    public string DistanceDisplay { get; set; } = "Đang tính...";
}