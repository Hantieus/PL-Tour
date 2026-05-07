using PLTour.Shared.Models.DTO;

namespace PLTour.Admin.ViewModels;

public class MonitorDashboardViewModel
{
    public int ActiveCount { get; set; }
    public int OnlineCount { get; set; }
    public int StaleCount { get; set; }
    public int OfflineCount { get; set; }
    public List<ActiveDeviceDto> Devices { get; set; } = new();
}
