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
        /// Все «особые» ветки (VLC, HWiNFO64, RivaTuner, KeePass, OpenOffice Portable, Bitwarden) включены.
        /// </summary>
        public async Task InstallAppAsync(InstallableApp app, string installRoot, CancellationToken token)
        {
            _logger.LogDebug("Начало установки «{App}»", app.Name);

            // 1) Вычисляем tempDir и извлекаем туда инсталлятор
            string archive7z = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Installers.7z");
            if (!File.Exists(archive7z))
                throw new FileNotFoundException($"Не найден {archive7z}");

            string tempDir = Path.Combine(installRoot, "Temp", $"WSI_{Guid.NewGuid()}");
            string extractDir = Path.Combine(tempDir, "exe");
            Directory.CreateDirectory(extractDir);
            _logger.LogDebug("TempDir = {Dir}", tempDir);
            _logger.LogDebug("ExtractDir = {Dir}", extractDir);

            string sourcePath;
            using (var archive = ArchiveFactory.Open(archive7z))
            {
                // Используем локализованный путь к архиву для портативных приложений
                string fileToExtract = app.IsPortable ? app.GetLocalizedArchivePath() : app.ExecutablePath;

                var entry = archive.Entries
                                   .FirstOrDefault(e => e.Key?.Equals(fileToExtract, StringComparison.OrdinalIgnoreCase) == true)
                           ?? throw new FileNotFoundException($"В архиве нет {fileToExtract}");

                sourcePath = Path.Combine(extractDir, fileToExtract);
                entry.WriteToFile(sourcePath);
                _logger.LogInformation("Извлечён {File} → {Dest}", fileToExtract, sourcePath);
            }

            // 1.2) Извлекаем дополнительные файлы, если они есть
            if (app.AdditionalFiles != null && app.AdditionalFiles.Any())
            {
                using (var archive = ArchiveFactory.Open(archive7z))
                {
                    foreach (var additionalFile in app.AdditionalFiles)
                    {
                        var entry = archive.Entries.FirstOrDefault(e =>
                            e.Key?.Equals(additionalFile, StringComparison.OrdinalIgnoreCase) == true);

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

            // Для VC++ 2015-2022 и других проблемных установщиков: копируем установщик в папку установки
            bool shouldCopyInstaller =
                app.Name.Equals("Opera", StringComparison.OrdinalIgnoreCase) ||
                app.Name.Contains("VC++ 2013", StringComparison.OrdinalIgnoreCase) ||
                app.Name.Contains("VC++ 2015-2022", StringComparison.OrdinalIgnoreCase) ||
                app.Name.Equals("LMMS", StringComparison.OrdinalIgnoreCase);

            if (shouldCopyInstaller)
            {
                string newSourcePath = Path.Combine(appDir, Path.GetFileName(sourcePath));

                // Добавляем задержку и повторные попытки для VC++ 2015-2022
                if (app.Name.Contains("VC++ 2015-2022", StringComparison.OrdinalIgnoreCase))
                {
                    await CopyFileWithRetryAsync(sourcePath, newSourcePath, app.Name);
                }
                else
                {
                    File.Copy(sourcePath, newSourcePath, overwrite: true);
                }

                sourcePath = newSourcePath;
                _logger.LogInformation("Установщик {App} скопирован в {Dest}", app.Name, newSourcePath);
            }

            var builder = new InstallerArgumentsBuilder(app, sourcePath, appDir);
            var psi = builder.BuildStartInfo();
            _logger.LogDebug("Запуск: {File} {Args}", psi.FileName, psi.Arguments);

            // Для VC++ 2015-2022 добавляем дополнительную обработку
            if (app.Name.Contains("VC++ 2015-2022", StringComparison.OrdinalIgnoreCase))
            {
                await InstallVcRedistWithRetryAsync(psi, app.Name, token);
            }
            else
            {
                using (var proc = Process.Start(psi) ?? throw new InvalidOperationException("Process.Start() вернул null"))
                {
                    await proc.WaitForExitAsync(token);
                    _logger.LogDebug("{App} ExitCode={Code}", app.Name, proc.ExitCode);
                }
            }

            // HWiNFO64: после окончания установки глушим его авто‑запуск
            if (app.Name.Equals("HWiNFO64", StringComparison.OrdinalIgnoreCase))
                KillProcesses("HWiNFO64");

            // RivaTuner: копируем шаблон
            if (app.Name.Equals("RivaTuner Statistics Server", StringComparison.OrdinalIgnoreCase))
                CopyRivaTunerConfig(appDir, tempDir);

            // удаляем MSI, если там остался
            DeleteInstallerIfLeft(sourcePath, appDir);

            // ярлыки
            CreateDesktopShortcutIfExists(app.ShortcutRelativePath, app.ShortcutName ?? app.Name, appDir);

            // общие словарные ярлыки
            if (ShortcutHelper.TryGetExeRelativePath(app.Name, out var rel))
                CreateDesktopShortcutIfExists(rel, app.ShortcutName ?? app.Name, appDir);

            // KeePass: копируем конфиг
            if (app.Name.Equals("KeePass", StringComparison.OrdinalIgnoreCase))
                ApplyKeePassConfiguration(appDir, extractDir);

            // XMedia Recode: создаем структуру папок и копируем конфиги (только для русского языка)
            if (app.Name.Equals("XMedia Recode", StringComparison.OrdinalIgnoreCase))
                ApplyXMediaRecodeConfiguration(extractDir);

            // После завершения установки проблемных приложений
            if (shouldCopyInstaller)
            {
                try
                {
                    // Для VC++ 2015-2022 добавляем задержку перед удалением
                    if (app.Name.Contains("VC++ 2015-2022", StringComparison.OrdinalIgnoreCase))
                    {
                        await Task.Delay(2000, token); // Задержка 2 секунды
                    }

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

            // Удаляем пустые папки для приложений, которые устанавливаются в системные расположения
            await DeleteEmptyFolderForSystemAppsAsync(app, appDir);

            CleanupTemp(tempDir);
        }

        private async Task DeleteEmptyFolderForSystemAppsAsync(InstallableApp app, string appDir)
        {
            // Список приложений, которые устанавливаются в системные расположения
            var systemApps = new[]
            {
            "Google Chrome",
            "Microsoft VC++ 2015-2022 Redistributable (x64)",
            "Microsoft VC++ 2015-2022 Redistributable (x86)",
            "VC++ 2013 Redistributable (x64)",
            "VC++ 2013 Redistributable (x86)"
        };

            // Проверяем, является ли приложение системным (устанавливается в стандартное расположение)
            bool isSystemApp = systemApps.Contains(app.Name, StringComparer.OrdinalIgnoreCase) ||
                              app.Name.Contains("VC++", StringComparison.OrdinalIgnoreCase);

            if (isSystemApp && !string.IsNullOrEmpty(appDir))
            {
                await DeleteEmptyFolderAsync(appDir, app.Name);
            }
        }

        private async Task DeleteEmptyFolderAsync(string folderPath, string appName)
        {
            try
            {
                // Даем время системе завершить все операции с папкой
                await Task.Delay(1500);

                if (Directory.Exists(folderPath))
                {
                    // Проверяем, пуста ли папка
                    var files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
                    var directories = Directory.GetDirectories(folderPath, "*", SearchOption.AllDirectories);

                    // Если папка пуста (нет файлов и подпапок)
                    if (files.Length == 0 && directories.Length == 0)
                    {
                        Directory.Delete(folderPath, recursive: true);
                        _logger.LogInformation("Пустая папка {App} удалена: {Path}", appName, folderPath);
                        return;
                    }

                    _logger.LogDebug("Папка {App} не пуста: {Path} (файлов: {Files}, папок: {Dirs})",
                        appName, folderPath, files.Length, directories.Length);

                    // Проверяем, можно ли удалить папку с временными файлами
                    await TryCleanTemporaryFolderAsync(folderPath, appName, files);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось удалить папку {App}: {Path}", appName, folderPath);

                // Планируем отложенное удаление
                ScheduleDeferredFolderCleanup(folderPath);
            }
        }

        private async Task TryCleanTemporaryFolderAsync(string folderPath, string appName, string[] files)
        {
            try
            {
                bool allFilesAreSmall = true;
                long totalSize = 0;
                var smallFileExtensions = new[] { ".tmp", ".log", ".txt", ".bak" };

                // Проверяем размер и тип файлов
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    totalSize += fileInfo.Length;

                    var extension = Path.GetExtension(file).ToLowerInvariant();

                    // Если файл больше 2 МБ ИЛИ не является временным файлом, считаем что это не временный файл
                    if (fileInfo.Length > 2 * 1024 * 1024 || !smallFileExtensions.Contains(extension))
                    {
                        allFilesAreSmall = false;
                        _logger.LogDebug("Файл {File} не считается временным (размер: {Size}, расширение: {Ext})",
                            file, fileInfo.Length, extension);
                        break;
                    }
                }

                // Если все файлы маленькие, временные и общий размер меньше 10 МБ, удаляем их
                if (allFilesAreSmall && totalSize < 10 * 1024 * 1024)
                {
                    _logger.LogInformation("Удаляем временные файлы {App} (файлов: {Count}, размер: {Size} байт)",
                        appName, files.Length, totalSize);

                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                            _logger.LogDebug("Удален временный файл: {File}", file);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug("Не удалось удалить файл {File}: {Error}", file, ex.Message);
                        }
                    }

                    // Даем время системе
                    await Task.Delay(1000);

                    // Проверяем, остались ли файлы
                    var remainingFiles = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
                    var remainingDirs = Directory.GetDirectories(folderPath, "*", SearchOption.AllDirectories);

                    if (remainingFiles.Length == 0 && remainingDirs.Length == 0)
                    {
                        Directory.Delete(folderPath, recursive: true);
                        _logger.LogInformation("Папка {App} очищена от временных файлов и удалена: {Path}", appName, folderPath);
                    }
                    else
                    {
                        _logger.LogWarning("После очистки в папке {App} остались файлы: {Files}, папки: {Dirs}",
                            appName, remainingFiles.Length, remainingDirs.Length);
                    }
                }
                else
                {
                    _logger.LogInformation("Папка {App} содержит нетemporary файлы, оставляем: {Path}", appName, folderPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось очистить папку {App}: {Path}", appName, folderPath);
            }
        }

        private async Task DeleteEmptyChromeFolderAsync(string appDir, string appName)
        {
            try
            {
                // Даем время системе завершить все операции с папкой
                await Task.Delay(1000);

                if (Directory.Exists(appDir))
                {
                    // Проверяем, пуста ли папка
                    var files = Directory.GetFiles(appDir, "*", SearchOption.AllDirectories);
                    var directories = Directory.GetDirectories(appDir, "*", SearchOption.AllDirectories);

                    // Если папка пуста (нет файлов и подпапок) или содержит только временные/служебные файлы
                    if (files.Length == 0 && directories.Length == 0)
                    {
                        Directory.Delete(appDir, recursive: true);
                        _logger.LogInformation("Пустая папка {App} удалена: {Path}", appName, appDir);
                    }
                    else
                    {
                        _logger.LogDebug("Папка {App} не пуста, оставляем: {Path} (файлов: {Files}, папок: {Dirs})",
                            appName, appDir, files.Length, directories.Length);

                        // Если есть файлы, но их мало и они маленькие - возможно, это временные файлы
                        // Можно попробовать удалить их и затем папку
                        await TryCleanChromeFolderAsync(appDir, appName, files);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось удалить папку {App}: {Path}", appName, appDir);

                // Планируем отложенное удаление
                ScheduleDeferredFolderCleanup(appDir);
            }
        }

        private async Task TryCleanChromeFolderAsync(string folderPath, string appName, string[] files)
        {
            try
            {
                bool allFilesAreSmall = true;
                long totalSize = 0;

                // Проверяем размер файлов
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    totalSize += fileInfo.Length;

                    // Если файл больше 1 МБ, считаем что это не временный файл
                    if (fileInfo.Length > 1024 * 1024)
                    {
                        allFilesAreSmall = false;
                        break;
                    }
                }

                // Если все файлы маленькие и общий размер меньше 5 МБ, удаляем их
                if (allFilesAreSmall && totalSize < 5 * 1024 * 1024)
                {
                    _logger.LogInformation("Удаляем временные файлы {App} (размер: {Size} байт)", appName, totalSize);

                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug("Не удалось удалить файл {File}: {Error}", file, ex.Message);
                        }
                    }

                    // Даем время системе
                    await Task.Delay(500);

                    // Проверяем, остались ли файлы
                    var remainingFiles = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
                    var remainingDirs = Directory.GetDirectories(folderPath, "*", SearchOption.AllDirectories);

                    if (remainingFiles.Length == 0 && remainingDirs.Length == 0)
                    {
                        Directory.Delete(folderPath, recursive: true);
                        _logger.LogInformation("Папка {App} очищена и удалена: {Path}", appName, folderPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось очистить папку {App}: {Path}", appName, folderPath);
            }
        }

        private void ScheduleDeferredFolderCleanup(string folderPath)
        {
            try
            {
                string cleanupBat = Path.Combine(Path.GetTempPath(), $"cleanup_folder_{Guid.NewGuid()}.bat");
                string batchContent = $@"
@echo off
chcp 65001 >nul
echo Запланированное удаление папки...
echo Папка: {folderPath}
timeout /t 5 /nobreak >nul

:: Несколько попыток удалить папку с увеличением задержки
set retry_count=0
:retry_folder
if exist ""{folderPath}"" (
    set /a retry_count+=1
    echo Попытка %retry_count% удалить папку: {folderPath}
    rmdir /s /q ""{folderPath}"" 2>nul
    if exist ""{folderPath}"" (
        echo Папка все еще заблокирована, повтор через 3 секунды...
        timeout /t 3 /nobreak >nul
        if %retry_count% lss 5 (
            goto retry_folder
        ) else (
            echo Не удалось удалить папку после 5 попыток.
        )
    ) else (
        echo Папка успешно удалена.
    )
) else (
    echo Папка уже удалена.
)

:: Удаляем сам bat-файл
del /f /q ""%~f0"" 2>nul

echo Очистка папки завершена.
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

                _logger.LogInformation("Запланировано отложенное удаление папки: {Path}", folderPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось запланировать отложенное удаление папки");
            }
        }

        public async Task FinalCleanupAsync(string installRoot)
        {
            try
            {
                _logger.LogInformation("Начинаем финальную очистку пустых папок в: {Path}", installRoot);

                if (!Directory.Exists(installRoot))
                    return;

                // Список папок, которые могут быть пустыми
                var potentialEmptyFolders = new[]
                {
                "Google Chrome",
                "Microsoft VC++ 2015-2022 Redistributable (x64)",
                "Microsoft VC++ 2015-2022 Redistributable (x86)",
                "VC++ 2013 Redistributable (x64)",
                "VC++ 2013 Redistributable (x86)"
            };

                foreach (var folderName in potentialEmptyFolders)
                {
                    string folderPath = Path.Combine(installRoot, folderName);
                    if (Directory.Exists(folderPath))
                    {
                        await DeleteEmptyFolderAsync(folderPath, folderName);
                    }
                }

                _logger.LogInformation("Финальная очистка завершена");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при финальной очистке");
            }
        }

        private async Task CopyFileWithRetryAsync(string sourcePath, string destPath, string appName, int maxRetries = 3)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    File.Copy(sourcePath, destPath, overwrite: true);
                    _logger.LogInformation("Файл {App} успешно скопирован с попытки {Attempt}", appName, attempt);
                    return;
                }
                catch (IOException ex) when (attempt < maxRetries)
                {
                    _logger.LogWarning("Попытка {Attempt} копирования {App} не удалась: {Error}", attempt, appName, ex.Message);
                    await Task.Delay(1000 * attempt); // Увеличивающаяся задержка
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Критическая ошибка при копировании {App}", appName);
                    throw;
                }
            }

            throw new IOException($"Не удалось скопировать файл {appName} после {maxRetries} попыток");
        }

        private async Task InstallVcRedistWithRetryAsync(ProcessStartInfo psi, string appName, CancellationToken token, int maxRetries = 3)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using (var proc = Process.Start(psi) ?? throw new InvalidOperationException("Process.Start() вернул null"))
                    {
                        await proc.WaitForExitAsync(token);
                        _logger.LogDebug("{App} ExitCode={Code} (попытка {Attempt})", appName, proc.ExitCode, attempt);

                        if (proc.ExitCode == 0 || proc.ExitCode == 1638 || proc.ExitCode == 3010)
                        {
                            // 0 - успех, 1638 - уже установлена более новая версия, 3010 - требуется перезагрузка
                            _logger.LogInformation("{App} установлен успешно (код: {Code})", appName, proc.ExitCode);
                            return;
                        }
                        else if (attempt < maxRetries)
                        {
                            _logger.LogWarning("{App} завершился с кодом {Code}, повторная попытка {Attempt}",
                                appName, proc.ExitCode, attempt + 1);
                            await Task.Delay(2000 * attempt, token); // Увеличивающаяся задержка
                        }
                        else
                        {
                            _logger.LogError("{App} завершился с ошибкой (код: {Code}) после {Attempt} попыток",
                                appName, proc.ExitCode, maxRetries);
                            throw new Exception($"Установка {appName} завершилась с кодом ошибки: {proc.ExitCode}");
                        }
                    }
                }
                catch (IOException ex) when (attempt < maxRetries)
                {
                    _logger.LogWarning("Ошибка ввода-вывода при установке {App} (попытка {Attempt}): {Error}",
                        appName, attempt, ex.Message);
                    await Task.Delay(1000 * attempt, token);
                }
            }
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
timeout /t 5 /nobreak >nul

:: Попытка удалить файл (несколько попыток с задержкой)
:retry_delete
if exist ""{filePath}"" (
    echo Попытка удалить файл: {filePath}
    del /f /q ""{filePath}"" 2>nul
    if exist ""{filePath}"" (
        echo Файл все еще заблокирован, повтор через 2 секунды...
        timeout /t 2 /nobreak >nul
        goto retry_delete
    )
    echo Файл успешно удален.
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

        private Task HandlePortableAsync(InstallableApp app, string installRoot, string sourcePath, string tempDir, CancellationToken token)
        {
            string targetDir = Path.Combine(installRoot, app.Name);
            Directory.CreateDirectory(targetDir);
            token.ThrowIfCancellationRequested();

            // Для MSI Afterburner используем локализованный архив
            string archiveToExtract = sourcePath;
            if (app.Name.Equals("MSI Afterburner", StringComparison.OrdinalIgnoreCase))
            {
                string localizedArchivePath = app.GetLocalizedArchivePath();
                if (!string.IsNullOrEmpty(localizedArchivePath))
                {
                    // Получаем путь к локализованному архиву
                    string archivesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Installers.7z");
                    using (var archive = ArchiveFactory.Open(archivesPath))
                    {
                        var localizedEntry = archive.Entries
                            .FirstOrDefault(e => e.Key?.Equals(localizedArchivePath, StringComparison.OrdinalIgnoreCase) == true);
                
                        if (localizedEntry != null)
                        {
                            string localizedSourcePath = Path.Combine(tempDir, localizedArchivePath);
                            localizedEntry.WriteToFile(localizedSourcePath);
                            archiveToExtract = localizedSourcePath;
                            _logger.LogInformation("Используется локализованный архив для MSI Afterburner: {Archive}", localizedArchivePath);
                        }
                        else
                        {
                            _logger.LogWarning("Локализованный архив {Archive} не найден, используется стандартный", localizedArchivePath);
                        }
                    }
                }
            }

            string ext = Path.GetExtension(archiveToExtract).ToLowerInvariant();
            if (ext is ".zip" or ".7z")
            {
                _logger.LogDebug("Распаковываем {Zip}", archiveToExtract);
                using var arc = ArchiveFactory.Open(archiveToExtract);
                foreach (var entry in arc.Entries.Where(e => !e.IsDirectory))
                {
                    token.ThrowIfCancellationRequested();
                    string outPath = Path.Combine(targetDir, entry.Key!);
                    Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
                    entry.WriteToFile(outPath,
                        new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
                }
            }
            else
            {
                // простой exe‑portable
                File.Copy(archiveToExtract, Path.Combine(targetDir, Path.GetFileName(archiveToExtract)), overwrite: true);
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

                return Task.CompletedTask;
            }

            // Bitwarden special
            if (app.Name.Equals("Bitwarden", StringComparison.OrdinalIgnoreCase))
            {
                var exe = Path.Combine(targetDir, Path.GetFileName(sourcePath));
                CreateDesktopShortcut(exe, app.ShortcutName!);
                return Task.CompletedTask;
            }

            // общая portable‑логика
            string? candidate = app.ShortcutRelativePath != null
                ? Directory.GetFiles(targetDir, app.ShortcutRelativePath, SearchOption.AllDirectories).FirstOrDefault()
                : Directory.GetFiles(targetDir, "*.exe", SearchOption.AllDirectories).FirstOrDefault();

            if (!string.IsNullOrEmpty(candidate))
                CreateDesktopShortcut(candidate, app.ShortcutName!);

            return Task.CompletedTask;
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
