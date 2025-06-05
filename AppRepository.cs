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
                        fileSizes[entry.Key] = entry.Size;
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
            Name = "mplayerc",
            Description = "Быстрый видеоплеер с минималистичным интерфейсом",
            ExecutablePath = "mplayerc.exe",
            SizeMB = Math.Round(fileSizes["mplayerc.exe"] / (1024.0 * 1024.0), 2),
            LicenseUrl = "https://mpc-hc.org/licenses/",
            IsPortable = true,
            ShortcutName = "MPC"
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
    }
}