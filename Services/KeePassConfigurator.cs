using Microsoft.Extensions.Logging;

namespace WindSoftInstaller.Services
{
    public class KeePassConfigurator
    {
        private readonly ILogger _logger;

        public KeePassConfigurator(ILogger logger)
        {
            _logger = logger;
        }

        public void Apply(string installPath)
        {
            try
            {
                _logger.LogInformation("Применение конфигурации KeePass");

                string sourceConfig = Path.Combine(installPath, "KeePass.config.xml");
                if (!File.Exists(sourceConfig))
                {
                    _logger.LogError("ФАЙЛ КОНФИГУРАЦИИ НЕ НАЙДЕН: {Path}", sourceConfig);
                    return;
                }

                string content = File.ReadAllText(sourceConfig);
                if (!content.Contains("Russian.lngx"))
                    _logger.LogWarning("Конфиг не содержит русский язык! {Path}", sourceConfig);

                string targetConfig = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "KeePass", "KeePass.config.xml");

                Directory.CreateDirectory(Path.GetDirectoryName(targetConfig)!);
                File.Copy(sourceConfig, targetConfig, overwrite: true);
                _logger.LogInformation("Конфиг KeePass скопирован в AppData");

                File.Delete(sourceConfig);
                _logger.LogDebug("Временный конфиг удалён из папки установки");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка применения конфига KeePass");
            }
        }
    }
}