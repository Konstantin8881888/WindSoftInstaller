using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SharpCompress.Archives;
using SharpCompress.Common;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace WindSoftInstaller.Services
{
    internal class InstallationManager
    {
        private readonly ILogger _logger;

        public InstallationManager(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Устанавливает одно приложение.
        /// Все «особые» ветки (VLC, HWMonitor, RivaTuner, KeePass, OpenOffice Portable, Bitwarden) включены.
        /// </summary>
        public async Task InstallAppAsync(InstallableApp app, string installRoot, CancellationToken token)
        {
            _logger.LogDebug("Начало установки «{App}»", app.Name);

            // 1) Вычисляем tempDir и извлекаем туда инсталлятор
            string archive7z = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Installers.7z");
            if (!File.Exists(archive7z))
                throw new FileNotFoundException($"Не найден {archive7z}");

            string tempDir = Path.Combine(installRoot, "Temp", $"WSI_{Guid.NewGuid()}");
            string extractDir = Path.Combine(tempDir, "exe");    // ← изолируем здесь всё, что распакует инсталлятор
            Directory.CreateDirectory(extractDir);
            _logger.LogDebug("TempDir = {Dir}", tempDir);
            _logger.LogDebug("ExtractDir = {Dir}", extractDir);

            string sourcePath;
            using (var archive = ArchiveFactory.Open(archive7z))
            {
                var entry = archive.Entries
                                   .FirstOrDefault(e => e.Key.Equals(app.ExecutablePath, StringComparison.OrdinalIgnoreCase))
                           ?? throw new FileNotFoundException($"В архиве нет {app.ExecutablePath}");
                sourcePath = Path.Combine(extractDir, app.ExecutablePath);
                entry.WriteToFile(sourcePath);
                _logger.LogInformation("Извлечён {File} → {Dest}", app.ExecutablePath, sourcePath);
            }

            // 1.2) Извлекаем дополнительные файлы, если они есть
            if (app.AdditionalFiles != null && app.AdditionalFiles.Any())
            {
                using (var archive = ArchiveFactory.Open(archive7z))
                {
                    foreach (var additionalFile in app.AdditionalFiles)
                    {
                        var entry = archive.Entries.FirstOrDefault(e =>
                            e.Key.Equals(additionalFile, StringComparison.OrdinalIgnoreCase));

                        if (entry != null)
                        {
                            string destPath = Path.Combine(extractDir, additionalFile);
                            entry.WriteToFile(destPath);
                            _logger.LogInformation("Извлечён дополнительный файл {File} → {Dest}", additionalFile, destPath);
                        }
                        else
                        {
                            _logger.LogWarning("Дополнительный файл {File} не найден в архиве", additionalFile);
                        }
                    }
                }
            }

            token.ThrowIfCancellationRequested();

            // 2) Если портативная ветка
            if (app.IsPortable)
            {
                await HandlePortableAsync(app, installRoot, sourcePath, tempDir, token);

                // MSI Afterburner: применяем конфигурацию с русским языком
                if (app.Name.Equals("MSI Afterburner", StringComparison.OrdinalIgnoreCase))
                {
                    string targetDir = Path.Combine(installRoot, app.Name);
                    ApplyMSIAfterburnerConfiguration(targetDir, extractDir);
                }

                CleanupTemp(tempDir);
                return;
            }

            // 3) НЕ портативная: готовим installPath и специальные кейсы
            string appDir = Path.Combine(installRoot, app.Name);
            Directory.CreateDirectory(appDir);
            _logger.LogInformation("InstallDir = {Dir}", appDir);

            bool isVlc = app.Name.StartsWith("vlc", StringComparison.OrdinalIgnoreCase);
            if (isVlc)
                WriteVlcRegistry(appDir);

            // Для Opera, VC++ 2013 и LMMS: копируем установщик в папку установки
            if (app.Name.Equals("Opera", StringComparison.OrdinalIgnoreCase) ||
                app.Name.Contains("VC++ 2013", StringComparison.OrdinalIgnoreCase) ||
                app.Name.Equals("LMMS", StringComparison.OrdinalIgnoreCase))
            {
                string newSourcePath = Path.Combine(appDir, Path.GetFileName(sourcePath));
                File.Copy(sourcePath, newSourcePath, overwrite: true);
                sourcePath = newSourcePath;
                _logger.LogInformation("Установщик {App} скопирован в {Dest}", app.Name, newSourcePath);
            }

            var builder = new InstallerArgumentsBuilder(app, sourcePath, appDir);
            var psi = builder.BuildStartInfo();
            _logger.LogDebug("Запуск: {File} {Args}", psi.FileName, psi.Arguments);

            using (var proc = Process.Start(psi) ?? throw new InvalidOperationException("Process.Start() вернул null"))
            {
                await proc.WaitForExitAsync(token);
                _logger.LogDebug("{App} ExitCode={Code}", app.Name, proc.ExitCode);

                // HWMonitor: после окончания установки глушим его авто‑запуск
                if (app.Name.Equals("HWMonitor", StringComparison.OrdinalIgnoreCase))
                    KillProcesses("HWiNFO64");

                // RivaTuner: копируем шаблон
                if (app.Name.Equals("RivaTuner Statistics Server", StringComparison.OrdinalIgnoreCase)
                    && proc.ExitCode == 0)
                    CopyRivaTunerConfig(appDir, tempDir);

                // удаляем MSI, если там остался
                DeleteInstallerIfLeft(sourcePath, appDir);

                // ярлыки
                CreateDesktopShortcutIfExists(app.ShortcutRelativePath, app.ShortcutName ?? app.Name, appDir);

                // общие словарные ярлыки
                if (ShortcutHelper.TryGetExeRelativePath(app.Name, out var rel))
                    CreateDesktopShortcutIfExists(rel, app.ShortcutName ?? app.Name, appDir);
            }



            //копируем конфиг
            using (var archive = ArchiveFactory.Open(archive7z))
            {
                var entry = archive.Entries
                                   .FirstOrDefault(e => e.Key.Equals(app.ExecutablePath, StringComparison.OrdinalIgnoreCase))
                               ?? throw new FileNotFoundException($"В архиве нет {app.ExecutablePath}");
                sourcePath = Path.Combine(extractDir, app.ExecutablePath);
                entry.WriteToFile(sourcePath);
                _logger.LogInformation("Извлечён {File} → {Dest}", app.ExecutablePath, sourcePath);

                // ДОБАВЛЯЕМ: Извлечение дополнительных файлов для KeePass
                if (app.Name.Equals("KeePass", StringComparison.OrdinalIgnoreCase))
                {
                    // Извлекаем KeePass.config.xml
                    var configEntry = archive.Entries
                                             .FirstOrDefault(e => e.Key.Equals("KeePass.config.xml", StringComparison.OrdinalIgnoreCase));
                    if (configEntry != null)
                    {
                        string configPath = Path.Combine(extractDir, "KeePass.config.xml");
                        configEntry.WriteToFile(configPath);
                        _logger.LogInformation("Извлечён конфиг KeePass → {Dest}", configPath);
                    }

                    // Извлекаем Russian.lngx
                    var lngxEntry = archive.Entries
                                           .FirstOrDefault(e => e.Key.Equals("Russian.lngx", StringComparison.OrdinalIgnoreCase));
                    if (lngxEntry != null)
                    {
                        string lngxPath = Path.Combine(extractDir, "Russian.lngx");
                        lngxEntry.WriteToFile(lngxPath);
                        _logger.LogInformation("Извлечён файл русского языка → {Dest}", lngxPath);
                    }
                }
            }

            // KeePass: копируем конфиг
            if (app.Name.Equals("KeePass", StringComparison.OrdinalIgnoreCase))
                ApplyKeePassConfiguration(appDir, extractDir);

            // XMedia Recode: создаем структуру папок и копируем конфиги (только для русского языка)
            if (app.Name.Equals("XMedia Recode", StringComparison.OrdinalIgnoreCase))
                ApplyXMediaRecodeConfiguration(extractDir);

            // После завершения установки проблемных приложений
            if (app.Name.Equals("Opera", StringComparison.OrdinalIgnoreCase) ||
                app.Name.Contains("VC++ 2013", StringComparison.OrdinalIgnoreCase) ||
                app.Name.Equals("LMMS", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    // Пытаемся удалить установщик из папки назначения
                    if (File.Exists(sourcePath))
                    {
                        File.Delete(sourcePath);
                        _logger.LogInformation("Установщик {App} удалён: {Path}", app.Name, sourcePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось удалить установщик {App}, будет выполнена отложенная очистка", app.Name);
                    // Запланируем удаление при следующем запуске
                    ScheduleDeferredCleanup(sourcePath);
                }
            }

            CleanupTemp(tempDir);
        }

        private void ScheduleDeferredCleanup(string filePath)
        {
            try
            {
                string cleanupBat = Path.Combine(Path.GetTempPath(), $"cleanup_{Guid.NewGuid()}.bat");
                string batchContent = $@"
@echo off
chcp 65001 >nul
echo Запланированная очистка заблокированных файлов...
timeout /t 10 /nobreak >nul

:: Попытка удалить файл
if exist ""{filePath}"" (
    echo Удаляем файл: {filePath}
    del /f /q ""{filePath}""
)

:: Попытка удалить папку, если это временная директория
for /d %%i in (""{Path.GetDirectoryName(filePath)}"") do (
    if ""%%~ni"" geq ""WSI_"" (
        echo Удаляем временную директорию: %%~fi
        rmdir /s /q ""%%~fi"" 2>nul
    )
)

:: Удаляем сам bat-файл
del /f /q ""%~f0""

echo Очистка завершена.
";

                File.WriteAllText(cleanupBat, batchContent, Encoding.UTF8);

                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c start /min \"\" \"{cleanupBat}\"",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false
                });

                _logger.LogInformation("Запланирована отложенная очистка для {Path}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось запланировать отложенную очистку");
            }
        }

        private void ApplyMSIAfterburnerConfiguration(string installPath, string extractDir)
        {
            try
            {
                if (Localization.Current != "ru")
                {
                    _logger.LogInformation("Пропускаем применение конфигурации для MSI Afterburner, т.к. текущий язык: {Lang}", Localization.Current);
                    return;
                }

                _logger.LogInformation("Применение конфигурации MSI Afterburner");

                string sourceConfig = Path.Combine(extractDir, "MSIAfterburner.cfg");
                if (!File.Exists(sourceConfig))
                {
                    _logger.LogError("ФАЙЛ КОНФИГУРАЦИИ MSI AFTERBURNER НЕ НАЙДЕН: {Path}", sourceConfig);
                    return;
                }

                // Формируем целевой путь в папке установленного приложения
                string targetConfigDir = Path.Combine(installPath, "Profiles");
                string targetConfigPath = Path.Combine(targetConfigDir, "MSIAfterburner.cfg");

                // Создаем целевую директорию, если её нет
                Directory.CreateDirectory(targetConfigDir);
                _logger.LogInformation("Создана директория: {Dir}", targetConfigDir);

                // Копируем файл конфигурации
                File.Copy(sourceConfig, targetConfigPath, overwrite: true);
                _logger.LogInformation("Конфиг MSI Afterburner скопирован в \"{Path}\"", targetConfigPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка применения конфига MSI Afterburner");
            }
        }

        private void ApplyXMediaRecodeConfiguration(string extractDir)
        {
            try
            {
                if (Localization.Current != "ru")
                {
                    _logger.LogInformation("Пропускаем применение русской конфигурации для XMedia Recode, т.к. текущий язык: {Lang}", Localization.Current);
                    return;
                }

                _logger.LogInformation("Создание структуры папок и конфигурации XMedia Recode");

                // Формируем целевой путь
                string targetDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "XMedia Recode");

                // Создаем основную директорию
                Directory.CreateDirectory(targetDir);
                _logger.LogInformation("Создана директория: {Dir}", targetDir);

                // Создаем подпапку xmr.log
                string logDir = Path.Combine(targetDir, "xmr.log");
                Directory.CreateDirectory(logDir);
                _logger.LogInformation("Создана поддиректория: {Dir}", logDir);

                // Копируем файлы конфигурации
                CopyConfigFile(extractDir, targetDir, "XMediaRecode.json");
                CopyConfigFile(extractDir, targetDir, "Fav.ini");

                _logger.LogInformation("Структура папок и файлов для XMedia Recode успешно создана");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка создания структуры папок для XMedia Recode");
            }
        }

        private void CopyConfigFile(string extractDir, string targetDir, string fileName)
        {
            try
            {
                string sourceFile = Path.Combine(extractDir, fileName);
                string targetFile = Path.Combine(targetDir, fileName);

                if (!File.Exists(sourceFile))
                {
                    _logger.LogError("Файл {File} не найден в временной директории: {Path}", fileName, sourceFile);
                    return;
                }

                File.Copy(sourceFile, targetFile, overwrite: true);
                _logger.LogInformation("Файл {File} скопирован в \"{Path}\"", fileName, targetFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка копирования файла {File}", fileName);
            }
        }

        private async Task HandlePortableAsync(InstallableApp app, string installRoot, string sourcePath, string tempDir, CancellationToken token)
        {
            string targetDir = Path.Combine(installRoot, app.Name);
            Directory.CreateDirectory(targetDir);

            string ext = Path.GetExtension(sourcePath).ToLowerInvariant();
            if (ext is ".zip" or ".7z")
            {
                _logger.LogDebug("Распаковываем {Zip}", sourcePath);
                using var arc = ArchiveFactory.Open(sourcePath);
                foreach (var entry in arc.Entries.Where(e => !e.IsDirectory))
                {
                    string outPath = Path.Combine(targetDir, entry.Key);
                    Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
                    entry.WriteToFile(outPath,
                        new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
                }
            }
            else
            {
                // простой exe‑portable
                File.Copy(sourcePath, Path.Combine(targetDir, Path.GetFileName(sourcePath)), overwrite: true);
            }

            // OpenOffice Portable — создаём ярлыки всех модулей
            if (app.Name.Equals("Apache OpenOffice Portable", StringComparison.OrdinalIgnoreCase))
            {
                var exes = Directory.GetFiles(targetDir, "OpenOffice*Portable.exe", SearchOption.TopDirectoryOnly);
                foreach (var exe in exes)
                {
                    var name = Path.GetFileNameWithoutExtension(exe)
                                   .Replace("OpenOffice", "")
                                   .Replace("Portable", "")
                                   .Trim();
                    if (string.IsNullOrEmpty(name)) name = "Main";
                    CreateDesktopShortcut(exe, $"{app.ShortcutName} {name}");
                }

                return;
            }

            // Bitwarden special
            if (app.Name.Equals("Bitwarden", StringComparison.OrdinalIgnoreCase))
            {
                var exe = Path.Combine(targetDir, Path.GetFileName(sourcePath));
                CreateDesktopShortcut(exe, app.ShortcutName!);
                return;
            }

            // общая portable‑логика
            string candidate = app.ShortcutRelativePath != null
                ? Directory.GetFiles(targetDir, app.ShortcutRelativePath, SearchOption.AllDirectories).FirstOrDefault()
                : Directory.GetFiles(targetDir, "*.exe", SearchOption.AllDirectories).FirstOrDefault();

            if (!string.IsNullOrEmpty(candidate))
                CreateDesktopShortcut(candidate, app.ShortcutName!);
        }

        private void WriteVlcRegistry(string appDir)
        {
            try
            {
                using var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\VideoLAN\VLC");
                key.SetValue("InstallDir", appDir, RegistryValueKind.String);
                _logger.LogInformation("VLC registry InstallDir");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось записать VLC InstallDir");
            }
        }

        private void KillProcesses(string name)
        {
            foreach (var p in Process.GetProcessesByName(name))
            {
                try
                {
                    _logger.LogDebug("Убиваем процесс {Name} (PID={Pid})", name, p.Id);
                    p.Kill(entireProcessTree: true);
                }
                catch { }
            }
        }

        private void CopyRivaTunerConfig(string appDir, string tempDir)
        {
            var cfg = AppRepository.ExtractTemplate("Config", tempDir);
            var profiles = Path.Combine(appDir, "Profiles");
            Directory.CreateDirectory(profiles);
            File.Copy(cfg, Path.Combine(profiles, "Config"), overwrite: true);
            _logger.LogInformation("RivaTuner config deployed");
        }

        private void DeleteInstallerIfLeft(string sourcePath, string appDir)
        {
            var left = Path.Combine(appDir, Path.GetFileName(sourcePath));
            if (File.Exists(left))
            {
                try
                {
                    File.Delete(left);
                    _logger.LogDebug("Installer {File} deleted", left);
                }
                catch { }
            }
        }

        private void CreateDesktopShortcutIfExists(string? relPath, string name, string appDir)
        {
            if (string.IsNullOrWhiteSpace(relPath)) return;
            var exe = Path.Combine(appDir, relPath);
            if (File.Exists(exe))
                CreateDesktopShortcut(exe, name);
        }

        private void CreateDesktopShortcut(string exePath, string name)
        {
            ShortcutHelper.CreateShortcut(_logger, exePath, name);
        }

        private void ApplyKeePassConfiguration(string installPath, string extractDir)
        {
            try
            {
                _logger.LogInformation("Применение конфигурации KeePass");

                if (Localization.Current != "ru")
                {
                    _logger.LogInformation(
                        "KeePassConfigurator: пропускаем копирование русского конфига, т.к. Current = {Lang}",
                        Localization.Current);
                    return;
                }

                // Пути к файлам во временной директории (extractDir)
                string sourceConfig = Path.Combine(extractDir, "KeePass.config.xml");
                string sourceLngx = Path.Combine(extractDir, "Russian.lngx");

                // Проверяем наличие файлов во временной директории
                if (!File.Exists(sourceConfig))
                {
                    _logger.LogError("ФАЙЛ КОНФИГУРАЦИИ НЕ НАЙДЕН: {Path}", sourceConfig);
                    return;
                }

                // Целевая директория — куда установлен KeePass
                string targetConfig = Path.Combine(installPath, "KeePass.config.xml");

                // Создаем папку Languages, если её нет
                string languagesDir = Path.Combine(installPath, "Languages");
                Directory.CreateDirectory(languagesDir);

                // Копируем файл языка в папку Languages
                string targetLngx = Path.Combine(languagesDir, "Russian.lngx");

                // 1. Копируем файл русского языка, если он есть
                if (File.Exists(sourceLngx))
                {
                    File.Copy(sourceLngx, targetLngx, overwrite: true);
                    _logger.LogInformation("Файл русского языка скопирован в \"{Path}\"", targetLngx);
                }
                else
                {
                    _logger.LogWarning("Файл русского языка не найден: {Path}", sourceLngx);
                }

                // 2. Копируем конфигурационный файл
                File.Copy(sourceConfig, targetConfig, overwrite: true);
                _logger.LogInformation("Конфиг KeePass скопирован в \"{Path}\"", targetConfig);

                // 3. Проверяем конфиг на наличие ссылки на русский язык
                string content = File.ReadAllText(targetConfig);
                if (!content.Contains("Russian.lngx"))
                {
                    _logger.LogWarning("Конфиг не содержит ссылку на русский язык! {Path}", targetConfig);

                    // Если нужно, можно автоматически обновить конфиг
                    // Но это уже дополнительная функциональность
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка применения конфига KeePass");
            }
        }

        private void CleanupTemp(string tempDir)
        {
            try
            {
                // удаляем свою WSI_{GUID} папку
                Directory.Delete(tempDir, recursive: true);
                _logger.LogDebug("Удалён временный каталог {Dir}", tempDir);

                // пробуем удалить общий Temp, если он пуст
                var tempRoot = Path.GetDirectoryName(tempDir); // это ...\installRoot\Temp
                if (tempRoot is not null
                    && Directory.Exists(tempRoot)
                    && !Directory.EnumerateFileSystemEntries(tempRoot).Any())
                {
                    Directory.Delete(tempRoot);
                    _logger.LogDebug("Удалена пустая директория Temp {Dir}", tempRoot);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Temp cleanup failed for {Dir}", tempDir);
            }
        }
    }
}
