using System.Runtime.Versioning;
using SharpCompress.Archives;
using static Localization;

namespace WindSoftInstaller
{
    [SupportedOSPlatform("windows")]
    internal class AppRepository
    {
        public static List<InstallableApp> LoadApps()
        {
            string archivePath = Path.Combine(Application.StartupPath, "Installers.7z");
            if (!File.Exists(archivePath))
            {
                throw new FileNotFoundException($"Архив не найден: {archivePath}");
            }

            // Создаем словарь для хранения размеров файлов
            var fileSizes = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

            // Получаем размеры файлов из архива
            using (var archive = ArchiveFactory.Open(archivePath))
            {
                foreach (var entry in archive.Entries)
                {
                    if (!entry.IsDirectory)
                    {
                        fileSizes[entry.Key!] = entry.Size;
                    }
                }
            }

            var apps = new List<InstallableApp>
            {
                new InstallableApp()
                {
                    Name = "vlc-3.0.21",
                    DescriptionKey = "app.vlc.Description",
                    ExecutablePath = "vlc-3.0.21.exe",
                    SizeMB = 182,
                    //SizeMB = Math.Round(fileSizes["vlc-3.0.21.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://www.videolan.org/legal.html",
                    PathParameterKey = "/D=",
                    CustomParameters =
                    {
                        { "param.InstallMode", "/S" },
                        { "param.Language",
                            "/L=" + (Current == "ru" ? "ru" : "en") }
                    }
                },
                new InstallableApp()
                {
                    Name = "MPC-HC",
                    DescriptionKey = "app.mpc.Description",
                    ExecutablePath = "MPC-HC.1.7.13.x64.exe",
                    SizeMB = 47,
                    //SizeMB = Math.Round(fileSizes["MPC-HC.1.7.13.x64.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://github.com/mpc-hc/mpc-hc/blob/develop/COPYING.txt",
                    PathParameterKey = "/DIR=",
                    CustomParameters =
                    {
                        { "param.SilentInstall", "/VERYSILENT" },
                        { "param.NoRestart", "/NORESTART" },
                        { "param.Language",
                            Current == "ru"
                    ? "/LANG=Russian"
                    : "/LANG=English" }
                    }
                },
                new InstallableApp()
                {
                    Name = "SMPlayer",
                    DescriptionKey = "app.smplayer.Description",
                    ExecutablePath = "smplayer-25.6.0-x64-unsigned.exe",
                    SizeMB = 226,
                    //SizeMB = Math.Round(fileSizes["smplayer-25.6.0-x64-unsigned.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://github.com/smplayer-dev/smplayer?tab=GPL-2.0-1-ov-file#readme",
                    PathParameterKey = "/D=",
                    CustomParameters =
                    {
                        { "param.SilentInstall", "/S" }
                    }
                },
                new InstallableApp()
                {
                    Name = "GIMP",
                    DescriptionKey = "app.gimp.Description",
                    ExecutablePath = "gimp-3.0.6-setup.exe",
                    SizeMB = 819,
                    //SizeMB = Math.Round(fileSizes["gimp-3.0.6-setup.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://www.gimp.org/about/COPYING",
                    PathParameterKey = "/DIR=",
                    CustomParameters =
                    {
                        { "param.InstallMode", "/VERYSILENT" },
                        { "param.SuppressMsg", "/SUPPRESSMSGBOXES" },
                        { "param.NoRestart", "/NORESTART" },
                        { "param.DisableSmartScreen", "/SP-" },
                        { "param.Language",
                            Current == "ru"
                    ? "/LANG=russian"
                    : "/LANG=english" }
                    }
                },
                new InstallableApp()
                {
                    Name = "Paint.NET",
                    DescriptionKey = "app.paintnet.Description",
                    ExecutablePath = "paint.net.5.1.9.install.x64.exe",
                    SizeMB = 616,
                    //SizeMB = Math.Round(fileSizes["paint.net.5.1.9.install.x64.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://www.getpaint.net/license.html",
                    PathParameterKey = "TARGETDIR=",
                    CustomParameters =
                    {
                        { "param.AutoInstall", "/auto" },
                        { "param.SkipConfig", "/skipConfig" },
                        { "param.Language",
                            Current == "ru"
                    ? "/language=ru"
                    : "/language=en" }
                    }
                },
                new InstallableApp()
                {
                    Name = "Shotcut",
                    DescriptionKey = "app.shotcut.Description",
                    ExecutablePath = "shotcut-win64-251031.exe",
                    SizeMB = 489,
                    //SizeMB = Math.Round(fileSizes["shotcut-win64-251031.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://github.com/mltframework/shotcut/blob/master/COPYING",
                    PathParameterKey = "/DIR=",
                    CustomParameters =
                    {
                        { "param.SilentInstall", "/VERYSILENT" },
                        { "param.NoRestart", "/NORESTART" },
                        { "param.CurrentUser", "/CURRENTUSER" },
                        { "param.DesktopShortcut", "/MERGETASKS=desktopicon" }
                    }
                },
                new InstallableApp()
                {
                    Name = "VSDC Free Video Editor",
                    DescriptionKey = "app.vsdc.Description",
                    ExecutablePath = "video_editor_x64.exe",
                    SizeMB = 282,
                    //SizeMB = Math.Round(fileSizes["video_editor_x64.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://www.videosoftdev.com/terms-and-conditions",
                    PathParameterKey = "/DIR=",
                    CustomParameters =
                    {
                        { "param.SilentInstall", "/VERYSILENT" },
                        { "param.NoRestart", "/NORESTART" }
                    }
                },
                new InstallableApp()
                {
                    Name = "Google Chrome",
                    DescriptionKey = "app.chrome.Description",
                    ExecutablePath = "googlechromestandaloneenterprise64.msi",
                    SizeMB = 1230,
                    //SizeMB = Math.Round(fileSizes["googlechromestandaloneenterprise64.msi"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://www.google.com/chrome/terms/",
                    PathParameterKey = "INSTALLDIR=",
                    CustomParameters =
                    {
                        { "param.SilentInstall", "/qn" }
                    }
                },
                new InstallableApp()
                {
                    Name = "Mozilla Firefox",
                    DescriptionKey = "app.firefox.Description",
                    // Динамически выбираем файл в зависимости от языка
                    ExecutablePath = GetLocalizedExecutableName(
                        "Firefox Setup 144.0.2.msi",          // Английская версия
                        "Firefox Setup 144.0.2ru.msi"         // Русская версия
                    ),
                    SizeMB = 309,
                    // Размер тоже нужно вычислять динамически, так как файлы могут быть разного размера
                    //SizeMB = Math.Round(fileSizes[GetLocalizedExecutableName(
                    //    "Firefox Setup 144.0.2.msi",
                    //    "Firefox Setup 144.0.2ru.msi"
                    //)] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://www.mozilla.org/en-US/MPL/2.0/",
                    CustomParameters = new Dictionary<string, string>
                    {
                        { "param.SilentInstall", "/qn" },
                        { "param.InstallPath", "INSTALL_DIRECTORY_PATH=\"{InstallDir}\"" }
                    }
                },
                new InstallableApp()
                {
                    Name = "Opera",
                    DescriptionKey = "app.opera.Description",
                    ExecutablePath = "Opera_123.0.5669.47_Setup_x64.exe",
                    SizeMB = 451,
                    //SizeMB = Math.Round(fileSizes["Opera_123.0.5669.47_Setup_x64.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://www.opera.com/legal",
                    PathParameterKey = "--installfolder=",
                    CustomParameters =
                    {
                        { "param.SilentInstall", "--silent" },
                        // Установка только для текущего пользователя; если нужно для всех, поставить allusers=1
                        { "param.CurrentUserOnly", "--allusers=0" },
                        //{ "param.InstallLanguage", "--language=ru" },
                        // Отключаем автозапуск после установки
                        { "param.DontLaunchAfter", "--launchopera=0" },
                        // Ярлык на рабочем столе (по умолчанию Opera его создаёт, но продублировать не вредно)
                        { "param.CreateDesktopShortcut", "--desktopshortcut=1" },
                        // Отключаем автообновления (если нужно)
                        //{ "param.DisableAutoUpdate", "--no-update" }
                    }
                },
                new InstallableApp()
                {
                    Name = "Audacity",
                    DescriptionKey = "app.audacity.Description",
                    ExecutablePath = "audacity-win-3.7.5-64bit.exe",
                    SizeMB = 88,
                    //SizeMB = Math.Round(fileSizes["audacity-win-3.7.5-64bit.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://www.audacityteam.org/about/license/",
                    PathParameterKey = "/DIR=",
                    CustomParameters =
                    {
                        { "param.VerySilent", "/VERYSILENT" },
                        { "param.NoRestart", "/NORESTART" },
                        // Отключение ассоциации с файлами
                        { "param.DisableAssociations", "/ASSOCIATE=0" }
                    }
                },
                new InstallableApp()
                {
                    Name = "LMMS",
                    DescriptionKey = "app.lmms.Description",
                    ExecutablePath = "lmms-1.2.2-win64.exe",
                    SizeMB = 101,
                    //SizeMB = Math.Round(fileSizes["lmms-1.2.2-win64.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://github.com/LMMS/lmms/blob/master/LICENSE.txt",
                    PathParameterKey = "/D=",
                    CustomParameters =
                    {
                        { "param.SilentInstall", "/S" }
                    }
                },
                new InstallableApp()
                {
                    Name = "HandBrake",
                    DescriptionKey = "app.handbrake.Description",
                    ExecutablePath = "HandBrake-1.10.2-x86_64-Win_GUI.exe",
                    SizeMB = 103,
                    //SizeMB = Math.Round(fileSizes["HandBrake-1.10.2-x86_64-Win_GUI.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://github.com/HandBrake/HandBrake/blob/master/COPYING",
                    PathParameterKey = "/D=",
                    CustomParameters =
                    {
                        { "param.SilentInstall", "/S" }
                    }
                },
                new InstallableApp()
                {
                    Name = "XMedia Recode",
                    DescriptionKey = "app.xmedia.Description",
                    ExecutablePath = "XMediaRecode3619_x64_setup.exe",
                    SizeMB = 94,
                    //SizeMB = Math.Round(fileSizes["XMediaRecode3619_x64_setup.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://www.xmedia-recode.de/en/",
                    PathParameterKey = "/DIR=",
                    CustomParameters =
                    {
                        { "param.SilentInstall", "/VERYSILENT" },
                        { "param.NoRestart", "/NORESTART" },
                        //{ "param.Language", "/LANG=Russian" }, Язык устанавливается копированием файлов
                        { "param.CreateDesktopShortcut", "/MERGETASKS=desktopicon" }
                    },
                        // Новые свойства для языкового файла
                        AdditionalFiles = new List<string> { "XMediaRecode.json", "Fav.ini" },
                        AdditionalFilesDestinations = new Dictionary<string, string>
                        {
                            { "XMediaRecode.json", "XMediaRecode.json" }, // Имя файла в целевой папке
                            { "Fav.ini", "Fav.ini" } // Имя файла в целевой папке
                        }
                },
                new InstallableApp()
                {
                    Name = "OpenShot",
                    DescriptionKey = "app.openshot.Description",
                    ExecutablePath = "OpenShot-v3.3.0-x86_64.exe",
                    SizeMB = 653,
                    //SizeMB = Math.Round(fileSizes["OpenShot-v3.3.0-x86_64.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://www.gnu.org/licenses/gpl-3.0.html",
                    PathParameterKey = "/DIR=",
                    CustomParameters =
                    {
                        { "param.SilentInstall", "/VERYSILENT" },
                        { "param.NoRestart", "/NORESTART" },
                        { "param.CreateDesktopShortcut", "/MERGETASKS=desktopicon" }
                    }
                },
                new InstallableApp()
                {
                    Name = "AIMP",
                    DescriptionKey = "app.aimp.Description",
                    ExecutablePath = "aimp_5.40.2696_w64.exe",
                    SizeMB = 115,
                    //SizeMB = Math.Round(fileSizes["aimp_5.40.2696_w64.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://www.aimp.ru/?do=eula&os=windows",
                    PathParameterKey = "/AUTO=",
                    CustomParameters =
                    {
                        { "param.SilentInstall", "/SILENT" },
                    }
                },
                new InstallableApp()
                {
                    Name = "Clementine",
                    DescriptionKey = "app.clementine.Description",
                    ExecutablePath = "ClementineSetup-1.4.1-18-g4ab6f35ec.exe",
                    SizeMB = 319,
                    //SizeMB = Math.Round(fileSizes["ClementineSetup-1.4.1-18-g4ab6f35ec.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://www.gnu.org/licenses/gpl-3.0.html",
                    PathParameterKey = "/D=",
                    CustomParameters =
                    {
                        { "param.InstallMode", "/S" }
                    }
                },
                new InstallableApp()
                {
                    Name = "7-Zip",
                    DescriptionKey = "app.7zip.Description",
                    ExecutablePath = "7z2501-x64.exe",
                    SizeMB = 6,
                    //SizeMB = Math.Round(fileSizes["7z2501-x64.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://www.7-zip.org/license.txt",
                    PathParameterKey = "/D=",
                    CustomParameters =
                    {
                        { "param.InstallMode", "/S" }
                    },
                    ShortcutRelativePath = "7zFM.exe",
                    ShortcutName = "7-Zip"
                },
                new InstallableApp()
                {
                    Name = "qBittorrent",
                    DescriptionKey = "app.qbittorrent.Description",
                    ExecutablePath = "qbittorrent_5.1.0_x64_setup.exe",
                    SizeMB = 214,
                    //SizeMB = Math.Round(fileSizes["qbittorrent_5.1.0_x64_setup.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://www.gnu.org/licenses/gpl-2.0.html",
                    PathParameterKey = "/D=",
                    CustomParameters =
                    {
                        { "param.InstallMode", "/S" },
                        { "param.DisableStartMenu", "/NOADDTOSTART" },
                        { "param.EnableAssociations", "/ASSOCIATE" }
                    },
                    ShortcutName = "qBittorrent",
                    ShortcutRelativePath = "qbittorrent.exe"
                },
                new InstallableApp()
                {
                    Name = "HWiNFO64",
                    DescriptionKey = "app.hwinfo64.Description",
                    ExecutablePath = "hwi64_832.exe",
                    SizeMB = Math.Round(fileSizes["hwi64_832.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://www.hwinfo.com/licenses/",
                    PathParameterKey = "/DIR=",
                    CustomParameters =
                    {
                        { "param.VerySilent", "/VERYSILENT" },
                        { "param.SuppressMessages", "/SUPPRESSMSGBOXES" },
                        { "param.NoRestart", "/NORESTART" }
                    },
                    ShortcutRelativePath = "HWiNFO64.exe",
                    ShortcutName = "HWiNFO64"
                },
                new InstallableApp()
                {
                    Name = "PDF‑XChange Editor",
                    DescriptionKey = "app.pdfxchange.Description",
                    ExecutablePath = "EditorV10.x64.msi",
                    SizeMB = 438,
                    //SizeMB = Math.Round(fileSizes["EditorV10.x64.msi"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://www.tracker-software.com/PDFXLicense.pdf",
                    PathParameterKey = "",
                    CustomParameters =
                    {
                        { "param.SilentInstall", "/quiet" },
                        { "param.NoRestart", "/norestart" },
                        { "param.Language", "EDITOR_LANGUAGE=ru-RU" },
                        { "param.InstallPath", "INSTALLLOCATION=\"{InstallDir}\"" }
                    }
                },
                new InstallableApp()
                {
                    Name = "ClamWin",
                    DescriptionKey = "app.clamwin.Description",
                    ExecutablePath = "clamwin-0.103.2.1-setup.exe",
                    SizeMB = 34,
                    //SizeMB = Math.Round(fileSizes["clamwin-0.103.2.1-setup.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://github.com/clamwin/clamwin/blob/master/COPYING",
                    PathParameterKey = "/DIR=",
                    CustomParameters =
                    {
                        { "param.SilentInstall", "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART" },
                        { "param.Language", "/LANG=Russian" },
                        { "param.Components", "/COMPONENTS=\"main,context_menu,russian\"" },
                        { "param.DisableSmartScreen", "/SP-" }
                    }
                },
                new InstallableApp()
                {
                    Name = "Emsisoft Emergency Kit",
                    DescriptionKey = "app.emsisoft.Description",
                    ExecutablePath = "EmsisoftEmergencyKit1.zip",
                    SizeMB = 384,
                    //SizeMB = Math.Round(fileSizes["EmsisoftEmergencyKit1.zip"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://www.emsisoft.com/en/eula/?utm_source=chatgpt.com",
                    IsPortable = true,
                    ShortcutName = "Emsisoft Emergency Kit",
                    ShortcutRelativePath = "Start Scanner.exe"
                },
                new InstallableApp()
                {
                    Name = "Cryptomator",
                    DescriptionKey = "app.cryptomator.Description",
                    ExecutablePath = "Cryptomator-1.17.1-x64.msi",
                    SizeMB = 124,
                    //SizeMB = Math.Round(fileSizes["Cryptomator-1.17.1-x64.msi"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://github.com/cryptomator/cryptomator/blob/master/LICENSE",
                    PathParameterKey = "",
                    CustomParameters =
                    {
                        { "param.SilentInstall", "/quiet" },
                        { "param.NoRestart", "/norestart" },
                        { "param.InstallPath", "INSTALLDIR=\"{InstallDir}\"" }
                    }
                },
                new InstallableApp()
                {
                    Name = "KeePass",
                    DescriptionKey = "app.keepass.Description",
                    ExecutablePath = "KeePass-2.60-Setup.exe",
                    SizeMB = 9,
                    //SizeMB = Math.Round(fileSizes["KeePass-2.60-Setup.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://keepass.info/help/v2/license.html",
                    PathParameterKey = "/DIR=",
                    CustomParameters =
                    {
                        { "param.SilentInstall", "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART" },
                        { "param.Language", "/LANG=English" }
                    },
                    AdditionalFiles = ["Russian.lngx", "KeePass.config.xml"],
                    AdditionalFilesDestinations =
                    {
                        { "Russian.lngx", "Languages\\Russian.lngx" },
                        { "KeePass.config.xml", "KeePass.config.xml" }
                    },
                    LanguageConfig = "Russian"
                },
                new InstallableApp()
                {
                    Name = "Bitwarden",
                    DescriptionKey = "app.bitwarden.Description",
                    ExecutablePath = "Bitwarden-Portable-2025.10.0.exe",
                    SizeMB = 244,
                    //SizeMB = Math.Round(fileSizes["Bitwarden-Portable-2025.10.0.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://github.com/bitwarden/clients/blob/main/LICENSE_BITWARDEN.txt",
                    IsPortable = true,
                    ShortcutName = "Bitwarden",
                    CustomParameters =
                    {
                        { "param.ExtractSilent", "/VERYSILENT" }
                    }
                },
                new InstallableApp()
                {
                    Name = "Wise Disk Cleaner",
                    DescriptionKey = "app.wisedisk.Description",
                    ExecutablePath = "WDCFree_11.2.7.847.exe",
                    SizeMB = 22,
                    //SizeMB = Math.Round(fileSizes["WDCFree_11.2.7.847.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://www.wisecleaner.com/eula.html",
                    PathParameterKey = "/DIR=",
                    CustomParameters =
                    {
                        { "param.SilentInstall", "/VERYSILENT" }
                    },
                    ShortcutName = "Wise Disk Cleaner"
                },
                new InstallableApp()
                {
                    Name = "BleachBit",
                    DescriptionKey = "app.bleachbit.Description",
                    ExecutablePath = "BleachBit-5.0.2-setup.exe",
                    SizeMB = 43,
                    //SizeMB = Math.Round(fileSizes["BleachBit-5.0.2-setup.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://github.com/bleachbit/bleachbit/blob/master/COPYING",
                    PathParameterKey = "/D=",
                    CustomParameters =
                    {
                        { "param.SilentInstall", "/S" },
                        { "param.AllUsers", "/allusers" }
                    },
                    ShortcutName = "BleachBit"
                },
                new InstallableApp()
                {
                    Name = "UltraDefrag",
                    DescriptionKey = "app.ultradefrag.Description",
                    ExecutablePath = "ultradefrag-7.1.4.bin.amd64.exe",
                    SizeMB = 8,
                    //SizeMB = Math.Round(fileSizes["ultradefrag-7.1.4.bin.amd64.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://ultradefrag.net/en/license",
                    PathParameterKey = "/D=",
                    CustomParameters =
                    {
                        { "param.SilentInstall", "/S" }
                    }
                },
                new InstallableApp()
                {
                    Name = "RetroArch",
                    DescriptionKey = "app.retroarch.Description",
                    ExecutablePath = "RetroArchPortable.zip",
                    SizeMB = 524,
                    //SizeMB = Math.Round(fileSizes["RetroArchPortable.zip"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://www.gnu.org/licenses/gpl-3.0.html",
                    IsPortable = true,
                    ShortcutRelativePath = "retroarch.exe",
                    ShortcutName = "RetroArch"
                },
                new InstallableApp()
                {
                    Name = "PCSX2",
                    DescriptionKey = "app.pcsx2.Description",
                    ExecutablePath = "pcsx2-v2.4.0-windows-x64-Qt.7z",
                    SizeMB = 96,
                    //SizeMB = Math.Round(fileSizes["pcsx2-v2.4.0-windows-x64-Qt.7z"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://github.com/PCSX2/pcsx2/blob/master/pcsx2/Docs/License.txt",
                    IsPortable = true,
                    ShortcutRelativePath = "pcsx2-qt.exe",
                    ShortcutName = "PCSX2"
                },
                new InstallableApp()
                {
                    Name = "Microsoft VC++ 2015-2022 Redistributable (x64)",
                    DescriptionKey = "app.vcredist.x64.Description",
                    SizeMB = Math.Round(fileSizes["vc_redist.x64.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist",
                    ExecutablePath = "vc_redist.x64.exe",
                    PathParameterKey = "",
                    CustomParameters = new Dictionary<string, string>
                    {
                        { "param.SilentInstall", "/install /quiet /norestart" }
                    },
                    ShortcutName = null
                },
                new InstallableApp()
                {
                    Name = "Microsoft VC++ 2015-2022 Redistributable (x86)",
                    DescriptionKey = "app.vcredist.x86.Description",
                    ExecutablePath = "vc_redist.x86.exe",
                    SizeMB = Math.Round(fileSizes["vc_redist.x86.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist",
                    PathParameterKey = "",
                    CustomParameters = new Dictionary<string, string>
                    {
                        { "param.SilentInstall", "/quiet /norestart" }
                    },
                    ShortcutName = null
                },
                new InstallableApp()
                {
                    Name = "MSI Afterburner",
                    DescriptionKey = "app.afterburner.Description",
                    ExecutablePath = "MSIAfterburnerPortable.zip",
                    ArchivePathEn = "MSIAfterburnerPortable.zip", // Английская версия
                    ArchivePathRu = "MSIAfterburnerPortableRu.zip", // Русская версия
                    SizeMB = 41,
                    //SizeMB = Math.Round(fileSizes["MSIAfterburnerPortable.zip"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://www.msi.com/page/eula",
                    IsPortable = true,
                    ShortcutName = "MSI Afterburner",
                    ShortcutRelativePath = "MSIAfterburner.exe",
                },
                new InstallableApp()
                {
                    Name = "RivaTuner Statistics Server",
                    DescriptionKey = "app.rivatuner.Description",
                    ExecutablePath = "RTSSSetup737.exe",
                    SizeMB = 97,
                    //SizeMB = Math.Round(fileSizes["RTSSSetup737.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://www.msi.com/page/eula",
                    PathParameterKey = "/D=",
                    CustomParameters = new Dictionary<string, string>
                    {
                        { "param.SilentInstall", "/S" }
                    }
                },
                new InstallableApp()
                {
                    Name = "Anki",
                    DescriptionKey = "app.anki.Description",
                    ExecutablePath = "anki-25.02.7-windows-qt6.exe",
                    SizeMB = 504,
                    //SizeMB = Math.Round(fileSizes["anki-25.02.7-windows-qt6.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://github.com/ankitects/anki/blob/main/LICENSE",
                    PathParameterKey = "/D=",
                    CustomParameters = new Dictionary<string, string>
                    {
                        { "param.SilentInstall", "/S" }
                    },
                    ShortcutName = "Anki",
                    ShortcutRelativePath = "anki.exe"
                },
                new InstallableApp()
                {
                    Name = "Kiwix Desktop",
                    DescriptionKey = "app.kiwix.Description",
                    ExecutablePath = "kiwix-desktop_windows_x64_2.4.1.zip",
                    SizeMB = 267,
                    //SizeMB = Math.Round(fileSizes["kiwix-desktop_windows_x64_2.4.1.zip"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://github.com/kiwix/kiwix-desktop/blob/master/LICENSE",
                    IsPortable = true,
                    ShortcutName = "Kiwix Desktop",
                    ShortcutRelativePath = "kiwix-desktop.exe"
                },
                new InstallableApp()
                {
                    Name = "Calibre",
                    DescriptionKey = "app.calibre.Description",
                    ExecutablePath = "calibre-64bit-8.14.0.msi",
                    SizeMB = 561,
                    //SizeMB = Math.Round(fileSizes["calibre-64bit-8.14.0.msi"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://calibre-ebook.com/license",
                    PathParameterKey = "",
                    CustomParameters = new Dictionary<string, string>
                    {
                        { "param.SilentInstall", "/quiet" },
                        { "param.NoRestart", "/norestart" },
                        { "param.InstallPath", "TARGETDIR=\"{InstallDir}\"" }
                    },
                    ShortcutName = "Calibre",
                    ShortcutRelativePath = "PFiles64\\Calibre2\\calibre.exe"
                },
                new InstallableApp()
                {
                    Name = "Zotero",
                    DescriptionKey = "app.zotero.Description",
                    ExecutablePath = "Zotero-7.0.27_x64_setup.exe",
                    SizeMB = 202,
                    //SizeMB = Math.Round(fileSizes["Zotero-7.0.27_x64_setup.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://www.zotero.org/license/",
                    PathParameterKey = "/D=",
                    CustomParameters = new Dictionary<string, string>
                    {
                        { "param.SilentInstall", "/S" }
                    },
                    ShortcutName = null
                },
                new InstallableApp()
                {
                    Name = "PDFsam Basic",
                    DescriptionKey = "app.pdfsam.Description",
                    ExecutablePath = "pdfsam-basic-5.4.1-windows-x64.msi",
                    SizeMB = 101,
                    //SizeMB = Math.Round(fileSizes["pdfsam-basic-5.4.1-windows-x64.msi"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://github.com/torakiki/pdfsam/blob/develop/LICENSE",
                    PathParameterKey = "",
                    CustomParameters =
                    {
                        { "param.SkipThanksPage", "SKIPTHANKSPAGE=Yes" },
                        { "param.DisableUpdateCheck", "CHECK_FOR_UPDATES=false" },
                        { "param.InstallPath", "TARGETDIR=\"{InstallDir}\"" }
                    },
                    ShortcutName = "PDFsam Basic",
                    ShortcutRelativePath = "PFiles\\PDFsam Basic\\pdfsam.exe"
                },
                new InstallableApp()
                {
                    Name = "PDF24 Creator",
                    DescriptionKey = "app.pdf24.Description",
                    ExecutablePath = "pdf24-creator-11.28.2-x64.msi",
                    SizeMB = 1010,
                    //SizeMB = Math.Round(fileSizes["pdf24-creator-11.28.2-x64.msi"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://www.pdf24.org/en/terms-of-use",
                    PathParameterKey = "",
                    CustomParameters = new Dictionary<string, string>
                    {
                        { "param.SilentInstall", "/quiet" },
                        { "param.NoRestart", "/norestart" },
                        { "param.InstallPath", "INSTALLDIR=\"{InstallDir}\"" }
                    },
                    ShortcutName = "PDF24 Creator",
                    ShortcutRelativePath = "pdf24-creator.exe"
                },
                new InstallableApp()
                {
                    Name = "XnView MP",
                    DescriptionKey = "app.xnview.Description",
                    ExecutablePath = "XnViewMP-win-x64.exe",
                    SizeMB = 156,
                    //SizeMB = Math.Round(fileSizes["XnViewMP-win-x64.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://www.xnview.com/en/license/",
                    PathParameterKey = "/dir=",
                    CustomParameters = new Dictionary<string, string>
                    {
                        { "param.SilentInstall", "/verysilent" },
                        { "param.NoRestart", "/norestart" },
                        { "param.InstallPath", "/dir=\"{InstallDir}\"" }
                    },
                    ShortcutName = "XnView MP",
                    ShortcutRelativePath = "xnviewmp.exe"
                },
                new InstallableApp()
                {
                    Name = "FastStone Image Viewer",
                    DescriptionKey = "app.faststone.Description",
                    ExecutablePath = "FSViewerSetup81.exe",
                    SizeMB = 22,
                    //SizeMB = Math.Round(fileSizes["FSViewerSetup81.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://documentation.help/FastStone-Image-Viewer-ru/License.htm",
                    CustomParameters = new Dictionary<string, string>
                    {
                        { "param.SilentInstall", "/S" },
                        { "param.DisableSmartScreen", "/SP-" },
                        { "param.SuppressMessages", "/SUPPRESSMSGBOXES" }
                    },
                    ShortcutName = null
                },
                new InstallableApp()
                {
                    Name = "VC++ 2013 Redistributable (x86)",
                    DescriptionKey = "app.vcredist2013.x86.Description",
                    ExecutablePath = "vcredist_x86.exe",
                    SizeMB = Math.Round(fileSizes["vcredist_x86.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://support.microsoft.com/kb/40784",
                    PathParameterKey = "/install /quiet /norestart",
                    CustomParameters = [],
                    ShortcutName = null
                },
                new InstallableApp()
                {
                    Name = "VC++ 2013 Redistributable (x64)",
                    DescriptionKey = "app.vcredist2013.x64.Description",
                    ExecutablePath = "vcredist_x64.exe",
                    SizeMB = Math.Round(fileSizes["vcredist_x64.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://support.microsoft.com/kb/40784",
                    PathParameterKey = "/install /quiet /norestart",
                    CustomParameters = [],
                    ShortcutName = null
                },
                new InstallableApp()
                {
                    Name = "Marble",
                    DescriptionKey = "app.marble.Description",
                    ExecutablePath = "Marble-setup_2.2.0-1_x64.exe",
                    SizeMB = 88,
                    //SizeMB = Math.Round(fileSizes["Marble-setup_2.2.0-1_x64.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://invent.kde.org/education/marble/-/blob/master/LICENSE.txt",
                    PathParameterKey = "/DIR=",
                    CustomParameters = new Dictionary<string, string>
                    {
                        { "param.SilentInstall", "/VERYSILENT" },
                        { "param.NoRestart", "/NORESTART" }
                    },
                    ShortcutName = "Marble Globe",
                    ShortcutRelativePath = "marble-qt.exe"
                },
                new InstallableApp()
                {
                    Name = "Notepad++",
                    DescriptionKey = "app.notepadpp.Description",
                    ExecutablePath = "npp.8.8.7.Installer.x64.exe",
                    SizeMB = 17,
                    //SizeMB = Math.Round(fileSizes["npp.8.8.7.Installer.x64.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://github.com/notepad-plus-plus/notepad-plus-plus/blob/master/LICENSE",
                    PathParameterKey = "/D=",
                    CustomParameters = new Dictionary<string, string>
                    {
                        { "param.SilentInstall", "/S" },
                        { "param.DisableAutoUpdate", "/noUpdater" },
                        { "param.Language", "-Lru" }
                    },
                    ShortcutName = "Notepad++",
                    ShortcutRelativePath = "notepad++.exe"
                },
                new InstallableApp()
                {
                    Name = "Geany",
                    DescriptionKey = "app.geany.Description",
                    ExecutablePath = "geany-2.1_setup.exe",
                    SizeMB = 121,
                    //SizeMB = Math.Round(fileSizes["geany-2.1_setup.exe"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://creativecommons.org/licenses/by-sa/4.0/",
                    PathParameterKey = "/D=",
                    CustomParameters = new Dictionary<string, string>
                    {
                        { "param.SilentInstall", "/S" },
                        { "param.Language", "/LANG=Russian" }
                    },
                    ShortcutName = "Geany"
                },
                new InstallableApp()
                {
                    Name = "muCommander",
                    DescriptionKey = "app.mucommander.Description",
                    ExecutablePath = "mucommander-1.5.2.msi",
                    SizeMB = 180,
                    //SizeMB = Math.Round(fileSizes["mucommander-1.5.2.msi"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://github.com/mucommander/mucommander/blob/master/LICENSE",
                    PathParameterKey = "",
                    CustomParameters = new Dictionary<string, string>
                    {
                        { "param.SilentInstall", "/quiet" },
                        { "param.NoRestart", "/norestart" },
                        { "param.InstallPath", "INSTALLDIR=\"{InstallDir}\"" }
                    },
                    ShortcutName = null
                },
                new InstallableApp()
                {
                    Name = "Double Commander",
                    DescriptionKey = "app.doublecmd.Description",
                    ExecutablePath = "doublecmd-1.1.29.x86_64-win64.zip",
                    SizeMB = 44,
                    //SizeMB = Math.Round(fileSizes["doublecmd-1.1.29.x86_64-win64.zip"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://doublecmd.sourceforge.io/license.html",
                    IsPortable = true,
                    ShortcutName = "Double Commander",
                    ShortcutRelativePath = "doublecmd.exe"
                },
                new InstallableApp()
                {
                    Name = "LibreOffice",
                    DescriptionKey = "app.libreoffice.Description",
                    ExecutablePath = "LibreOffice_25.8.2_Win_x86-64.msi",
                    SizeMB = 1300,
                    //SizeMB = Math.Round(fileSizes["LibreOffice_25.8.2_Win_x86-64.msi"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://www.libreoffice.org/about-us/licenses/",
                    PathParameterKey = "",
                    CustomParameters = new Dictionary<string, string>
                    {
                        { "param.SilentInstall", "/qn" },
                        { "param.Components", "ADDLOCAL=ALL" },
                        { "param.InstallPath", "INSTALLLOCATION=\"{InstallDir}\"" }
                    }
                },
                new InstallableApp()
                {
                    Name = "Apache OpenOffice Portable",
                    DescriptionKey = "app.openoffice.Description",
                    ExecutablePath = "ApacheOpenOfficePortable.zip",
                    SizeMB = 1444,
                    //SizeMB = Math.Round(fileSizes["ApacheOpenOfficePortable.zip"] / (1024.0 * 1024.0), 2),
                    LicenseUrl = "https://portableapps.com/about/legal",
                    IsPortable = true,
                    ShortcutName = "OpenOffice",
                    ShortcutRelativePath = "OpenOfficeBasePortable.exe"
                }
            };
            if (Localization.Current != "ru")
            {
                foreach (var app in apps)
                {
                    // Если в CustomParameters есть param.Language — убираем его
                    app.CustomParameters.Remove("param.Language");
                }
            }

            //возвращаем изменённый список
            return apps;
        }
        public static string ExtractFromArchive(string archivePath, string fileName, string outputDir)
        {
            using var archive = ArchiveFactory.Open(archivePath);
            var entry = (archive.Entries
                .FirstOrDefault(e => e.Key != null && e.Key.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                ?? throw new FileNotFoundException($"Файл {fileName} не найден в архиве.")) ?? throw new FileNotFoundException($"Файл {fileName} не найден в архиве.");
            string outputPath = Path.Combine(outputDir, fileName);
            entry.WriteToFile(outputPath);
            return outputPath;
        }

        // Извлекает шаблонный файл из Installers.7z и возвращает полный путь к нему в temp-папке.
        public static string ExtractTemplate(string templateName, string tempDir)
        {
            string archivePath = Path.Combine(Application.StartupPath, "Installers.7z");
            using var archive = ArchiveFactory.Open(archivePath);
            var entry = archive.Entries
                .FirstOrDefault(e => e.Key.Equals(templateName, StringComparison.OrdinalIgnoreCase))
                ?? throw new FileNotFoundException($"Шаблон {templateName} не найден в архиве");

            string outPath = Path.Combine(tempDir, Path.GetFileName(templateName));
            entry.WriteToFile(outPath);
            return outPath;
        }
        //вспомогательный метод для выбора правильного имени файла в зависимости от языка
        private static string GetLocalizedExecutableName(string baseNameEn, string baseNameRu)
        {
            return Localization.Current == "ru" ? baseNameRu : baseNameEn;
        }
    }
}