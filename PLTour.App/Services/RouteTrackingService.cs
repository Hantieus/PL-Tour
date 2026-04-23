using Microsoft.Maui.Devices.Sensors;
using PLTour.App.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PLTour.App.Services
{
    public class RouteTrackingService
    {
        // Cấu hình thuật toán xác định điểm dừng
        private const int DWELL_TIME_MINUTES = 5; // Thời gian dừng tối thiểu để tính là một "Visit"

        private DateTime? _stayStartTime;
        private int? _lastPoiInsideId;
        private readonly HashSet<int> _recordedVisits = new();

        /// <summary>
        /// Xử lý vị trí hiện tại để xác định lộ trình và các điểm dừng (POI Visit)
        /// </summary>
        public async Task ProcessLocationForRoute(Location userLoc, List<PoiModel> allPois)
        {
            if (userLoc == null || allPois == null || !allPois.Any()) return;

            // 1. Tìm POI gần nhất mà người dùng đang đứng trong bán kính (Radius)
            // SỬA LỖI: Dùng Kilometers rồi nhân 1000 vì Sensors không hỗ trợ trực tiếp Meters
            var currentPoi = allPois.FirstOrDefault(p =>
            {
                var poiLoc = new Location(p.Lat, p.Lng);
                double distanceInKm = Location.CalculateDistance(userLoc, poiLoc, DistanceUnits.Kilometers);
                return (distanceInKm * 1000) <= p.Radius;
            });

            if (currentPoi != null)
            {
                // Nếu đây là lần đầu tiên bước vào vùng POI này hoặc chuyển từ POI khác sang
                if (_lastPoiInsideId != currentPoi.Id)
                {
                    _stayStartTime = DateTime.UtcNow;
                    _lastPoiInsideId = currentPoi.Id;
                }
                else // Nếu vẫn đang duy trì ở trong vùng POI này
                {
                    if (_stayStartTime.HasValue)
                    {
                        var dwellTime = (DateTime.UtcNow - _stayStartTime.Value).TotalMinutes;

                        // XÁC ĐỊNH ĐIỂM DỪNG: Ở lại > 5 phút và chưa được ghi nhận trong phiên này
                        if (dwellTime >= DWELL_TIME_MINUTES && !_recordedVisits.Contains(currentPoi.Id))
                        {
                            await RecordVisitAsync(currentPoi, userLoc);
                        }
                    }
                }
            }
            else
            {
                // Nếu đã đi ra khỏi vùng POI (Loại bỏ nhiễu khi đi ngang qua nhanh)
                _stayStartTime = null;
                _lastPoiInsideId = null;
            }
        }

        /// <summary>
        /// Ghi nhận một điểm dừng chân vào hệ thống (Local Cache & Server)
        /// </summary>
        public async Task RecordVisitAsync(PoiModel poi, Location loc)
        {
            if (_recordedVisits.Contains(poi.Id)) return;

            _recordedVisits.Add(poi.Id);

            // 1. Local Cache: Lưu vào SQLite (Hỗ trợ Offline Mode)
            var history = new PoiHistory
            {
                Name = poi.Name,
                Time = DateTime.UtcNow,
                Lat = loc.Latitude,
                Lng = loc.Longitude
            };

            // TODO: Gọi DatabaseContext để Insert history vào SQLite 

            // 2. Sync Service: Đẩy dữ liệu lên Server (Heatmap Engine)
            // Tracking xem chi tiết POI tự động khi xác định có dừng chân
            await AnalyticsService.Instance.TrackPoiViewAsync(poi.Id);

            System.Diagnostics.Debug.WriteLine($"[ROUTE_TRACKER] Đã ghi nhận điểm dừng chân: {poi.Name}");
        }

        /// <summary>
        /// Reset danh sách đã ghi nhận khi kết thúc một Tour hoặc bắt đầu ngày mới
        /// </summary>
        public void ResetTracking()
        {
            _recordedVisits.Clear();
            _stayStartTime = null;
            _lastPoiInsideId = null;
        }
    }
}