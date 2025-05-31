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

            return
            [
                new() 
                {
                    Name = "vlc-3.0.21",
                    Description = "Мощный видеопроигрыватель с поддержкой большинства кодеков",
                    ExecutablePath = "vlc-3.0.21.exe",
                    CustomParameters =
                    {
                        { "Режим установки", "/S" },
                        { "Язык", "/DE=ru" }
                    }
                },
                new() 
                {
                    Name = "mplayerc",
                    Description = "Быстрый видеоплеер с минималистичным интерфейсом",
                    ExecutablePath = "mplayerc.exe",
                    IsPortable = true,
                    ShortcutName = "MPC"
                },
                new() 
                {
                    Name = "GIMP",
                    Description = "GIMP — растровый графический редактор. Аналог Adobe Photoshop для обработки фото, создания цифрового искусства и дизайна.",
                    ExecutablePath = "gimp-3.0.4-setup.exe",
                    CustomParameters =
                    {
                        { "Режим установки", "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-" },
                        { "Язык", "/LANG=russian" }
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