using System;
using System.Collections.Generic;
using System.Linq;
using WindSoftInstaller.Services;
using Xunit;

namespace WindSoftInstaller.Tests
{
    public class InstallPlanRulesTests
    {
        // ---------- IsVlc ----------

        [Theory]
        [InlineData("VLC")]
        [InlineData("vlc")]
        [InlineData("VLC Media Player")]
        public void IsVlc_ReturnsTrue_ForVlcNames(string name)
        {
            Assert.True(InstallPlanRules.IsVlc(name));
        }

        [Theory]
        [InlineData("NotVlc")]
        [InlineData("AIMP")]
        [InlineData("")]
        [InlineData(null)]
        public void IsVlc_ReturnsFalse_ForNonVlc(string? name)
        {
            Assert.False(InstallPlanRules.IsVlc(name ?? string.Empty));
        }

        // ---------- IsSystemApp ----------

        [Theory]
        [InlineData("Google Chrome")]
        [InlineData("Microsoft VC++ 2015-2022 Redistributable (x64)")]
        [InlineData("VC++ 2013 Redistributable (x86)")]
        [InlineData("Some VC++ product")]
        public void IsSystemApp_ReturnsTrue(string name)
        {
            Assert.True(InstallPlanRules.IsSystemApp(name));
        }

        [Theory]
        [InlineData("Firefox")]
        [InlineData("Notepad++")]
        [InlineData("")]
        [InlineData(null)]
        public void IsSystemApp_ReturnsFalse(string? name)
        {
            Assert.False(InstallPlanRules.IsSystemApp(name ?? string.Empty));
        }

        [Theory]
        [InlineData("Google Chrom")]       // похожее, но не полное имя
        [InlineData("chrome")]             // без полного названия
        [InlineData("VCplusplus")]         // "VC++" с пропущенными плюсами
        public void IsSystemApp_ReturnsFalse_ForSimilarNames(string name)
        {
            Assert.False(InstallPlanRules.IsSystemApp(name));
        }

        // ---------- IsVcRedist2015_2022 ----------

        [Theory]
        [InlineData("Microsoft VC++ 2015-2022 Redistributable (x64)")]
        [InlineData("any vc++ 2015-2022 thing")]
        public void IsVcRedist2015_2022_ReturnsTrue(string name)
        {
            Assert.True(InstallPlanRules.IsVcRedist2015_2022(name));
        }

        [Theory]
        [InlineData("VC++ 2013 Redistributable (x64)")]
        [InlineData("Opera")]
        [InlineData(null)]
        public void IsVcRedist2015_2022_ReturnsFalse(string? name)
        {
            Assert.False(InstallPlanRules.IsVcRedist2015_2022(name ?? string.Empty));
        }

        // ---------- ShouldCopyInstaller ----------

        [Theory]
        [InlineData("Opera")]
        [InlineData("LMMS")]
        [InlineData("VC++ 2013 Redistributable (x64)")]
        [InlineData("Microsoft VC++ 2015-2022 Redistributable (x64)")]
        public void ShouldCopyInstaller_ReturnsTrue(string name)
        {
            Assert.True(InstallPlanRules.ShouldCopyInstaller(name));
        }

        [Theory]
        [InlineData("opera")]                    // нижний регистр
        [InlineData("lmms")]
        [InlineData("VC++ 2013 redistributable")] // без скобок, смешанный регистр
        [InlineData("microsoft vc++ 2015-2022 redistributable (x86)")]
        public void ShouldCopyInstaller_ReturnsTrue_CaseInsensitive(string name)
        {
            Assert.True(InstallPlanRules.ShouldCopyInstaller(name));
        }

        [Theory]
        [InlineData("Firefox")]
        [InlineData("AIMP")]
        [InlineData("")]
        [InlineData(null)]
        public void ShouldCopyInstaller_ReturnsFalse(string? name)
        {
            Assert.False(InstallPlanRules.ShouldCopyInstaller(name ?? string.Empty));
        }

        // ---------- IsVcRedistSuccessCode ----------

        [Theory]
        [InlineData(0)]
        [InlineData(1638)]
        [InlineData(3010)]
        public void IsVcRedistSuccessCode_ReturnsTrue(int code)
        {
            Assert.True(InstallPlanRules.IsVcRedistSuccessCode(code));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(1603)]
        [InlineData(-1)]
        public void IsVcRedistSuccessCode_ReturnsFalse(int code)
        {
            Assert.False(InstallPlanRules.IsVcRedistSuccessCode(code));
        }

        // ---------- IsTemporaryExtension ----------

        [Theory]
        [InlineData(".tmp")]
        [InlineData(".log")]
        [InlineData(".txt")]
        [InlineData(".bak")]
        [InlineData(".TMP")]
        [InlineData(".Log")]
        public void IsTemporaryExtension_ReturnsTrue(string ext)
        {
            Assert.True(InstallPlanRules.IsTemporaryExtension(ext));
        }

        [Theory]
        [InlineData(".exe")]
        [InlineData(".dll")]
        [InlineData(".json")]
        [InlineData("")]
        [InlineData(null)]
        public void IsTemporaryExtension_ReturnsFalse(string? ext)
        {
            Assert.False(InstallPlanRules.IsTemporaryExtension(ext));
        }

        // ---------- IsTemporaryFolder ----------

        [Fact]
        public void IsTemporaryFolder_ReturnsTrue_ForSmallTemporaryFiles()
        {
            var files = new List<FileSizeInfo>
            {
                new FileSizeInfo(1024, ".tmp"),
                new FileSizeInfo(2048, ".log")
            };
            Assert.True(InstallPlanRules.IsTemporaryFolder(3072, files));
        }

        [Fact]
        public void IsTemporaryFolder_ReturnsFalse_WhenFileTooLarge()
        {
            var files = new List<FileSizeInfo>
            {
                new FileSizeInfo(3 * 1024 * 1024, ".tmp") // > 2 МБ
            };
            Assert.False(InstallPlanRules.IsTemporaryFolder(3 * 1024 * 1024, files));
        }

        [Fact]
        public void IsTemporaryFolder_ReturnsFalse_WhenNonTemporaryExtension()
        {
            var files = new List<FileSizeInfo>
            {
                new FileSizeInfo(100, ".exe")
            };
            Assert.False(InstallPlanRules.IsTemporaryFolder(100, files));
        }

        [Fact]
        public void IsTemporaryFolder_ReturnsFalse_WhenTotalTooLarge()
        {
            // 3 файла по 4 МБ = 12 МБ > 10 МБ
            var files = new List<FileSizeInfo>
            {
                new FileSizeInfo(4 * 1024 * 1024, ".log"),
                new FileSizeInfo(4 * 1024 * 1024, ".log"),
                new FileSizeInfo(4 * 1024 * 1024, ".log")
            };
            Assert.False(InstallPlanRules.IsTemporaryFolder(12 * 1024 * 1024, files));
        }

        // ---------- IsChromeTemporaryFolder ----------

        [Fact]
        public void IsChromeTemporaryFolder_ReturnsTrue_ForSmallFiles()
        {
            Assert.True(InstallPlanRules.IsChromeTemporaryFolder(512 * 1024, new List<long> { 512 * 1024 }));
        }

        [Fact]
        public void IsChromeTemporaryFolder_ReturnsFalse_WhenFileExceedsLimit()
        {
            // файл 1.5 МБ > 1 МБ
            Assert.False(InstallPlanRules.IsChromeTemporaryFolder(1536 * 1024, new List<long> { 1536 * 1024 }));
        }

        [Fact]
        public void IsChromeTemporaryFolder_ReturnsFalse_WhenTotalExceedsLimit()
        {
            // 3 файла по 2 МБ = 6 МБ > 5 МБ
            Assert.False(InstallPlanRules.IsChromeTemporaryFolder(6 * 1024 * 1024, new List<long> { 2 * 1024 * 1024, 2 * 1024 * 1024, 2 * 1024 * 1024 }));
        }

        // ---------- GetInstallerLeftPath ----------

        [Fact]
        public void GetInstallerLeftPath_CombinesAppDirWithFileName()
        {
            var result = InstallPlanRules.GetInstallerLeftPath(@"C:\Apps\Opera", @"C:\Temp\opera_setup.exe");
            Assert.StartsWith(@"C:\Apps\Opera\", result);
            Assert.EndsWith("opera_setup.exe", result);
        }

        // ---------- PotentialEmptyFolders ----------

        [Fact]
        public void PotentialEmptyFolders_ContainsSystemApps()
        {
            Assert.Contains("Google Chrome", InstallPlanRules.PotentialEmptyFolders);
            Assert.Contains("Microsoft VC++ 2015-2022 Redistributable (x64)", InstallPlanRules.PotentialEmptyFolders);
            Assert.Contains("VC++ 2013 Redistributable (x86)", InstallPlanRules.PotentialEmptyFolders);
        }

        [Fact]
        public void PotentialEmptyFolders_CoversAllExpectedEntries()
        {
            var expected = new[]
            {
                "Google Chrome",
                "Microsoft VC++ 2015-2022 Redistributable (x64)",
                "Microsoft VC++ 2015-2022 Redistributable (x86)",
                "VC++ 2013 Redistributable (x64)",
                "VC++ 2013 Redistributable (x86)"
            };
            Assert.Equal(expected.OrderBy(n => n),
                         InstallPlanRules.PotentialEmptyFolders.OrderBy(n => n));
        }
    }
}
