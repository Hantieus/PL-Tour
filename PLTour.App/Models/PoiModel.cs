using System.ComponentModel;
using Mapsui.Styles;
using MapsuiColor = Mapsui.Styles.Color;

namespace PLTour.App.Models;

// Quy định sẵn các danh mục chuẩn để lọc dữ liệu trên bản đồ
public static class PoiCategories
{
    public const string ThamQuan = "Điểm tham quan";
    public const string AnUong = "Địa điểm ăn uống";
    public const string SuKien = "Sự kiện";
}

public class PoiModel : INotifyPropertyChanged
{
    // 1. --- THÔNG TIN TỪ API (CHỈ LẤY NHỮNG GÌ THỰC SỰ CẦN) ---
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; } // Mô tả ngắn (hiện trên danh sách)
    public string ImageUrl { get; set; }
    public string Category { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double Radius { get; set; }
    public MapsuiColor PinColor { get; set; }

    // --- DỮ LIỆU THUYẾT MINH (Lọc từ NarrationDto theo Ngôn ngữ) ---
    public int NarrationId { get; set; }
    public string AudioUrl { get; set; }    // Link MP3 từ Admin (Ưu tiên 1)
    public string FullContent { get; set; }  // Nội dung chi tiết để đọc TTS (Ưu tiên 2)
    public string LanguageName { get; set; } // Tên ngôn ngữ đang dùng (Ví dụ: Tiếng Việt)

    // THÊM MỚI: Lấy thêm ID và Code của ngôn ngữ
    public int LanguageId { get; set; }
    public string LanguageCode { get; set; }

    // 2. --- LOGIC XỬ LÝ KHOẢNG CÁCH (Tự động cập nhật giao diện) ---
    private double _distanceMeters;
    private string _address;

    public string Address
    {
        get => _address;
        set { _address = value; OnPropertyChanged(nameof(Address)); }
    }

    public double DistanceMeters
    {
        get => _distanceMeters;
        set
        {
            if (_distanceMeters != value)
            {
                _distanceMeters = value;
                OnPropertyChanged(nameof(DistanceMeters));
                OnPropertyChanged(nameof(DistanceText));
            }
        }
    }

    public string DistanceText
    {
        get
        {
            if (DistanceMeters <= 0) return "Đang đo...";
            return DistanceMeters < 1000
                ? $"{Math.Round(DistanceMeters)} m"
                : $"{(DistanceMeters / 1000.0):F1} km";
        }
    }

    // 3. --- TRẠNG THÁI PHÁT ÂM THANH (Dùng cho UX/UI) ---
    private bool _isPlaying;
    public bool IsPlaying
    {
        get => _isPlaying;
        set
        {
            if (_isPlaying != value)
            {
                _isPlaying = value;
                OnPropertyChanged(nameof(IsPlaying));
                OnPropertyChanged(nameof(PlayButtonText));
                OnPropertyChanged(nameof(PlayButtonColor));
            }
        }
    }

    // Tự động đổi chữ trên nút bấm
    public string PlayButtonText => IsPlaying ? "⏸️ Dừng" : "🔊 Nghe";

    // Tự động đổi màu nút khi đang phát (Cam nhạt khi phát, Xám nhẹ khi chờ)
    public Microsoft.Maui.Graphics.Color PlayButtonColor =>
        IsPlaying ? Microsoft.Maui.Graphics.Color.FromArgb("#FFB4A2") : Microsoft.Maui.Graphics.Color.FromArgb("#E9ECEF");

    // --- SỰ KIỆN CẬP NHẬT GIAO DIỆN ---
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}