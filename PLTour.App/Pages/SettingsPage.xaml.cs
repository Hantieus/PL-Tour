using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using PLTour.App.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace PLTour.App.Pages;

public partial class SettingsPage : ContentPage, INotifyPropertyChanged
{
    private readonly string[] _languageCodes = { "vi", "en", "zh", "ko", "ja" };
    private readonly DeviceMonitorService _deviceMonitorService;
    private readonly AutoPlayPreferenceService _autoPlayPreferenceService;
    private readonly AutoPlayHistoryService _autoPlayHistoryService;

    private string _queueStatusText = "Chưa có dữ liệu hàng đợi";
    private string _queueHintText = "Sự kiện tracking được xếp hàng và gửi lại khi mạng ổn định.";
    private double _queueProgress;
    private string _autoPlayStatusText = "Chưa có lịch sử auto-play";
    private readonly ObservableCollection<AutoPlayHistoryItemViewModel> _autoPlayHistory = new();

    public ObservableCollection<AutoPlayHistoryItemViewModel> AutoPlayHistory => _autoPlayHistory;

    public string QueueStatusText
    {
        get => _queueStatusText;
        set { _queueStatusText = value; OnPropertyChanged(nameof(QueueStatusText)); }
    }

    public string QueueHintText
    {
        get => _queueHintText;
        set { _queueHintText = value; OnPropertyChanged(nameof(QueueHintText)); }
    }

    public double QueueProgress
    {
        get => _queueProgress;
        set { _queueProgress = value; OnPropertyChanged(nameof(QueueProgress)); }
    }

    public string AutoPlayStatusText
    {
        get => _autoPlayStatusText;
        set { _autoPlayStatusText = value; OnPropertyChanged(nameof(AutoPlayStatusText)); }
    }

    public SettingsPage()
    {
        InitializeComponent();

        _deviceMonitorService = DeviceMonitorService.Instance ?? new DeviceMonitorService();
        _autoPlayPreferenceService = AutoPlayPreferenceService.Instance;
        _autoPlayHistoryService = AutoPlayHistoryService.Instance;
        _deviceMonitorService.QueueChanged += DeviceMonitorService_QueueChanged;
        BindingContext = this;

        string savedTheme = Preferences.Default.Get("AppTheme", "Light");
        Application.Current.UserAppTheme = savedTheme == "Dark" ? AppTheme.Dark : AppTheme.Light;
        ThemeSwitch.IsToggled = savedTheme == "Dark";

        string savedLang = Preferences.Default.Get("UserLanguage", "vi");
        LocalizationService.Instance.ApplyLanguage(savedLang, persist: false);
        int index = Array.IndexOf(_languageCodes, savedLang);
        LangPicker.SelectedIndex = index >= 0 ? index : 0;
        LangPicker.SelectedIndexChanged += LangPicker_SelectedIndexChanged;

        AutoPlaySwitch.IsToggled = _autoPlayPreferenceService.IsEnabled;
        AutoPlayStatusText = AutoPlaySwitch.IsToggled ? "Auto-play đang bật." : "Auto-play đang tắt.";
        _ = LoadAutoPlayHistoryAsync();
        UpdateQueueUi();
    }

    private async Task LoadAutoPlayHistoryAsync()
    {
        await _autoPlayHistoryService.LoadAsync();
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _autoPlayHistory.Clear();
            foreach (var item in _autoPlayHistoryService.Items.Take(10))
            {
                _autoPlayHistory.Add(new AutoPlayHistoryItemViewModel(item));
            }

            AutoPlayStatusText = _autoPlayHistory.Count == 0
                ? "Chưa có lịch sử auto-play"
                : $"Đã ghi nhận {_autoPlayHistory.Count} lần auto-play gần nhất.";
        });
    }

    private void DeviceMonitorService_QueueChanged(object? sender, EventArgs e)
        => MainThread.BeginInvokeOnMainThread(UpdateQueueUi);

    private void UpdateQueueUi()
    {
        var pending = _deviceMonitorService.PendingCount;
        var running = _deviceMonitorService.QueueIsRunning;

        QueueStatusText = running
            ? $"Đang chạy: {pending} sự kiện chờ"
            : $"Đã dừng: {pending} sự kiện chờ";

        QueueProgress = Math.Min(1.0, pending / 10.0);
        QueueHintText = pending == 0
            ? "Không còn sự kiện nào trong hàng đợi."
            : "Sự kiện sẽ được gửi theo thứ tự và tự động retry nếu lỗi mạng.";
    }

    private void ThemeSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        Application.Current.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
        Preferences.Default.Set("AppTheme", e.Value ? "Dark" : "Light");
    }

    private void AutoPlaySwitch_Toggled(object sender, ToggledEventArgs e)
    {
        _autoPlayPreferenceService.SetEnabled(e.Value);
        AutoPlayStatusText = e.Value ? "Auto-play đang bật." : "Auto-play đang tắt.";
    }

    private void LangPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (LangPicker.SelectedIndex < 0) return;

        string langCode = _languageCodes[LangPicker.SelectedIndex];
        if (Preferences.Default.Get("UserLanguage", "vi") == langCode) return;

        Preferences.Default.Set("UserLanguage", langCode);
        _ = DeviceMonitorService.Instance.TrackEventAsync("language_change", new PLTour.Shared.Models.DTO.AnalyticsEventDto { LanguageCode = langCode, Keyword = "settings" });

        LocalizationService.Instance.ApplyLanguage(langCode, persist: false);
        Application.Current.MainPage = new AppShell();
    }
}

public sealed class AutoPlayHistoryItemViewModel
{
    public AutoPlayHistoryItemViewModel(AutoPlayHistoryItem item)
    {
        PoiName = item.PoiName;
        PlayedAtDisplay = item.PlayedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
        DetailText = $"Cách {Math.Round(item.DistanceMeters)}m / bán kính {Math.Round(item.RadiusMeters)}m";
    }

    public string PoiName { get; }
    public string PlayedAtDisplay { get; }
    public string DetailText { get; }
}
