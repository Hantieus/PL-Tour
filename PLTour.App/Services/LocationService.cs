using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Storage;
using System;
using System.Threading.Tasks;

namespace PLTour.App.Services;

public class LocationService
{
    // THÊM BIẾN NÀY ĐỂ FIX LỖI: Thuộc tính này sẽ tự động đọc/ghi vào bộ nhớ máy (Preferences)
    public Location CurrentLocation
    {
        get => GetSavedLocation(); // Khi gọi lấy vị trí, tự động đọc từ Preferences
        set
        {
            if (value != null)
            {
                Preferences.Default.Set("UserLat", value.Latitude);
                Preferences.Default.Set("UserLng", value.Longitude);
            }
        }
    }

    // Hàm 1: Lấy vị trí thuần túy từ GPS
    public async Task<Location> GetCurrentLocationAsync()
    {
        try
        {
            // Kiểm tra quyền trước để tránh hỏi lại nếu đã cấp
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted) return null;
            }

            // Yêu cầu lấy GPS với độ chính xác vừa phải, timeout 5 giây
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5));
            var location = await Geolocation.Default.GetLocationAsync(request);

            // Tự động lưu vào CurrentLocation (và Preferences) nếu lấy thành công
            if (location != null)
            {
                CurrentLocation = location;
            }

            return location;
        }
        catch
        {
            return null;
        }
    }

    // Hàm 2: Vừa lấy từ GPS vừa lưu vào bộ nhớ máy (Preferences)
    public async Task<Location> GetAndSaveCurrentLocationAsync()
    {
        // Chỉ cần gọi hàm trên là nó đã tự động lưu nhờ logic setter của CurrentLocation
        return await GetCurrentLocationAsync();
    }

    // Hàm 3: Lấy vị trí đã lưu từ lần trước (Tốc độ phản hồi ngay lập tức, dùng cho các trang con)
    public Location GetSavedLocation()
    {
        if (Preferences.Default.ContainsKey("UserLat") && Preferences.Default.ContainsKey("UserLng"))
        {
            var lat = Preferences.Default.Get("UserLat", 0.0);
            var lng = Preferences.Default.Get("UserLng", 0.0);
            return new Location(lat, lng);
        }

        // Trả về null nếu chưa từng lưu
        return null;
    }
}