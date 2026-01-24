namespace JournalApp.Services
{
    public class ThemeService
    {
        public bool IsDarkMode { get; private set; } = false;

        public event Action? OnThemeChanged;

        public void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;
            OnThemeChanged?.Invoke();
        }

        public string ThemeClass => IsDarkMode ? "dark-theme" : "light-theme";
    }
}