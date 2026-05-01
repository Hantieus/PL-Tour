namespace PLTour.Admin.ViewModels;

public class ChartDataViewModel
{
    public List<string> Labels { get; set; } = new();
    public List<int> Values { get; set; } = new();
}

public class MonitorStatsViewModel
{
    public ChartDataViewModel EventTypeChart { get; set; } = new();
    public ChartDataViewModel TopDevicesChart { get; set; } = new();
    public ChartDataViewModel HeartbeatByHourChart { get; set; } = new();
    public ChartDataViewModel OnlineByTimeChart { get; set; } = new();
}

public class LabelValuePoint
{
    public string Label { get; set; } = string.Empty;
    public int Value { get; set; }
}
