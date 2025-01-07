namespace UniSky.Services;

public interface IThemeService
{
    AppTheme GetThemeForDisplay();
    AppTheme GetTheme();
    void SetThemeOnRelaunch(AppTheme theme);
}