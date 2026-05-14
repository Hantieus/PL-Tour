using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using PLTour.App.Services;
using PLTour.Shared.Models.DTO;
using Mapsui.Styles;
using MapsuiColor = Mapsui.Styles.Color;

namespace PLTour.App.Models;

// Quy định sẵn các danh mục chuẩn để lọc dữ liệu trên bản đồ
public static class PoiCategories
{
    public const string ThamQuan = "Tham quan";
    public const string AnUong = "Ăn uống";
    public const string SuKien = "Sự kiện";
}

public class PoiModel : INotifyPropertyChanged, IDisposable
{
    private bool _disposed;

    public PoiModel()
    {
        LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(LocalizedName));
        OnPropertyChanged(nameof(LocalizedDescription));
        OnPropertyChanged(nameof(LocalizedCategory));
        OnPropertyChanged(nameof(LocalizedFullContent));
        OnPropertyChanged(nameof(DistanceText));
        OnPropertyChanged(nameof(PlayButtonText));
    }

    // 1. --- THÔNG TIN TỪ API ---
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string? NameZh { get; set; }
    public string? NameKo { get; set; }
    public string? NameJa { get; set; }

    public string Description { get; set; } = string.Empty;
    public string? DescriptionEn { get; set; }
    public string? DescriptionZh { get; set; }
    public string? DescriptionKo { get; set; }
    public string? DescriptionJa { get; set; }

    public string ImageUrl { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int VendorId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? CategoryEn { get; set; }
    public string? CategoryZh { get; set; }
    public string? CategoryKo { get; set; }
    public string? CategoryJa { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double Radius { get; set; }
    public MapsuiColor PinColor { get; set; }

    // --- DỮ LIỆU THUYẾT MINH ---
    public int NarrationId { get; set; }
    public string AudioUrl { get; set; } = string.Empty;
    public string? AudioUrlEn { get; set; }
    public string? AudioUrlZh { get; set; }
    public string? AudioUrlKo { get; set; }
    public string? AudioUrlJa { get; set; }
    public string FullContent { get; set; } = string.Empty;
    public string? FullContentEn { get; set; }
    public string? FullContentZh { get; set; }
    public string? FullContentKo { get; set; }
    public string? FullContentJa { get; set; }
    public string LanguageName { get; set; } = string.Empty;
    public int LanguageId { get; set; }
    public string LanguageCode { get; set; } = string.Empty;

    public string LocalizedName => PickLocalized(Name, NameEn, NameZh, NameKo, NameJa);
    public string LocalizedDescription => PickLocalized(Description, DescriptionEn, DescriptionZh, DescriptionKo, DescriptionJa);
    public string LocalizedCategory => PickLocalized(Category, CategoryEn, CategoryZh, CategoryKo, CategoryJa);
    public string LocalizedFullContent => PickLocalized(FullContent, FullContentEn, FullContentZh, FullContentKo, FullContentJa);
    public string LocalizedAudioUrl => PickLocalized(AudioUrl, AudioUrlEn, AudioUrlZh, AudioUrlKo, AudioUrlJa);

    private ObservableCollection<ProductDto> _storeProducts = new();
    public ObservableCollection<ProductDto> StoreProducts
    {
        get => _storeProducts;
        set
        {
            if (_storeProducts != value)
            {
                _storeProducts = value;
                OnPropertyChanged(nameof(StoreProducts));
            }
        }
    }

    public void SetStoreProducts(IEnumerable<ProductDto> products)
    {
        StoreProducts = new ObservableCollection<ProductDto>(products);
    }

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

    // 2. --- KHOẢNG CÁCH ---
    private double _distanceMeters;
    private string _address = string.Empty;

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
            if (DistanceMeters <= 0) return LocalizationService.Instance["LocationUnknown"];
            return DistanceMeters < 1000 ? $"{Math.Round(DistanceMeters)} m" : $"{(DistanceMeters / 1000.0):F1} km";
        }
    }

    // 3. --- TRẠNG THÁI PHÁT ÂM THANH ---
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

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }

    public string PlayButtonText => IsPlaying ? LocalizationService.Instance["Stop"] : LocalizationService.Instance["Listen"];
    public Microsoft.Maui.Graphics.Color PlayButtonColor => IsPlaying ? Microsoft.Maui.Graphics.Color.FromArgb("#FFB4A2") : Microsoft.Maui.Graphics.Color.FromArgb("#E9ECEF");

    public event PropertyChangedEventHandler? PropertyChanged;
    public void RaiseLocalizedChanged()
    {
        OnPropertyChanged(nameof(LocalizedName));
        OnPropertyChanged(nameof(LocalizedDescription));
        OnPropertyChanged(nameof(LocalizedCategory));
        OnPropertyChanged(nameof(LocalizedFullContent));
        OnPropertyChanged(nameof(LocalizedAudioUrl));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        LocalizationService.Instance.LanguageChanged -= OnLanguageChanged;
    }

    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
