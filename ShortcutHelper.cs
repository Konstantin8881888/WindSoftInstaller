using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace WindSoftInstaller
{
    internal static class ShortcutHelper
    {
        private static readonly Dictionary<string, string> Map = new()
        {
            ["LMMS"] = "lmms.exe",
            ["HandBrake"] = "HandBrake.exe",
            ["Clementine"] = "Clementine.exe",
            ["ClamWin"] = Path.Combine("bin", "ClamWin.exe"),  // пример вложенной папки
            ["Cryptomator"] = "Cryptomator.exe",
            ["KeePass"] = "KeePass.exe",
            ["UltraDefrag"] = "ufd.gui.exe",
            ["RivaTuner Statistics Server"] = "RTSS.exe"
        };

        public static bool TryGetExeRelativePath(string appName, out string relativePath)
            => Map.TryGetValue(appName, out relativePath!);

        public static void CreateShortcut(ILogger logger, string targetPath, string shortcutName)
        {
            logger.LogDebug("Создаём ярлык {Shortcut} → {Target}", shortcutName, targetPath);

            if (!File.Exists(targetPath))
            {
                logger.LogError("Целевой файл {Target} для ярлыка {Shortcut} не найден.", targetPath, shortcutName);
                throw new FileNotFoundException($"Целевой файл для ярлыка не найден: {targetPath}");
            }

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (string.IsNullOrEmpty(desktopPath))
            {
                logger.LogError("Не удалось получить путь к рабочему столу.");
                throw new InvalidOperationException("Не удалось получить путь к рабочему столу");
            }

            string shortcutPath = Path.Combine(desktopPath, $"{shortcutName}.lnk");

            object? shellObject = null, shortcutObject = null;
            try
            {
                var shellType = Type.GetTypeFromProgID("WScript.Shell")
                                ?? throw new InvalidOperationException("COM‑класс WScript.Shell не найден");
                shellObject = Activator.CreateInstance(shellType)
                              ?? throw new InvalidOperationException("Ошибка создания COM‑объекта WScript.Shell");

                dynamic shell = shellObject;
                shortcutObject = shell.CreateShortcut(shortcutPath)
                                 ?? throw new InvalidOperationException("Ошибка создания объекта ярлыка");

                dynamic shortcut = shortcutObject;
                shortcut.TargetPath = targetPath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath) ?? string.Empty;
                shortcut.WindowStyle = 1;
                shortcut.Save();

                logger.LogInformation("Ярлык {Shortcut} успешно создан", shortcutName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при создании ярлыка {Shortcut}", shortcutName);
                MessageBox.Show(
                    $"{Localization.T("shortcutError")} \"{shortcutName}\":\n{ex.Message}",
                    Localization.T("errorTitle"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                if (shortcutObject != null) Marshal.ReleaseComObject(shortcutObject);
                if (shellObject != null) Marshal.ReleaseComObject(shellObject);
            }
        }
    }
}