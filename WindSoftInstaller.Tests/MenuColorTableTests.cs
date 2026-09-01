using WindSoftInstaller.Services;
using WindSoftInstaller.Utilities;
using Xunit;

namespace WindSoftInstaller.Tests
{
    public class MenuColorTableTests
    {
        private static MenuColorTable MakeTable(out Theme theme)
        {
            theme = ThemeManager.DarkTheme;
            return new MenuColorTable(theme);
        }

        [Fact]
        public void MenuStripGradients_UseThemeMenuBackColor()
        {
            var table = MakeTable(out var theme);
            Assert.Equal(theme.MenuBackColor, table.MenuStripGradientBegin);
            Assert.Equal(theme.MenuBackColor, table.MenuStripGradientEnd);
        }

        [Fact]
        public void ToolStripDropDownBackground_UsesTheme()
        {
            var table = MakeTable(out var theme);
            Assert.Equal(theme.MenuDropDownBackColor, table.ToolStripDropDownBackground);
        }

        [Fact]
        public void Borders_UseThemeGridLineColor()
        {
            var table = MakeTable(out var theme);
            Assert.Equal(theme.GridLineColor, table.MenuBorder);
            Assert.Equal(theme.GridLineColor, table.MenuItemBorder);
        }

        [Fact]
        public void MenuItemSelected_UsesThemeGridSelectionBackColor()
        {
            var table = MakeTable(out var theme);
            Assert.Equal(theme.GridSelectionBackColor, table.MenuItemSelected);
            Assert.Equal(theme.GridSelectionBackColor, table.MenuItemSelectedGradientBegin);
            Assert.Equal(theme.GridSelectionBackColor, table.MenuItemSelectedGradientEnd);
        }

        [Fact]
        public void ImageMarginGradients_UseThemeMenuDropDownBackColor()
        {
            var table = MakeTable(out var theme);
            Assert.Equal(theme.MenuDropDownBackColor, table.ImageMarginGradientBegin);
            Assert.Equal(theme.MenuDropDownBackColor, table.ImageMarginGradientMiddle);
            Assert.Equal(theme.MenuDropDownBackColor, table.ImageMarginGradientEnd);
        }

        [Fact]
        public void DifferentThemes_ProduceDifferentMenuBackColors()
        {
            var dark = new MenuColorTable(ThemeManager.DarkTheme);
            var light = new MenuColorTable(ThemeManager.LightTheme);
            Assert.NotEqual(dark.MenuStripGradientBegin, light.MenuStripGradientBegin);
        }
    }
}
