using System.Collections.Generic;

namespace PLTour.App.Models;

public class TourModel
{
    // Các thông tin cơ bản của Tour
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty; // Ví dụ: "1 tiếng 45 phút"
    public string IntroText { get; set; } = string.Empty; // Nội dung thuyết minh

    // Danh sách các điểm tham quan trong Tour (Sử dụng PoiModel từ file riêng)
    public List<PoiModel> Pois { get; set; } = new List<PoiModel>();

    // --- CÁC THUỘC TÍNH BỔ SUNG ĐỂ HIỂN THỊ TRÊN HOME PAGE ---

    // Tọa độ đại diện của Tour (thường lấy tọa độ của POI đầu tiên)
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    // Chuỗi hiển thị khoảng cách (Ví dụ: "Cách bạn: 1.2 km")
    // Thuộc tính này sẽ được tính toán và gán giá trị tại HomePage.xaml.cs
    public string DistanceDisplay { get; set; } = "Đang tính...";

    // Hình ảnh đại diện cho Tour
    public string ImageUrl { get; set; } = "tour_thumb.jpg";
}