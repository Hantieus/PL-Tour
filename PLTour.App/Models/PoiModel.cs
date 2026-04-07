using System.ComponentModel;
using Mapsui.Styles;
using MapsuiColor = Mapsui.Styles.Color;

namespace PLTour.App.Models;

// Quy định sẵn 3 danh mục chuẩn
public static class PoiCategories
{
    public const string ThamQuan = "Tham quan";
    public const string AnUong = "Ăn uống";
    public const string SuKien = "Sự kiện";
}

public class PoiModel : INotifyPropertyChanged
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string Address { get; set; }

    // Thuộc tính Category sẽ lưu 1 trong 3 giá trị trên
    public string Category { get; set; }

    public double Lat { get; set; }
    public double Lng { get; set; }
    public double Radius { get; set; }

    public MapsuiColor PinColor { get; set; }

    private double _distanceMeters;
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

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}