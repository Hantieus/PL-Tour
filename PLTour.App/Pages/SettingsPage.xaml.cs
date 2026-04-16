using Microsoft.Maui.Storage;
using System.Globalization;

namespace PLTour.App.Pages;

public partial class SettingsPage : ContentPage
{
    // Tạo sẵn mảng mã ngôn ngữ khớp với thứ tự trong file XAML
    private readonly string[] _languageCodes = { "vi", "en", "zh", "ko", "ja" };

    public SettingsPage()
    {
        InitializeComponent();

        // 1. Load và áp dụng theme đã lưu
        string savedTheme = Preferences.Default.Get("AppTheme", "Light");
        Application.Current.UserAppTheme = savedTheme == "Dark" ? AppTheme.Dark : AppTheme.Light;

        // Gán trạng thái cho công tắc
        ThemeSwitch.IsToggled = savedTheme == "Dark";

        // 2. Đọc ngôn ngữ đang dùng
        string savedLang = Preferences.Default.Get("UserLanguage", "vi");
        int index = Array.IndexOf(_languageCodes, savedLang);
        LangPicker.SelectedIndex = index >= 0 ? index : 0;

        // Đăng ký sự kiện (Luôn để dưới cùng để tránh bị kích hoạt sự kiện ngoài ý muốn khi vừa khởi tạo)
        LangPicker.SelectedIndexChanged += LangPicker_SelectedIndexChanged;
    }

    // ✅ Đã được viết gộp lại cực kỳ ngắn gọn và dễ đọc
    private void ThemeSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        Application.Current.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
        Preferences.Default.Set("AppTheme", e.Value ? "Dark" : "Light");
    }

    private void LangPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (LangPicker.SelectedIndex < 0) return;

        string langCode = _languageCodes[LangPicker.SelectedIndex];

        if (Preferences.Default.Get("UserLanguage", "vi") == langCode) return;

        // Lưu
        Preferences.Default.Set("UserLanguage", langCode);

        // Đổi ngôn ngữ hệ thống
        var culture = new CultureInfo(langCode);
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        // Reload UI để áp dụng chữ mới
        Application.Current.MainPage = new AppShell();
    }
}