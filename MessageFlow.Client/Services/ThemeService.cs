namespace MessageFlow.Client.Services
{
    public class ThemeService
    {
        public string CurrentTheme { get; private set; } = "dark";
        public event Action? OnChange;

        public void ToggleTheme()
        {
            CurrentTheme = CurrentTheme == "dark" ? "light" : "dark";
            OnChange?.Invoke();
        }

        public string GetCssClass()
        {
            var css = $"{CurrentTheme}-theme";
            return css;
        }
    }
}