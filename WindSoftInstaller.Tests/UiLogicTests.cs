using System;
using System.Collections.Generic;
using System.Linq;
using WindSoftInstaller;
using Xunit;

namespace WindSoftInstaller.Tests
{
    public class UiLogicTests
    {
        private static InstallableApp Make(string name)
            => new InstallableApp
            {
                Name = name,
                ExecutablePath = name + ".exe",
                SizeMB = 10.0,
                LicenseUrl = "https://example.com/license"
            };

        // ---------- IsHidden ----------

        [Theory]
        [InlineData("Microsoft VC++ 2015-2022 Redistributable (x64)")]
        [InlineData("VC++ 2013 Redistributable (x86)")]
        public void IsHidden_ReturnsTrue_ForSystemApps(string name)
        {
            Assert.True(UiLogic.IsHidden(name));
        }

        [Theory]
        [InlineData("Firefox")]
        [InlineData("Notepad++")]
        [InlineData("")]
        [InlineData(null)]
        public void IsHidden_ReturnsFalse_ForNormalOrEmpty(string? name)
        {
            Assert.False(UiLogic.IsHidden(name));
        }

        [Fact]
        public void IsHidden_CoversAllHiddenAppsEntries()
        {
            foreach (var app in UiLogic.HiddenApps)
                Assert.True(UiLogic.IsHidden(app), $"'{app}' должен быть скрыт");
        }

        [Fact]
        public void IsHidden_IsCaseInsensitive()
        {
            Assert.True(UiLogic.IsHidden("microsoft vc++ 2015-2022 redistributable (x64)"));
            Assert.True(UiLogic.IsHidden("vc++ 2013 redistributable (x86)"));
        }

        [Fact]
        public void IsHidden_DoesNotHideChrome_AsItIsNotInHiddenAppsList()
        {
            // Chrome скрывается через InstallPlanRules.IsSystemApp, а не через UiLogic.HiddenApps
            Assert.False(UiLogic.IsHidden("Google Chrome"));
            Assert.False(UiLogic.IsHidden("GOOGLE CHROME"));
        }

        // ---------- HasDependencies / GetDependencies ----------

        [Theory]
        [InlineData("Marble")]
        [InlineData("MSI Afterburner")]
        [InlineData("RivaTuner Statistics Server")]
        [InlineData("XMedia Recode")]
        public void HasDependencies_ReturnsTrue(string name)
        {
            Assert.True(UiLogic.HasDependencies(name));
        }

        [Fact]
        public void HasDependencies_ReturnsFalse_ForAppWithoutDeps()
        {
            Assert.False(UiLogic.HasDependencies("Notepad++"));
        }

        [Fact]
        public void GetDependencies_ReturnsBothVc2013_ForMarble()
        {
            var deps = UiLogic.GetDependencies("Marble");
            Assert.Contains("VC++ 2013 Redistributable (x86)", deps);
            Assert.Contains("VC++ 2013 Redistributable (x64)", deps);
        }

        [Fact]
        public void GetDependencies_ReturnsEmpty_ForMissingKey()
        {
            Assert.Empty(UiLogic.GetDependencies("Notepad++"));
        }

        // ---------- GetIconFileName / GetIconPath ----------

        [Fact]
        public void GetIconFileName_ReplacesExtensionWithIco()
        {
            Assert.Equal("app.ico", UiLogic.GetIconFileName("app.exe"));
        }

        [Fact]
        public void GetIconFileName_HandlesNullPath()
        {
            Assert.Equal(".ico", UiLogic.GetIconFileName(null));
        }

        [Fact]
        public void GetIconFileName_HandlesPathWithoutExtension()
        {
            Assert.Equal("app.ico", UiLogic.GetIconFileName("app"));
        }

        [Fact]
        public void GetIconPath_CombinesFolderAndIco()
        {
            var path = UiLogic.GetIconPath(@"C:\Icons", "app.exe");
            Assert.StartsWith(@"C:\Icons\", path);
            Assert.EndsWith("app.ico", path);
        }

        // ---------- SumSelectedSizes ----------

        [Fact]
        public void SumSelectedSizes_ReturnsZero_ForEmpty()
        {
            Assert.Equal(0.0, UiLogic.SumSelectedSizes(Array.Empty<double>()));
        }

        [Fact]
        public void SumSelectedSizes_ReturnsZero_ForNull()
        {
            Assert.Equal(0.0, UiLogic.SumSelectedSizes(null!));
        }

        [Fact]
        public void SumSelectedSizes_ReturnsSum()
        {
            Assert.Equal(35.0, UiLogic.SumSelectedSizes(new[] { 10.0, 15.0, 10.0 }));
        }

        // ---------- AddMissingPrerequisites ----------

        [Fact]
        public void AddMissingPrerequisites_AddsVc2013_ForMarble_WhenMissing()
        {
            var marble = Make("Marble");
            var vc2013x86 = Make("VC++ 2013 Redistributable (x86)");
            var vc2013x64 = Make("VC++ 2013 Redistributable (x64)");
            var lookup = new Dictionary<string, InstallableApp>(StringComparer.OrdinalIgnoreCase)
            {
                ["VC++ 2013 Redistributable (x86)"] = vc2013x86,
                ["VC++ 2013 Redistributable (x64)"] = vc2013x64
            };

            var result = UiLogic.AddMissingPrerequisites(new List<InstallableApp?> { marble }, lookup);

            Assert.Contains(vc2013x86, result);
            Assert.Contains(vc2013x64, result);
            Assert.Equal("VC++ 2013 Redistributable (x86)", result[0]!.Name);
            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void AddMissingPrerequisites_DoesNotDuplicate_WhenAlreadySelected()
        {
            var marble = Make("Marble");
            var vc2013x86 = Make("VC++ 2013 Redistributable (x86)");
            var vc2013x64 = Make("VC++ 2013 Redistributable (x64)");
            var lookup = new Dictionary<string, InstallableApp>(StringComparer.OrdinalIgnoreCase)
            {
                ["VC++ 2013 Redistributable (x86)"] = vc2013x86,
                ["VC++ 2013 Redistributable (x64)"] = vc2013x64
            };

            var result = UiLogic.AddMissingPrerequisites(
                new List<InstallableApp?> { marble, vc2013x86, vc2013x64 }, lookup);

            Assert.Equal(3, result.Count);
        }

        [Fact]
        public void AddMissingPrerequisites_AddsVc2015_ForAfterburner()
        {
            var ab = Make("MSI Afterburner");
            var vc2015x86 = Make("Microsoft VC++ 2015-2022 Redistributable (x86)");
            var vc2015x64 = Make("Microsoft VC++ 2015-2022 Redistributable (x64)");
            var lookup = new Dictionary<string, InstallableApp>(StringComparer.OrdinalIgnoreCase)
            {
                ["Microsoft VC++ 2015-2022 Redistributable (x86)"] = vc2015x86,
                ["Microsoft VC++ 2015-2022 Redistributable (x64)"] = vc2015x64
            };

            var result = UiLogic.AddMissingPrerequisites(new List<InstallableApp?> { ab }, lookup);

            Assert.Equal(3, result.Count);
            Assert.Contains(vc2015x86, result);
            Assert.Contains(vc2015x64, result);
        }

        [Fact]
        public void AddMissingPrerequisites_ReturnsSame_WhenNoRequirement()
        {
            var firefox = Make("Firefox");
            var result = UiLogic.AddMissingPrerequisites(new List<InstallableApp?> { firefox },
                new Dictionary<string, InstallableApp>(StringComparer.OrdinalIgnoreCase));

            Assert.Single(result);
            Assert.Equal("Firefox", result[0]!.Name);
        }

        [Fact]
        public void AddMissingPrerequisites_DoesNotCrash_WhenPrerequisiteMissingFromLookup()
        {
            var marble = Make("Marble");
            // В lookup нет ни одного VC++ — не должно бросать исключение и не должно ничего добавлять.
            var result = UiLogic.AddMissingPrerequisites(
                new List<InstallableApp?> { marble },
                new Dictionary<string, InstallableApp>(StringComparer.OrdinalIgnoreCase));

            Assert.Single(result);
            Assert.Equal("Marble", result[0]!.Name);
        }

        [Fact]
        public void AddMissingPrerequisites_DoesNotCrash_WhenLookupIsNull()
        {
            var marble = Make("Marble");
            var result = UiLogic.AddMissingPrerequisites(new List<InstallableApp?> { marble }, null!);

            Assert.Single(result);
            Assert.Equal("Marble", result[0]!.Name);
        }

        [Fact]
        public void AddMissingPrerequisites_ReturnsEmpty_ForEmptyInput()
        {
            var result = UiLogic.AddMissingPrerequisites(new List<InstallableApp?>(),
                new Dictionary<string, InstallableApp>(StringComparer.OrdinalIgnoreCase));

            Assert.Empty(result);
        }

        [Fact]
        public void AddMissingPrerequisites_AddsMissingAcrossMultipleGroups()
        {
            var marble = Make("Marble");
            var ab = Make("RivaTuner Statistics Server");
            var vc2013 = Make("VC++ 2013 Redistributable (x86)");
            var vc2013x64 = Make("VC++ 2013 Redistributable (x64)");
            var vc2015x86 = Make("Microsoft VC++ 2015-2022 Redistributable (x86)");
            var vc2015x64 = Make("Microsoft VC++ 2015-2022 Redistributable (x64)");
            var lookup = new Dictionary<string, InstallableApp>(StringComparer.OrdinalIgnoreCase)
            {
                ["VC++ 2013 Redistributable (x86)"] = vc2013,
                ["VC++ 2013 Redistributable (x64)"] = vc2013x64,
                ["Microsoft VC++ 2015-2022 Redistributable (x86)"] = vc2015x86,
                ["Microsoft VC++ 2015-2022 Redistributable (x64)"] = vc2015x64
            };

            var result = UiLogic.AddMissingPrerequisites(
                new List<InstallableApp?> { marble, ab }, lookup);

            Assert.Equal(6, result.Count);
            Assert.Contains(vc2015x86, result);
            Assert.Contains(vc2015x64, result);
            Assert.Contains(vc2013, result);
            Assert.Contains(vc2013x64, result);
        }
    }
}
