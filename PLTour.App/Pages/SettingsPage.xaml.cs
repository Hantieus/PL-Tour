using Microsoft.Maui.Storage;
using System.Globalization;

namespace PLTour.App.Pages;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        ThemeSwitch.IsToggled = Application.Current.UserAppTheme == AppTheme.Dark;

        // Đọc ngôn ngữ đang dùng để hiển thị đúng lựa chọn trên Picker
        string savedLang = Preferences.Default.Get("UserLanguage", "vi");
        LangPicker.SelectedIndex = savedLang == "vi" ? 0 : 1;

        // Đăng ký sự kiện khi người dùng bấm chọn dòng khác
        LangPicker.SelectedIndexChanged += LangPicker_SelectedIndexChanged;
    }

    private void ThemeSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        Application.Current.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
    }

    private void LangPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        // 1. Lấy mã ngôn ngữ người dùng vừa chọn
        string langCode = LangPicker.SelectedIndex == 0 ? "vi" : "en";

        // Tránh tình trạng chọn lại ngôn ngữ cũ gây giật lag
        if (Preferences.Default.Get("UserLanguage", "vi") == langCode) return;

        // 2. Lưu vào máy
        Preferences.Default.Set("UserLanguage", langCode);

        // 3. Đổi Culture của toàn hệ thống (Dùng cho đa ngôn ngữ giao diện .resx)
        var culture = new CultureInfo(langCode);
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        // 4. MẸO: Tải lại toàn bộ khung giao diện để áp dụng chữ mới lập tức
        Application.Current.MainPage = new AppShell();
    }
}