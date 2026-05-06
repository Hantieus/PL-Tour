using System.Collections.Generic;
using System.ComponentModel;

namespace PLTour.App.Models;

public class TourModel : INotifyPropertyChanged
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
    public string? IntroAudioUrl { get; init; }
    public List<PoiModel> Pois { get; init; } = new List<PoiModel>();

    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string ImageUrl { get; init; } = "tour_thumb.jpg";

    public string DistanceDisplay { get; set; } = "Đang tính...";

    // --- TRẠNG THÁI PHÁT ÂM THANH (Dùng cho UX/UI) ---
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
        IsPlaying ? Microsoft.Maui.Graphics.Color.FromArgb("#FFB4A2") : Microsoft.Maui.Graphics.Color.FromArgb("#F8F9FA");

    // --- SỰ KIỆN CẬP NHẬT GIAO DIỆN ---
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}