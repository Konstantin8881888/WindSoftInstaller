using System.Runtime.Versioning;
using SharpCompress.Archives;

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

            return
            [
                new()
        {
            Name = "vlc-3.0.21",
            Description = "Мощный видеопроигрыватель с поддержкой большинства кодеков",
            ExecutablePath = "vlc-3.0.21.exe",
            SizeMB = Math.Round(fileSizes["vlc-3.0.21.exe"] / (1024.0 * 1024.0), 2),
            LicenseUrl = "https://www.videolan.org/legal.html",
            PathParameterKey = "/D=",
            CustomParameters =
            {
                { "Режим установки", "/S" },
                { "Язык", "/L=ru" }
            }
        },
        new()
        {
            Name = "MPC-HC",
            Description = "Media Player Classic Home Cinema — лёгкий медиа-плеер",
            // Укажите точное имя оффлайн-инсталлятора из вашего архива Installers.7z
            ExecutablePath = "MPC-HC.1.7.13.x64.exe",
            SizeMB = Math.Round(fileSizes["MPC-HC.1.7.13.x64.exe"] / (1024.0 * 1024.0), 2),
            LicenseUrl = "https://github.com/mpc-hc/mpc-hc/blob/develop/COPYING.txt",
            PathParameterKey = "/DIR=",
            CustomParameters =
            {
                { "Тихая установка", "/VERYSILENT" },
                { "Не перезагружать", "/NORESTART" },
                { "Язык", "/LANG=Russian" }
            }
        },
        new()
        {
            Name = "SMPlayer",
            Description = "SMPlayer — кроссплатформенный медиаплеер. Программа представляет собой графическую оболочку для MPlayer.",
            ExecutablePath = "smplayer-24.5.0-x64-unsigned.exe",
            SizeMB = Math.Round(fileSizes["smplayer-24.5.0-x64-unsigned.exe"] / (1024.0 * 1024.0), 2),
            LicenseUrl = "https://github.com/smplayer-dev/smplayer?tab=GPL-2.0-1-ov-file#readme",
            PathParameterKey = "/D=",
            CustomParameters =
            {
                { "Тихая установка", "/S" }
            }
        },
        new()
        {
            Name = "GIMP",
            Description = "GIMP — растровый графический редактор. Аналог Adobe Photoshop для обработки фото, создания цифрового искусства и дизайна.",
            ExecutablePath = "gimp-3.0.4-setup.exe",
            SizeMB = Math.Round(fileSizes["gimp-3.0.4-setup.exe"] / (1024.0 * 1024.0), 2),
            LicenseUrl = "https://www.gimp.org/about/COPYING",
            PathParameterKey = "/DIR=",
            CustomParameters =
            {
                { "Режим установки", "/VERYSILENT" },
                { "Без окон", "/SUPPRESSMSGBOXES" },
                { "Без перезагрузки", "/NORESTART" },
                { "Откл SmartScreen", "/SP-" },
                { "Язык", "/LANG=russian" }
            }
        },
        new()
        {
            Name = "Paint.NET",
            Description = "Мощный редактор изображений с поддержкой слоёв и плагинов",
            ExecutablePath = "paint.net.5.1.8.install.x64.exe",
            SizeMB = Math.Round(fileSizes["paint.net.5.1.8.install.x64.exe"] / (1024.0 * 1024.0), 2),
            LicenseUrl = "https://www.getpaint.net/license.html",
            PathParameterKey = "TARGETDIR=",
            CustomParameters =
            {
                { "Режим установки: тихий", "/auto" },
                { "Пропустить конфигурации", "/skipConfig" },
                { "Язык", "/language=ru" }
            }
        },
        new()
        {
            Name = "Shotcut",
            Description = "Мощный видеоредактор с открытым исходным кодом и поддержкой всех форматов",
            ExecutablePath = "shotcut-win64-250511.exe",
            SizeMB = Math.Round(fileSizes["shotcut-win64-250511.exe"] / (1024.0 * 1024.0), 2),
            LicenseUrl = "https://github.com/mltframework/shotcut/blob/master/COPYING",
            PathParameterKey = "/DIR=",
            CustomParameters =
            {
                { "Тихая установка", "/VERYSILENT" },
                { "Без перезагрузки", "/NORESTART" },
                { "Для текущего пользователя", "/CURRENTUSER" },
                { "Ярлык на столе", "/MERGETASKS=desktopicon" }
            }
        },
        new()
        {
            Name = "VSDC Free Video Editor",
            Description = "Бесплатный нелинейный видеоредактор VSDC Free Video Editor",
            ExecutablePath = "video_editor_x64.exe",
            SizeMB = Math.Round(fileSizes["video_editor_x64.exe"] / (1024.0 * 1024.0), 2),
            LicenseUrl = "https://www.videosoftdev.com/terms-and-conditions",
            PathParameterKey = "/DIR=",
            CustomParameters =
            {
                { "Тихая установка", "/VERYSILENT" },
                { "Без перезагрузки", "/NORESTART" },
            }
        },
        new()
        {
            Name = "Google Chrome",
            Description = "Браузер Google Chrome",
            ExecutablePath = "googlechromestandaloneenterprise64.msi",
            SizeMB = Math.Round(fileSizes["googlechromestandaloneenterprise64.msi"] / (1024.0 * 1024.0), 2),
            LicenseUrl = "https://www.google.com/chrome/terms/",
            PathParameterKey = "INSTALLDIR=",
            CustomParameters =
            {
                { "Тихая установка", "/qn" }
            }
        },
        //new()
        //{
        //    Name = "Mozilla Firefox",
        //    Description = "Браузер Mozilla Firefox",
        //    ExecutablePath = "Firefox Setup 139.0.1.exe",
        //    SizeMB = Math.Round(fileSizes["Firefox Setup 139.0.1.exe"] / (1024.0 * 1024.0), 2),
        //    LicenseUrl = "https://www.mozilla.org/en-US/legal/",
        //    PathParameterKey = "/InstallDir=",
        //    CustomParameters =
        //    {
        //        { "Тихая установка", "/S" },
        //        // Отключение установки как браузера по умолчанию
        //        { "Не устанавливать по умолчанию", "/NoMakeDefaultBrowser" },
        //        // Установка в указанную директорию
        //        { "Путь установки", "/InstallDir=" },
        //        // Отключение проверки браузера по умолчанию
        //        { "Отключить проверку по умолчанию", "/NoDefaultBrowserCheck" }
        //    }
        //},
        new()
        {
            Name = "Opera",
            Description = "Браузер Opera",
            ExecutablePath = "Opera_119.0.5497.70_Setup_x64.exe",
            SizeMB = Math.Round(fileSizes["Opera_119.0.5497.70_Setup_x64.exe"] / (1024.0 * 1024.0), 2),
            LicenseUrl = "https://www.opera.com/legal",
            PathParameterKey = "--installfolder=",
            CustomParameters =
            {
                { "Тихая установка", "--silent" },
                // Установка только для текущего пользователя; если нужно для всех, поставить allusers=1
                { "Только текущий пользователь", "--allusers=0" },
                { "Язык установки", "--language=ru" },
                // Отключаем автозапуск после установки
                { "Не запускать после установки", "--launchopera=0" },
                // Ярлык на рабочем столе (по умолчанию Opera его создаёт, но продублировать не вредно)
                { "Создать ярлык", "--desktopshortcut=1" },
                // Отключаем автообновления (если нужно)
                //{ "Отключить автообновления", "--no-update" }
            }
        },
        new()
        {
            Name = "Audacity",
            Description = "Бесплатный аудиоредактор для записи и редактирования звука",
            ExecutablePath = "audacity-win-3.7.3-64bit.exe",
            SizeMB = Math.Round(fileSizes["audacity-win-3.7.3-64bit.exe"] / (1024.0 * 1024.0), 2),
            LicenseUrl = "https://www.audacityteam.org/about/license/",
            PathParameterKey = "/DIR=",
            CustomParameters =
            {
                { "Скрытая установка", "/VERYSILENT" },
                { "Не перезагружать", "/NORESTART" },
                // Отключение ассоциации с файлами
                { "Отключить ассоциацию", "/ASSOCIATE=0" }
            }
        },
        new()
        {
            Name = "LMMS",
            Description = "LMMS — цифровая аудио рабочая станция (NSIS-инсталлятор)",
            ExecutablePath = "lmms-1.2.2-win64.exe",
            SizeMB = Math.Round(fileSizes["lmms-1.2.2-win64.exe"] / (1024.0 * 1024.0), 2),
            LicenseUrl = "https://github.com/LMMS/lmms/blob/master/LICENSE.txt",
            PathParameterKey = "/D=",
            CustomParameters =
            {
                { "Тихая установка", "/S" }
            }
        },
        new()
        {
            Name = "HandBrake",
            Description = "HandBrake — бесплатный видеотранскодер (GPL)",
            ExecutablePath = "HandBrake-1.9.2-x86_64-Win_GUI.exe",
            SizeMB = Math.Round(fileSizes["HandBrake-1.9.2-x86_64-Win_GUI.exe"]/(1024.0*1024.0),2),
            LicenseUrl = "https://github.com/HandBrake/HandBrake/blob/master/COPYING",
            PathParameterKey = "/D=",
            CustomParameters =
            {
                { "Тихая установка", "/S" }
            }
        },
        new()
        {
            Name = "XMedia Recode",
            Description = "XMedia Recode — универсальный аудио/видео конвертер",
            ExecutablePath = "XMediaRecode3612_x64_setup.exe",
            SizeMB = Math.Round(fileSizes["XMediaRecode3612_x64_setup.exe"]/(1024.0*1024.0),2),
            LicenseUrl = "https://www.xmedia-recode.de/en/",
            PathParameterKey = "/DIR=",
            CustomParameters =
            {
                { "Тихая установка", "/VERYSILENT" },
                { "Не перезагружать", "/NORESTART" },
                { "Язык", "/LANG=Russian" },
                { "Ярлык на столе", "/MERGETASKS=desktopicon" }
            }
        },
        new()
        {
            Name = "OpenShot",
            Description = "OpenShot Video Editor — бесплатный видеоредактор и конвертер",
            ExecutablePath = "OpenShot-v3.3.0-x86_64.exe",
            SizeMB = Math.Round(fileSizes["OpenShot-v3.3.0-x86_64.exe"]/(1024.0*1024.0),2),
            LicenseUrl = "https://www.gnu.org/licenses/gpl-3.0.html",
            PathParameterKey = "/DIR=",
            CustomParameters =
            {
                { "Тихая установка", "/VERYSILENT" },
                { "Не перезагружать", "/NORESTART" },
                { "Ярлык на столе", "/MERGETASKS=desktopicon" }
            }
        },
        new()
        {
            Name = "AIMP",
            Description = "AIMP — бесплатный аудиоплеер",
            ExecutablePath = "aimp_5.40.2675_w64.exe",
            SizeMB = Math.Round(fileSizes["aimp_5.40.2675_w64.exe"]/(1024.0*1024.0),2),
            LicenseUrl = "https://www.aimp.ru/?do=eula&os=windows",
            PathParameterKey = "/AUTO=",
            CustomParameters =
            {
                { "Тихая установка", "/SILENT" },
            }
        },
        new()
        {
            Name = "Clementine",
            Description = "Современный музыкальный проигрыватель и библиотека для организации коллекции",
            ExecutablePath = "ClementineSetup-1.4.1-18-g4ab6f35ec.exe",
            SizeMB = Math.Round(fileSizes["ClementineSetup-1.4.1-18-g4ab6f35ec.exe"] / (1024.0 * 1024.0), 2),
            LicenseUrl = "https://www.gnu.org/licenses/gpl-3.0.html",
            PathParameterKey = "/D=",
            CustomParameters =
            {
                 { "Режим установки", "/S" }
            }
        },
        new()
        {
            Name = "7-Zip",
            Description = "Мощный архиватор с высокой степенью сжатия и поддержкой множества форматов",
            ExecutablePath = "7z2409-x64.exe",
            SizeMB = Math.Round(fileSizes["7z2409-x64.exe"] / (1024.0 * 1024.0), 2),
            LicenseUrl = "https://www.7-zip.org/license.txt",
            PathParameterKey = "/D=",
            CustomParameters =
            {
                { "Режим установки", "/S" },
                { "Контекстное меню", "/NoShell=0" }
            }
        },
        new()
        {
            Name = "qBittorrent",
            Description = "Мощный клиент для торрент-загрузок с открытым исходным кодом",
            ExecutablePath = "qbittorrent_5.1.0_x64_setup.exe", // Уточните версию!
            SizeMB = Math.Round(fileSizes["qbittorrent_5.1.0_x64_setup.exe"] / (1024.0 * 1024.0), 2),
            LicenseUrl = "https://www.gnu.org/licenses/gpl-2.0.html",
            PathParameterKey = "/D=",
            CustomParameters =
            {
                { "Режим установки", "/S" },
                { "Задачи", "/NOADDTOSTART" },
                { "Ассоциации", "/ASSOCIATE" }
            }
        },
        new()
        {
            Name = "HWMonitor",
            Description = "Мониторинг температуры, напряжения и скорости вентиляторов компонентов ПК",
            ExecutablePath = "hwi64_826.exe",
            SizeMB = Math.Round(fileSizes["hwi64_826.exe"] / (1024.0 * 1024.0), 2),
            LicenseUrl = "https://www.cpuid.com/softwares/hwmonitor.html",
            PathParameterKey = "/DIR=",
            CustomParameters =
            {
                { "VerySilent",    "/VERYSILENT" },
                { "SuppressMsgs",  "/SUPPRESSMSGBOXES" },
                { "NoRestart",     "/NORESTART" }
            }
        },
        new()
        {
            Name = "LibreOffice",
            Description = "LibreOffice — офисный пакет с поддержкой PDF‑редактора (Draw)",
            ExecutablePath = "LibreOffice_25.2.4_Win_x86-64.msi",
            SizeMB = Math.Round(fileSizes["LibreOffice_25.2.4_Win_x86-64.msi"] / (1024.0 * 1024.0), 2),
            LicenseUrl = "https://www.libreoffice.org/about-us/licenses/",
            // PathParameterKey не нужен для MSI‑разветвления ???
            PathParameterKey = "", // MSI не использует /D=
                    CustomParameters =
            {
                { "Quiet",      "/qn" },
                { "Components", "ADDLOCAL=ALL" },
                { "InstallDir", "INSTALLLOCATION=\"{InstallDir}\"" }
            }
        },
        new()
        {
            Name = "PDF‑XChange Editor",
            Description = "PDF‑редактор с аннотациями и редактированием текста",
            ExecutablePath = "PDF-XChangex64.msi",
            SizeMB = Math.Round(fileSizes["PDF-XChangex64.msi"] / (1024.0 * 1024.0), 2),
            LicenseUrl = "https://www.tracker-software.com/PDFXLicense.pdf",
            PathParameterKey = "", // для MSI путь будет добавлен в CustomParameters
            CustomParameters =
            {
                { "Тихая установка", "/quiet" },
                { "Без перезагрузки", "/norestart" },
                { "Язык", "EDITOR_LANGUAGE=ru-RU" },
                { "Путь установки", "INSTALLLOCATION=\"{InstallDir}\"" }
            }
        },
        new InstallableApp()
        {
            Name = "ClamWin",
            Description = "Антивирус с открытым исходным кодом.",
            ExecutablePath = "clamwin-0.103.2.1-setup.exe",
            SizeMB = Math.Round(fileSizes["clamwin-0.103.2.1-setup.exe"]/(1024.0*1024.0),2),
            LicenseUrl = "https://github.com/clamwin/clamwin/blob/master/COPYING",
            PathParameterKey = "/DIR=",
            CustomParameters =
            {
                { "Режим установки", "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART" },
                { "Язык", "/LANG=Russian" },
                { "Компоненты", "/COMPONENTS=\"main,context_menu,russian\"" },
                { "Отключить SmartScreen", "/SP-" }
            },
            ShortcutName = "ClamWin"
        },
        new InstallableApp()
        {
            Name = "Emsisoft Emergency Kit",
            Description = "Портативный антивирусный сканер для экстренной проверки системы",
            ExecutablePath = "EmsisoftEmergencyKit.zip",  // теперь ZIP
            SizeMB = Math.Round(fileSizes["EmsisoftEmergencyKit.zip"] / (1024.0 * 1024.0), 2),
            LicenseUrl = "https://www.emsisoft.com/en/eula/?utm_source=chatgpt.com",
            IsPortable = true,
            ShortcutName = "Emsisoft Emergency Kit",
            ShortcutRelativePath = "Start Scanner.exe"
            // CustomParameters и PathParameterKey не нужны — универсальная распаковка ZIP сработает
        },
        new InstallableApp()
        {
            Name = "Cryptomator",
            Description = "Клиентское шифрование папок и облаков (FUSE‑тома). Автоматически шифрует файлы перед загрузкой в Dropbox/Google Drive.",
            ExecutablePath = "Cryptomator-1.16.0-x64.msi",
            SizeMB = Math.Round(fileSizes["Cryptomator-1.16.0-x64.msi"] / (1024.0 * 1024.0), 2),
            LicenseUrl = "https://github.com/cryptomator/cryptomator/blob/master/LICENSE",
            PathParameterKey = "",  // для MSI не используем отдельный ключ
            CustomParameters =
            {
                { "Тихая установка", "/quiet" },
                { "Без перезагрузки", "/norestart" },
                { "Путь установки", "INSTALLDIR=\"{InstallDir}\"" }
            },
            ShortcutName = "Cryptomator"
        },
        new InstallableApp()
        {
            Name = "KeePass",
            Description = "Бесплатный менеджер паролей с открытым исходным кодом для безопасного хранения учетных данных. Локальное шифрование (AES-256) и поддержка плагинов.",
            ExecutablePath = "KeePass-2.58-Setup.exe",
            SizeMB = Math.Round(fileSizes["KeePass-2.58-Setup.exe"] / (1024.0 * 1024.0), 2),
            LicenseUrl = "https://keepass.info/help/v2/license.html",
            PathParameterKey = "/DIR=",
            CustomParameters =
            {
                { "Режим установки", "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART" },
                { "Язык", "/LANG=English" } // Временно английский, русский установим позже
            },
            ShortcutName = "KeePass",
            AdditionalFiles = ["Russian.lngx", "KeePass.config.xml"],
            AdditionalFilesDestinations =
            {
                {"Russian.lngx", "Languages\\Russian.lngx" },
                { "KeePass.config.xml", "KeePass.config.xml" }  // Копируем в корень
            },
            LanguageConfig = "Russian" // Указываем язык для конфигурации
        },
        new InstallableApp()
        {
            Name = "Bitwarden",
            Description = "Менеджер паролей с открытым кодом, шифрованием AES-256 и синхронизацией. Генерирует сложные пароли, поддерживает 2FA.",
            ExecutablePath = "Bitwarden-Portable-2025.5.1.exe",
            SizeMB = Math.Round(fileSizes["Bitwarden-Portable-2025.5.1.exe"] / (1024.0 * 1024.0), 2),
            LicenseUrl = "https://github.com/bitwarden/clients/blob/main/LICENSE_BITWARDEN.txt",
            IsPortable = true,
            ShortcutName = "Bitwarden",
            CustomParameters =
            {
                { "Режим распаковки", "/VERYSILENT" }
            }
        },
        new InstallableApp()
        {
            Name = "Wise Disk Cleaner",
            Description = "Программа для оптимизации дискового пространства с глубоким анализом мусорных файлов. Удаляет временные данные, кэш приложений и дубликаты, ускоряя работу системы.",
            ExecutablePath = "WDCFree_11.2.3.843.exe",
            SizeMB = Math.Round(fileSizes["WDCFree_11.2.3.843.exe"]/(1024.0*1024.0),2),
            LicenseUrl = "https://www.wisecleaner.com/eula.html",
            PathParameterKey = "/DIR=",
            CustomParameters =
            {
                { "Тихая установка", "/VERYSILENT" }
            },
            ShortcutName = "Wise Disk Cleaner"
        },
        new InstallableApp()
        {
            Name = "BleachBit",
            Description = "Инструмент для удаления кэша, истории и временных файлов с перезаписью для защиты конфиденциальности.",
            ExecutablePath = "BleachBit-5.0.0-setup.exe",
            SizeMB = Math.Round(fileSizes["BleachBit-5.0.0-setup.exe"] / (1024.0*1024.0), 2),
            LicenseUrl = "https://github.com/bleachbit/bleachbit/blob/master/COPYING",
            PathParameterKey = "/D=",
            CustomParameters =
            {
                { "Тихая установка", "/S" },
                { "Для всех пользователей", "/allusers" }
            },
            ShortcutName = "BleachBit"
        },
        new InstallableApp()
        {
            Name = "UltraDefrag",
            Description = "Профессиональный дефрагментатор для Windows. Оптимизирует системные файлы, включая загрузочные области, и поддерживает работу из командной строки.",
            ExecutablePath = "ultradefrag-7.1.4.bin.amd64.exe",
            SizeMB = Math.Round(fileSizes["ultradefrag-7.1.4.bin.amd64.exe"] / (1024.0 * 1024.0), 2),
            LicenseUrl = "https://ultradefrag.net/en/license",
            PathParameterKey = "/D=",
            CustomParameters =
            {
                { "Тихая установка", "/S" }
            },
            // Ярлык создадим вручную после установки
            ShortcutName = "UltraDefrag",
            ShortcutRelativePath = "ufd.gui.exe"
        },
        new InstallableApp()
        {
            Name = "RetroArch",
            Description = "Универсальная платформа для запуска эмуляторов, игр и мультимедиа с поддержкой ретро-консолей. Объединяет различные движки в единый интерфейс с расширенными настройками графики, шейдеров и управления. Подходит для создания унифицированной игровой среды с кросс-платформенной синхронизацией.",
            ExecutablePath = "RetroArchPortable.zip",
            SizeMB = Math.Round(fileSizes["RetroArchPortable.zip"]/(1024.0*1024.0), 2),
            LicenseUrl = "https://www.gnu.org/licenses/gpl-3.0.html",
            IsPortable = true,
            ShortcutRelativePath = "retroarch.exe",
            ShortcutName = "RetroArch"
        },
        new InstallableApp()
        {
            Name = "PCSX2",
            Description = "Эмулятор PlayStation 2 с поддержкой HD-разрешения, улучшенной графикой и сохранениями состояний. Позволяет запускать игры с физических дисков или образов, включает настройки для оптимизации производительности.",
            ExecutablePath = "pcsx2-v2.2.0-windows-x64-Qt.7z",
            SizeMB = Math.Round(fileSizes["pcsx2-v2.2.0-windows-x64-Qt.7z"]/(1024.0*1024.0),2),
            LicenseUrl = "https://github.com/PCSX2/pcsx2/blob/master/pcsx2/Docs/License.txt",
            IsPortable = true,
            ShortcutRelativePath = "pcsx2-qt.exe",
            ShortcutName = "PCSX2"
        },
        new InstallableApp()
        {
            Name = "Microsoft VC++ 2015-2019 Redistributable (x64)",
            Description = "Необходимые SxS-сборки для приложений на C++ (Afterburner, др.)",
            SizeMB = Math.Round(fileSizes["vc_redist.x64.exe"]/(1024.0*1024.0),2),
            LicenseUrl = "https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist",
            ExecutablePath = "vc_redist.x64.exe",
            PathParameterKey = "",
            CustomParameters = new Dictionary<string,string>
            {
                { "Тихая установка", "/install /quiet /norestart" }
            },
            ShortcutName    = null
        },
        new InstallableApp()
        {
          Name = "Microsoft VC++ 2015-2019 Redistributable (x86)",
          Description = "SxS-библиотеки x86 для приложений на C++",
          ExecutablePath = "vc_redist.x86.exe",
          SizeMB = Math.Round(fileSizes["vc_redist.x86.exe"]/(1024.0*1024.0),2),
          LicenseUrl  = "https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist",
          PathParameterKey = "",
          CustomParameters = new Dictionary<string,string>
          {
            { "Тихая установка", "/quiet /norestart" }
          },
          ShortcutName    = null
        },
        new InstallableApp()
        {
            Name                 = "MSI Afterburner",
            Description          = "Портативный мониторинг и разгон видеокарт (без RTSS)",
            ExecutablePath       = "MSIAfterburnerPortable.zip",
            SizeMB               = Math.Round(fileSizes["MSIAfterburnerPortable.zip"]/(1024.0*1024.0), 2),
            LicenseUrl           = "https://www.msi.com/page/eula",
            IsPortable           = true,
            ShortcutRelativePath = "MSIAfterburner.exe",
            ShortcutName         = "MSI Afterburner"
        },
        new InstallableApp()
        {
          Name = "RivaTuner Statistics Server",
          Description = "Сервер статистики для MSI Afterburner (независимый)",
          ExecutablePath = "RTSSSetup736.exe",
          SizeMB = Math.Round(fileSizes["RTSSSetup736.exe"]/(1024.0*1024.0),2),
          LicenseUrl = "https://www.msi.com/page/eula",
          PathParameterKey = "/D=",
          CustomParameters = new Dictionary<string,string>
          {
                { "Тихая установка", "/S" },
                //{ "Language", "/LANG=1049" }// Похоже не работает. Исправлено заменой конф-файла
          },
          ShortcutName = "RTSS",
          ShortcutRelativePath = "RTSS.exe"
        },
        new InstallableApp()
        {
            Name            = "Anki",
            Description     = "Обучение иностранным языкам. Система интервальных повторений для карточек (SRS)",
            ExecutablePath  = "anki-25.02.7-windows-qt6.exe",            // адаптируйте под вашу версию
            SizeMB          = Math.Round(fileSizes["anki-25.02.7-windows-qt6.exe"]/(1024.0*1024.0),2),
            LicenseUrl      = "https://github.com/ankitects/anki/blob/main/LICENSE",
            PathParameterKey= "/D=",
            CustomParameters= new Dictionary<string,string>
            {
                { "Тихая установка", "/S" }
            },
            ShortcutName        = "Anki",
            ShortcutRelativePath= "anki.exe"
        },
        new InstallableApp()
        {
            Name                  = "Kiwix Desktop",
            Description           = "Офлайн-читалка для Wikipedia и Wikibooks через ZIM-архивы",
            ExecutablePath        = "kiwix-desktop_windows_x64_2.4.1.zip",
            SizeMB                = Math.Round(fileSizes["kiwix-desktop_windows_x64_2.4.1.zip"] / (1024.0*1024.0), 2),
            LicenseUrl            = "https://github.com/kiwix/kiwix-desktop/blob/master/LICENSE",
            IsPortable            = true,
            ShortcutName          = "Kiwix Desktop",
            ShortcutRelativePath  = "kiwix.exe"
        },
        new InstallableApp()
        {
            Name            = "Calibre",
            Description     = "Менеджер электронных книг и конвертер форматов",
            ExecutablePath  = "calibre-64bit-8.5.0.msi",
            SizeMB          = Math.Round(fileSizes["calibre-64bit-8.5.0.msi"]/(1024.0*1024.0),2),
            LicenseUrl      = "https://calibre-ebook.com/license",
            PathParameterKey= "",
            CustomParameters= new Dictionary<string,string>
            {
                { "Quiet",      "/quiet" },
                { "NoRestart",  "/norestart" },
                { "Папка установки", "TARGETDIR=\"{InstallDir}\"" }
            },
            ShortcutName        = "Calibre",
            ShortcutRelativePath= "calibre.exe"
        },
        new InstallableApp()
        {
            Name            = "Zotero",
            Description     = "Менеджер библиографии и PDF-анализатор для исследователей",
            ExecutablePath  = "Zotero-7.0.16_x64_setup.exe",
            SizeMB          = Math.Round(fileSizes["Zotero-7.0.16_x64_setup.exe"]/(1024.0*1024.0),2),
            LicenseUrl      = "https://www.zotero.org/license/",
            PathParameterKey= "/D=",
            CustomParameters= new Dictionary<string,string>
            {
                { "Silent", "/S" }
            },
            ShortcutName        = "Zotero",
            ShortcutRelativePath= "zotero.exe"
        },

    ];
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
    }
}