using System.Collections.Generic;
using System.ComponentModel;
using PLTour.App.Services;

namespace PLTour.App.Models;

public class TourModel : INotifyPropertyChanged
{
    public TourModel()
    {
        LocalizationService.Instance.LanguageChanged += (_, __) =>
        {
            OnPropertyChanged(nameof(LocalizedName));
            OnPropertyChanged(nameof(LocalizedIntroText));
            OnPropertyChanged(nameof(DurationDisplay));
            OnPropertyChanged(nameof(DistanceDisplay));
            OnPropertyChanged(nameof(PlayButtonText));
            OnPropertyChanged(nameof(PlayButtonColor));
        };
    }

    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? NameEn { get; init; }
    public string? NameZh { get; init; }
    public string? NameKo { get; init; }
    public string? NameJa { get; init; }

    public string LocalizedName => PickLocalized(Name, NameEn, NameZh, NameKo, NameJa);

    // 1. Lưu tổng số phút bằng số nguyên
    public int Duration { get; init; }

    // 2. TỰ ĐỘNG TÍNH TOÁN HIỂN THỊ GIỜ / PHÚT
    public string DurationDisplay
    {
        get
        {
            if (Duration <= 0) return LocalizationService.Instance["Updating"];

            int hours = Duration / 60;
            int minutes = Duration % 60;

            if (hours > 0 && minutes > 0)
                return string.Format(LocalizationService.Instance["DurationHoursMinutes"], hours, minutes);
            else if (hours > 0)
                return string.Format(LocalizationService.Instance["DurationHours"], hours);
            else
                return string.Format(LocalizationService.Instance["DurationMinutes"], minutes);
        }
    }

    public string IntroText { get; init; } = string.Empty;
    public string? IntroTextEn { get; init; }
    public string? IntroTextZh { get; init; }
    public string? IntroTextKo { get; init; }
    public string? IntroTextJa { get; init; }
    public string LocalizedIntroText => PickLocalized(IntroText, IntroTextEn, IntroTextZh, IntroTextKo, IntroTextJa);

    public string? IntroAudioUrl { get; init; }
    public string? IntroAudioUrlEn { get; init; }
    public string? IntroAudioUrlZh { get; init; }
    public string? IntroAudioUrlKo { get; init; }
    public string? IntroAudioUrlJa { get; init; }
    public string? LocalizedIntroAudioUrl => PickLocalized(IntroAudioUrl, IntroAudioUrlEn, IntroAudioUrlZh, IntroAudioUrlKo, IntroAudioUrlJa);

    public List<PoiModel> Pois { get; set; } = new List<PoiModel>();

    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string ImageUrl { get; init; } = "tour_thumb.jpg";

    private string _distanceDisplay = LocalizationService.Instance["Calculating"];
    public string DistanceDisplay
    {
        get => _distanceDisplay;
        set
        {
            if (_distanceDisplay != value)
            {
                _distanceDisplay = value;
                OnPropertyChanged(nameof(DistanceDisplay));
            }
        }
    }

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

    public string PlayButtonText => IsPlaying ? LocalizationService.Instance["Stop"] : LocalizationService.Instance["Listen"];

    public Microsoft.Maui.Graphics.Color PlayButtonColor =>
        IsPlaying ? Microsoft.Maui.Graphics.Color.FromArgb("#FFB4A2") : Microsoft.Maui.Graphics.Color.FromArgb("#F8F9FA");

    private static string PickLocalized(string vi, string? en = null, string? zh = null, string? ko = null, string? ja = null)
    {
        var lang = LocalizationService.Instance.CurrentLanguageCode;
        return lang switch
        {
            "en" when !string.IsNullOrWhiteSpace(en) => en!,
            "zh" when !string.IsNullOrWhiteSpace(zh) => zh!,
            "ko" when !string.IsNullOrWhiteSpace(ko) => ko!,
            "ja" when !string.IsNullOrWhiteSpace(ja) => ja!,
            _ => vi
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
