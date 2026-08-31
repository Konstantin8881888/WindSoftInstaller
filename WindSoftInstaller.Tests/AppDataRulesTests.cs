using System;
using System.Collections.Generic;
using WindSoftInstaller.Services;
using Xunit;

namespace WindSoftInstaller.Tests
{
    public class AppDataRulesTests
    {
        // ---------- ToMegaBytes ----------

        [Fact]
        public void ToMegaBytes_ReturnsRoundedMegaBytes()
        {
            // 10485760 байт = 10 МБ
            Assert.Equal(10.0, AppDataRules.ToMegaBytes(10L * 1024 * 1024));
        }

        [Theory]
        [InlineData(0, 0.0)]
        [InlineData(1048576, 1.0)]
        [InlineData(524288, 0.5)]
        public void ToMegaBytes_VariousValues(long bytes, double expected)
        {
            Assert.Equal(expected, AppDataRules.ToMegaBytes(bytes));
        }

        [Fact]
        public void ToMegaBytes_RoundsToTwoDecimals()
        {
            // 1.5 МБ -> 1.5
            Assert.Equal(1.5, AppDataRules.ToMegaBytes((long)(1.5 * 1024 * 1024)));
        }

        // ---------- GetLocalizedFileName ----------

        [Fact]
        public void GetLocalizedFileName_ReturnsRussian_ForRu()
        {
            Assert.Equal("ru.exe", AppDataRules.GetLocalizedFileName("ru", "en.exe", "ru.exe"));
        }

        [Theory]
        [InlineData("en")]
        [InlineData("de")]
        [InlineData(null)]
        public void GetLocalizedFileName_ReturnsEnglish_ForNonRu(string? lang)
        {
            Assert.Equal("en.exe", AppDataRules.GetLocalizedFileName(lang, "en.exe", "ru.exe"));
        }

        [Fact]
        public void GetLocalizedFileName_CaseInsensitive_Ru()
        {
            Assert.Equal("ru.exe", AppDataRules.GetLocalizedFileName("RU", "en.exe", "ru.exe"));
        }

        // ---------- RemoveLanguageParamIfNotRussian ----------

        [Fact]
        public void RemoveLanguageParam_Removes_ForNonRu()
        {
            var dict = new Dictionary<string, string> { ["param.Language"] = "/LANG=ru", ["param.Silent"] = "/S" };
            Assert.True(AppDataRules.RemoveLanguageParamIfNotRussian("en", dict));
            Assert.False(dict.ContainsKey("param.Language"));
            Assert.True(dict.ContainsKey("param.Silent"));
        }

        [Fact]
        public void RemoveLanguageParam_DoesNotRemove_ForRu()
        {
            var dict = new Dictionary<string, string> { ["param.Language"] = "/LANG=ru" };
            Assert.False(AppDataRules.RemoveLanguageParamIfNotRussian("ru", dict));
            Assert.True(dict.ContainsKey("param.Language"));
        }

        [Fact]
        public void RemoveLanguageParam_DoesNotRemove_WhenParamAbsent()
        {
            var dict = new Dictionary<string, string> { ["param.Other"] = "/S" };
            Assert.False(AppDataRules.RemoveLanguageParamIfNotRussian("en", dict));
            Assert.True(dict.ContainsKey("param.Other"));
        }

        [Fact]
        public void RemoveLanguageParam_HandlesNullDictionary()
        {
            Assert.False(AppDataRules.RemoveLanguageParamIfNotRussian("en", null!));
        }
    }
}
