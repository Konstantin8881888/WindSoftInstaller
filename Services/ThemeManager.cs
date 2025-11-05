using System.Drawing;

namespace WindSoftInstaller.Services
{
    public class Theme
    {
        public string Key { get; set; }  // Ключ для локализации вместо прямого имени
        public string Name => Localization.T($"theme.{Key}");  // Локализованное имя
        public Color FormBackColor { get; set; }
        public Color ControlBackColor { get; set; }
        public Color ControlForeColor { get; set; }
        public Color ButtonBackColor { get; set; }
        public Color ButtonForeColor { get; set; }
        public Color GridBackColor { get; set; }
        public Color GridForeColor { get; set; }
        public Color GridAlternatingBackColor { get; set; }
        public Color GridSelectionBackColor { get; set; }
        public Color MenuBackColor { get; set; }
        public Color MenuForeColor { get; set; }
        public Color ProgressBarColor { get; set; }
        public Color MenuDropDownBackColor { get; set; }
        public Color MenuDropDownForeColor { get; set; }
        public Color GridHeaderBackColor { get; set; }
        public Color GridHeaderForeColor { get; set; }
        public Color GridLineColor { get; set; }
    }

    public static class ThemeManager
    {
        public static event Action<Theme> ThemeChanged;
        public static Theme CurrentTheme { get; private set; }

        // Стандартная тема (существующий стиль)
        public static readonly Theme DefaultTheme = new Theme
        {
            Key = "default",  // Используем ключ вместо прямого имени
            FormBackColor = Color.LightSteelBlue,
            ControlBackColor = Color.White,
            ControlForeColor = Color.Black,
            ButtonBackColor = Color.MediumSeaGreen,
            ButtonForeColor = Color.White,
            GridBackColor = Color.White,
            GridForeColor = Color.Black,
            GridAlternatingBackColor = Color.LightGray,
            GridSelectionBackColor = Color.DarkGray,
            MenuBackColor = SystemColors.Menu,
            MenuForeColor = SystemColors.MenuText,
            ProgressBarColor = SystemColors.Highlight,
            MenuDropDownBackColor = SystemColors.Menu,
            MenuDropDownForeColor = SystemColors.MenuText,
            GridHeaderBackColor = SystemColors.Control,
            GridHeaderForeColor = SystemColors.ControlText,
            GridLineColor = Color.Silver
        };

        // Темная тема
        public static readonly Theme DarkTheme = new Theme
        {
            Key = "dark",
            FormBackColor = Color.FromArgb(45, 45, 48),
            ControlBackColor = Color.FromArgb(63, 63, 70),
            ControlForeColor = Color.White,
            ButtonBackColor = Color.FromArgb(0, 122, 204),
            ButtonForeColor = Color.White,
            GridBackColor = Color.FromArgb(37, 37, 38),
            GridForeColor = Color.White,
            GridAlternatingBackColor = Color.FromArgb(51, 51, 55),
            GridSelectionBackColor = Color.FromArgb(75, 75, 80),
            MenuBackColor = Color.FromArgb(45, 45, 48),
            MenuForeColor = Color.White,
            ProgressBarColor = Color.FromArgb(0, 122, 204),
            MenuDropDownBackColor = Color.FromArgb(63, 63, 70),
            MenuDropDownForeColor = Color.White,
            GridHeaderBackColor = Color.FromArgb(51, 51, 55),
            GridHeaderForeColor = Color.White,
            GridLineColor = Color.FromArgb(80, 80, 80)
        };

        // Светлая тема
        public static readonly Theme LightTheme = new Theme
        {
            Key = "light",
            FormBackColor = SystemColors.Control,
            ControlBackColor = Color.White,
            ControlForeColor = Color.Black,
            ButtonBackColor = Color.SteelBlue,
            ButtonForeColor = Color.White,
            GridBackColor = Color.White,
            GridForeColor = Color.Black,
            GridAlternatingBackColor = Color.FromArgb(240, 240, 240),
            GridSelectionBackColor = Color.FromArgb(200, 200, 200),
            MenuBackColor = SystemColors.Menu,
            MenuForeColor = SystemColors.MenuText,
            ProgressBarColor = Color.SteelBlue,
            MenuDropDownBackColor = SystemColors.Menu,
            MenuDropDownForeColor = SystemColors.MenuText,
            GridHeaderBackColor = SystemColors.Control,
            GridHeaderForeColor = SystemColors.ControlText,
            GridLineColor = Color.Silver
        };

        static ThemeManager()
        {
            CurrentTheme = DefaultTheme;
        }

        public static void ChangeTheme(Theme theme)
        {
            CurrentTheme = theme;
            ThemeChanged?.Invoke(theme);
        }

        public static List<Theme> GetAvailableThemes()
        {
            return new List<Theme> { DefaultTheme, DarkTheme, LightTheme };
        }
    }
}
