using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace WindSoftInstaller.Services
{
    public class ShortcutService
    {
        private readonly Microsoft.Extensions.Logging.ILogger _logger;

        public ShortcutService(Microsoft.Extensions.Logging.ILogger logger)
        {
            _logger = logger;
        }

        public void Create(string targetPath, string shortcutName)
        {
            _logger.LogDebug("Создаём ярлык {Shortcut} → {Target}", shortcutName, targetPath);

            if (!File.Exists(targetPath))
            {
                _logger.LogError("Целевой файл {Target} для ярлыка {Shortcut} не найден.", targetPath, shortcutName);
                throw new FileNotFoundException($"Целевой файл для ярлыка не найден: {targetPath}");
            }

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (string.IsNullOrEmpty(desktopPath))
            {
                _logger.LogError("Не удалось получить путь к рабочему столу.");
                throw new InvalidOperationException("Не удалось получить путь к рабочему столу");
            }

            string shortcutPath = Path.Combine(desktopPath, $"{shortcutName}.lnk");
            object shellObject = null, shortcutObject = null;

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
                shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath)!;
                shortcut.WindowStyle = 1;
                shortcut.Save();

                _logger.LogInformation("Ярлык {Shortcut} успешно создан", shortcutName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании ярлыка {Shortcut}", shortcutName);
                MessageBox.Show(
                    $"Ошибка при создании ярлыка \"{shortcutName}\":\n{ex.Message}",
                    "Ошибка",
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