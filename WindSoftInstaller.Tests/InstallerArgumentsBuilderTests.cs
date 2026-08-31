using System;
using Xunit;

namespace WindSoftInstaller.Tests
{
    public class InstallerArgumentsBuilderTests
    {
        private static InstallableApp MakeApp(string name, string pathKey = "/D=",
            Dictionary<string, string>? custom = null)
        {
            return new InstallableApp
            {
                Name = name,
                ExecutablePath = name,
                PathParameterKey = pathKey,
                SizeMB = 10.0,
                LicenseUrl = "https://example.com/license",
                CustomParameters = custom ?? new Dictionary<string, string>()
            };
        }

        [Fact]
        public void BuildArguments_ReturnsEmpty_WhenNoCustomParams_AndNotSpecial()
        {
            var app = MakeApp("Some App");
            var builder = new InstallerArgumentsBuilder(app, "C:\\setup.exe", "C:\\Program Files\\App");

            var result = builder.BuildArguments();

            Assert.Equal("/D=\"C:\\Program Files\\App\"", result);
        }

        [Fact]
        public void BuildArguments_QuotesInstallDir_WhenItContainsSpaces()
        {
            var app = MakeApp("Some App");
            var builder = new InstallerArgumentsBuilder(app, "C:\\setup.exe", "C:\\Program Files\\My App");

            var result = builder.BuildArguments();

            Assert.Equal("/D=\"C:\\Program Files\\My App\"", result);
        }

        [Fact]
        public void BuildArguments_DoesNotUsePathParameter_ForVlc()
        {
            var app = MakeApp("VLC Media Player");
            var builder = new InstallerArgumentsBuilder(app, "C:\\vlc\\setup.exe", "C:\\MyApps");

            var result = builder.BuildArguments();

            Assert.DoesNotContain("/D=", result);
            Assert.Equal("", result);
        }

        [Fact]
        public void BuildArguments_UsesSpecialPathKey_ForRivaTuner()
        {
            var app = MakeApp("RivaTuner Statistics Server", pathKey: "/D=");
            var builder = new InstallerArgumentsBuilder(app, "C:\\rtss\\setup.exe", "C:\\RTSS");

            var result = builder.BuildArguments();

            Assert.Equal("/D=C:\\RTSS", result);
        }

        [Fact]
        public void BuildArguments_ReplacesInstallDirToken_InCustomParameter()
        {
            var app = MakeApp("App", "/D=", new Dictionary<string, string>
            {
                { "Param", "/DIR={InstallDir}" }
            });
            var builder = new InstallerArgumentsBuilder(app, "C:\\setup.exe", "C:\\Install\\X");

            var result = builder.BuildArguments();

            Assert.Contains("/DIR=C:\\Install\\X", result);
        }

        [Fact]
        public void BuildArguments_SkipsNullOrWhitespaceCustomParameters()
        {
            var app = MakeApp("App", "/D=", new Dictionary<string, string>
            {
                { "Empty", "   " }
            });
            var builder = new InstallerArgumentsBuilder(app, "C:\\setup.exe", "C:\\Install\\X");

            var result = builder.BuildArguments();

            Assert.DoesNotContain("Empty", result);
        }

        [Fact]
        public void BuildStartInfo_UsesMsiexec_ForMsiPackage()
        {
            var app = MakeApp("Calibre", "/D=", new Dictionary<string, string>
            {
                { "TargetDir", "TARGETDIR={InstallDir}" }
            });
            var builder = new InstallerArgumentsBuilder(app, "C:\\calibre\\setup.msi", "C:\\Lib");

            var startInfo = builder.BuildStartInfo();

            Assert.Equal("msiexec.exe", startInfo.FileName);
            Assert.StartsWith("/a \"C:\\calibre\\setup.msi\" /qn", startInfo.Arguments);
            Assert.Contains("TARGETDIR=C:\\Lib", startInfo.Arguments);
        }

        [Fact]
        public void BuildStartInfo_UsesSourceDirectly_ForNonMsi()
        {
            var app = MakeApp("App", "/D=");
            var builder = new InstallerArgumentsBuilder(app, "C:\\setup.exe", "C:\\App");

            var startInfo = builder.BuildStartInfo();

            Assert.Equal("C:\\setup.exe", startInfo.FileName);
            Assert.Contains("/D=C:\\App", startInfo.Arguments);
        }

        [Fact]
        public void Constructor_Throws_WhenAppIsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new InstallerArgumentsBuilder(null!, "C:\\setup.exe", "C:\\App"));
        }
    }
}
