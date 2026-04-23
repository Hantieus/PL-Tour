using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Mapsui.Styles;
using MapsuiColor = Mapsui.Styles.Color;
using Microsoft.Maui.Graphics;
using PLTour.Shared.Models.DTO;

namespace PLTour.App.Models
{
    // Quy định sẵn các danh mục chuẩn để lọc dữ liệu trên bản đồ
    public static class PoiCategories
    {
        public const string ThamQuan = "Điểm tham quan";
        public const string AnUong = "Địa điểm ăn uống";
        public const string SuKien = "Sự kiện";
    }

    public class PoiModel : INotifyPropertyChanged
    {
        // 1. --- THÔNG TIN TỪ API ---
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = "tour_thumb.jpg";

        public int CategoryId { get; set; }
        private string _category = string.Empty;
        public string Category
        {
            get => _category;
            set
            {
                if (_category != value)
                {
                    _category = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsDiningCategory));
                }
            }
        }

        public double Lat { get; set; }
        public double Lng { get; set; }
        public double Radius { get; set; }
        public MapsuiColor PinColor { get; set; } = MapsuiColor.Blue;

        // --- DỮ LIỆU VENDOR & MENU ---
        public bool IsDiningCategory => Category == PoiCategories.AnUong;
        public int? VendorId { get; set; }

        private ObservableCollection<ProductDto> _menuItems = new();
        public ObservableCollection<ProductDto> MenuItems
        {
            get => _menuItems;
            set
            {
                _menuItems = value;
                OnPropertyChanged();
            }
        }

        private bool _isLoadingMenu;
        public bool IsLoadingMenu
        {
            get => _isLoadingMenu;
            set { _isLoadingMenu = value; OnPropertyChanged(); }
        }

        // --- DỮ LIỆU THUYẾT MINH ---
        public int NarrationId { get; set; }
        public string? AudioUrl { get; set; }
        public string FullContent { get; set; } = string.Empty;
        public string LanguageName { get; set; } = "Tiếng Việt";
        public int LanguageId { get; set; }
        public string LanguageCode { get; set; } = "vi";

        // 2. --- LOGIC XỬ LÝ KHOẢNG CÁCH & ĐỊA CHỈ ---
        private double _distanceMeters;
        private string _address = string.Empty;

        public string Address
        {
            get => _address;
            set { _address = value; OnPropertyChanged(); }
        }

        public double DistanceMeters
        {
            get => _distanceMeters;
            set
            {
                if (Math.Abs(_distanceMeters - value) > 0.1) // Tránh cập nhật quá liên tục nếu sai số nhỏ
                {
                    _distanceMeters = value;
                    OnPropertyChanged();
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
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PlayButtonText));
                    OnPropertyChanged(nameof(PlayButtonColor));
                }
            }
        }

        public string PlayButtonText => IsPlaying ? "⏸️ Dừng" : "🔊 Nghe";

        // Sử dụng Microsoft.Maui.Graphics.Color rõ ràng để tránh nhầm với Mapsui.Styles.Color
        public Microsoft.Maui.Graphics.Color PlayButtonColor =>
            IsPlaying ? Microsoft.Maui.Graphics.Color.FromArgb("#FFB4A2") : Microsoft.Maui.Graphics.Color.FromArgb("#E9ECEF");

        // --- SỰ KIỆN CẬP NHẬT GIAO DIỆN ---
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}