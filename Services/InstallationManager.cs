using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SharpCompress.Archives;
using SharpCompress.Common;
using Microsoft.Win32;

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
            Directory.CreateDirectory(tempDir);
            _logger.LogDebug("TempDir = {Dir}", tempDir);

            string sourcePath;
            using (var archive = ArchiveFactory.Open(archive7z))
            {
                var entry = archive.Entries
                                   .FirstOrDefault(e => e.Key.Equals(app.ExecutablePath, StringComparison.OrdinalIgnoreCase))
                           ?? throw new FileNotFoundException($"В архиве нет {app.ExecutablePath}");
                sourcePath = Path.Combine(tempDir, app.ExecutablePath);
                entry.WriteToFile(sourcePath);
                _logger.LogInformation("Извлечён {File} → {Dest}", app.ExecutablePath, sourcePath);
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

            // KeePass: копируем конфиг
            if (app.Name.Equals("KeePass", StringComparison.OrdinalIgnoreCase))
                ApplyKeePassConfiguration(appDir);

            CleanupTemp(tempDir);
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

        private void ApplyKeePassConfiguration(string installPath)
        {
            // скопирован из Form1… без правок
            // …
        }

        private void CleanupTemp(string tempDir)
        {
            try { Directory.Delete(tempDir, recursive: true); }
            catch (Exception ex) { _logger.LogWarning(ex, "Temp cleanup failed"); }
        }
    }
}
