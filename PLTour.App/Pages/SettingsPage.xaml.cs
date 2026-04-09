namespace PLTour.App.Pages;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        ThemeSwitch.IsToggled = Application.Current.UserAppTheme == AppTheme.Dark;
        LangPicker.SelectedIndex = 0; // Mặc định Tiếng Việt
    }

    private void ThemeSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        Application.Current.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
    }
}