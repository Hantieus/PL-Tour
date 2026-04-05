using System;
using System.ComponentModel;

namespace PLTour.Share.Models
{
    public class PoiModel : INotifyPropertyChanged
    {
        public int Id { get; set; } // Thêm Id để sau này làm việc với Database/API
        public double Lat { get; set; }
        public double Lng { get; set; }
        public double Radius { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Address { get; set; }

        // ✅ Dùng chuỗi Hex (VD: "#FF0000") thay vì Mapsui.Color hay Maui.Color
        public string PinColorHex { get; set; }
        public string CategoryColorHex { get; set; }

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
                    OnPropertyChanged(nameof(DistanceText)); // Cập nhật luôn text hiển thị
                }
            }
        }

        public string DistanceText
        {
            get
            {
                if (DistanceMeters <= 0) return "Đang đo...";
                if (DistanceMeters < 1000)
                    return $"{Math.Round(DistanceMeters)} m";
                else
                    return $"{(DistanceMeters / 1000.0):F1} km";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}