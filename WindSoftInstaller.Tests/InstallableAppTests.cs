using System.Collections.Generic;
using Xunit;

namespace WindSoftInstaller.Tests
{
    public class InstallableAppTests
    {
        private static InstallableApp Make(string name, string descriptionKey = "btnInstall")
            => new InstallableApp
            {
                Name = name,
                ExecutablePath = name + ".exe",
                SizeMB = 10.0,
                LicenseUrl = "https://example.com/license",
                DescriptionKey = descriptionKey
            };

        // ---------- Description ----------

        [Fact]
        public void Description_UsesLocalizedKey()
        {
            Localization.Change("ru");
            try
            {
                var app = Make("Firefox");
                Assert.Equal("Установить", app.Description); // btnInstall в ru
            }
            finally
            {
                Localization.Change("ru");
            }
        }

        [Fact]
        public void Description_UnknownKey_ReturnsKey()
        {
            var app = Make("Firefox", "no.such.key");
            Assert.Equal("no.such.key", app.Description);
        }

        // ---------- ParametersDisplay ----------

        [Fact]
        public void ParametersDisplay_JoinsParameterKeyValue()
        {
            var app = Make("App");
            app.CustomParameters = new Dictionary<string, string>
            {
                ["param.SilentInstall"] = "/S",
                ["param.InstallPath"] = "{InstallDir}"
            };
            Localization.Change("ru");
            try
            {
                var display = app.ParametersDisplay;
                Assert.Contains("Тихая установка: /S", display);   // param.SilentInstall в ru
                Assert.Contains("Путь установки: {InstallDir}", display); // param.InstallPath в ru
                Assert.Contains("; ", display);
            }
            finally
            {
                Localization.Change("ru");
            }
        }

        [Fact]
        public void ParametersDisplay_EmptyWhenNoParams()
        {
            Localization.Change("ru");
            try
            {
                var app = Make("App");
                Assert.Equal("", app.ParametersDisplay);
            }
            finally
            {
                Localization.Change("ru");
            }
        }

        [Fact]
        public void ParametersDisplay_UsesEnglishKeys_ForEn()
        {
            var app = Make("App");
            app.CustomParameters = new Dictionary<string, string>
            {
                ["param.SilentInstall"] = "/S",
                ["param.InstallPath"] = "{InstallDir}"
            };
            Localization.Change("en");
            try
            {
                var display = app.ParametersDisplay;
                Assert.Contains("Silent install: /S", display);
                Assert.Contains("Install path: {InstallDir}", display);
            }
            finally
            {
                Localization.Change("ru");
            }
        }

        // ---------- Defaults ----------

        [Fact]
        public void Defaults_AreApplied()
        {
            var app = Make("App");
            Assert.Empty(app.CustomParameters);
            Assert.Empty(app.AdditionalFiles);
            Assert.Equal("/D=", app.PathParameterKey);
            Assert.False(app.IsPortable);
            Assert.Null(app.ShortcutName);
            Assert.Equal("", app.ArchivePathEn);
            Assert.Equal("", app.ArchivePathRu);
        }

        // ---------- GetLocalizedArchivePath ----------

        [Fact]
        public void GetLocalizedArchivePath_ReturnsRu_WhenRuAndSet()
        {
            var app = Make("MSI Afterburner");
            app.ArchivePathEn = "En.zip";
            app.ArchivePathRu = "Ru.zip";
            Localization.Change("ru");
            try
            {
                Assert.Equal("Ru.zip", app.GetLocalizedArchivePath());
            }
            finally
            {
                Localization.Change("ru");
            }
        }

        [Fact]
        public void GetLocalizedArchivePath_ReturnsEn_WhenNotRu()
        {
            var app = Make("MSI Afterburner");
            app.ArchivePathEn = "En.zip";
            app.ArchivePathRu = "Ru.zip";
            Localization.Change("en");
            try
            {
                Assert.Equal("En.zip", app.GetLocalizedArchivePath());
            }
            finally
            {
                Localization.Change("ru");
            }
        }

        [Fact]
        public void GetLocalizedArchivePath_FallsBackToEn_WhenRuEmpty()
        {
            var app = Make("App");
            app.ArchivePathEn = "OnlyEn.zip";
            app.ArchivePathRu = "";
            Localization.Change("ru");
            try
            {
                Assert.Equal("OnlyEn.zip", app.GetLocalizedArchivePath());
            }
            finally
            {
                Localization.Change("ru");
            }
        }

        [Fact]
        public void GetLocalizedArchivePath_FallsBackToExecutablePath_WhenBothEmpty()
        {
            var app = Make("App");
            app.ExecutablePath = "app-setup.exe";
            app.ArchivePathEn = "";
            app.ArchivePathRu = "";
            Assert.Equal("app-setup.exe", app.GetLocalizedArchivePath());
        }

        [Fact]
        public void GetLocalizedArchivePath_ReturnsEn_WhenNotRu_EvenIfRuEmpty()
        {
            var app = Make("App");
            app.ArchivePathEn = "EnOnly.zip";
            app.ArchivePathRu = "";
            Localization.Change("en");
            try
            {
                Assert.Equal("EnOnly.zip", app.GetLocalizedArchivePath());
            }
            finally
            {
                Localization.Change("ru");
            }
        }
    }
}
