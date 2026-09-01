using System.Collections.Generic;
using System.Linq;
using WindSoftInstaller.Services;
using Xunit;

namespace WindSoftInstaller.Tests
{
    public class ThemeManagerTests
    {
        [Fact]
        public void CurrentTheme_DefaultsToDefaultTheme()
        {
            Assert.Same(ThemeManager.DefaultTheme, ThemeManager.CurrentTheme);
        }

        [Fact]
        public void ChangeTheme_UpdatesCurrentTheme()
        {
            var previous = ThemeManager.CurrentTheme;
            try
            {
                ThemeManager.ChangeTheme(ThemeManager.DarkTheme);
                Assert.Same(ThemeManager.DarkTheme, ThemeManager.CurrentTheme);
            }
            finally
            {
                ThemeManager.ChangeTheme(previous);
            }
        }

        [Fact]
        public void ChangeTheme_RaisesThemeChanged()
        {
            var previous = ThemeManager.CurrentTheme;
            Theme? raised = null;
            System.Action<Theme> handler = t => raised = t;
            ThemeManager.ThemeChanged += handler;
            try
            {
                ThemeManager.ChangeTheme(ThemeManager.LightTheme);
                Assert.Same(ThemeManager.LightTheme, raised);
            }
            finally
            {
                ThemeManager.ThemeChanged -= handler;
                ThemeManager.ChangeTheme(previous);
            }
        }

        [Fact]
        public void GetAvailableThemes_ReturnsThreeDistinctThemes()
        {
            var themes = ThemeManager.GetAvailableThemes();
            Assert.Equal(3, themes.Count);
            Assert.Equal(3, themes.Distinct().Count());
        }

        [Fact]
        public void GetAvailableThemes_ContainsExpectedKeysInOrder()
        {
            var themes = ThemeManager.GetAvailableThemes();
            Assert.Equal(new[] { "default", "dark", "light" }, themes.Select(t => t.Key));
        }

        [Fact]
        public void GetAvailableThemes_ContainsPredefinedThemes()
        {
            var themes = ThemeManager.GetAvailableThemes();
            Assert.Contains(ThemeManager.DefaultTheme, themes);
            Assert.Contains(ThemeManager.DarkTheme, themes);
            Assert.Contains(ThemeManager.LightTheme, themes);
        }

        [Fact]
        public void DefaultTheme_HasExpectedKey_AndLocalizedName()
        {
            Localization.Change("ru");
            try
            {
                Assert.Equal("default", ThemeManager.DefaultTheme.Key);
                Assert.Equal("Стандартная", ThemeManager.DefaultTheme.Name);
            }
            finally
            {
                Localization.Change("ru");
            }
        }

        [Fact]
        public void Themes_AreDistinctByKey()
        {
            Assert.NotEqual(ThemeManager.DefaultTheme.Key, ThemeManager.DarkTheme.Key);
            Assert.NotEqual(ThemeManager.DefaultTheme.Key, ThemeManager.LightTheme.Key);
            Assert.NotEqual(ThemeManager.DarkTheme.Key, ThemeManager.LightTheme.Key);
        }

        [Fact]
        public void DarkTheme_IsDarkerThanDefault_OnFormBackColor()
        {
            Assert.True(
                ThemeManager.DarkTheme.FormBackColor.GetBrightness()
                < ThemeManager.DefaultTheme.FormBackColor.GetBrightness());
        }
    }
}
