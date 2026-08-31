using WindSoftInstaller;
using Xunit;

namespace WindSoftInstaller.Tests
{
    public class LocalizationTests
    {
        [Fact]
        public void Current_DefaultsToRussian()
        {
            Assert.Equal("ru", Localization.Current);
        }

        [Fact]
        public void T_ReturnsValue_ForExistingKey()
        {
            Assert.Equal("Установить", Localization.T("btnInstall"));
        }

        [Fact]
        public void T_ReturnsKey_WhenUnknown()
        {
            Assert.Equal("no.such.key", Localization.T("no.such.key"));
        }

        [Fact]
        public void T_ReturnsEnglish_AfterChangeToEn()
        {
            Localization.Change("en");
            try
            {
                Assert.Equal("Install", Localization.T("btnInstall"));
            }
            finally
            {
                Localization.Change("ru");
            }
        }

        [Fact]
        public void T_ReturnsKey_WhenLanguageNotFound()
        {
            Localization.Change("de");
            try
            {
                Assert.Equal("btnInstall", Localization.T("btnInstall"));
            }
            finally
            {
                Localization.Change("ru");
            }
        }

        [Fact]
        public void Change_UpdatesCurrent()
        {
            Localization.Change("en");
            try
            {
                Assert.Equal("en", Localization.Current);
            }
            finally
            {
                Localization.Change("ru");
            }
        }

        [Fact]
        public void EulaText_ExistsInBothLanguages()
        {
            Localization.Change("ru");
            var ru = Localization.T("EulaForm.Text");
            Localization.Change("en");
            var en = Localization.T("EulaForm.Text");
            Localization.Change("ru");

            Assert.False(string.IsNullOrWhiteSpace(ru));
            Assert.False(string.IsNullOrWhiteSpace(en));
        }
    }

    public class ShortcutHelperTests
    {
        [Theory]
        [InlineData("LMMS", "lmms.exe")]
        [InlineData("HandBrake", "HandBrake.exe")]
        [InlineData("Clementine", "Clementine.exe")]
        [InlineData("UltraDefrag", "ufd.gui.exe")]
        [InlineData("RivaTuner Statistics Server", "RTSS.exe")]
        [InlineData("Cryptomator", "Cryptomator.exe")]
        [InlineData("KeePass", "KeePass.exe")]
        public void TryGetExeRelativePath_ReturnsTrue_ForKnownApp(string app, string expected)
        {
            Assert.True(ShortcutHelper.TryGetExeRelativePath(app, out var path));
            Assert.Equal(expected, path);
        }

        [Theory]
        [InlineData("ClamWin")]
        public void TryGetExeRelativePath_ReturnsNestedPath_ForClamWin(string app)
        {
            Assert.True(ShortcutHelper.TryGetExeRelativePath(app, out var path));
            Assert.EndsWith("ClamWin.exe", path);
            Assert.Contains("bin", path);
        }

        [Fact]
        public void TryGetExeRelativePath_ReturnsFalse_ForUnknownApp()
        {
            Assert.False(ShortcutHelper.TryGetExeRelativePath("Notepad++", out _));
        }

        [Fact]
        public void TryGetExeRelativePath_OutputsNull_ForUnknownApp()
        {
            ShortcutHelper.TryGetExeRelativePath("Unknown", out var path);
            Assert.Null(path);
        }

        [Fact]
        public void CreateShortcut_Throws_WhenTargetFileDoesNotExist()
        {
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
            var missing = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "WSI_nonexistent_target_shortcut.exe");

            Assert.Throws<System.IO.FileNotFoundException>(() =>
                ShortcutHelper.CreateShortcut(logger, missing, "SomeApp"));
        }
    }
}
