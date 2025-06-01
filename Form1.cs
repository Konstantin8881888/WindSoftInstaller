using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SharpCompress.Archives;
using File = System.IO.File;

namespace WindSoftInstaller
{
    [SupportedOSPlatform("windows")]
    public partial class Form1 : Form
    {
        private readonly List<InstallableApp> apps = AppRepository.LoadApps();
        private bool allSelected = false;
        int dotCount = 0;
        private CancellationTokenSource? _cts; // Делаем nullable
        private readonly ILogger<Form1> _logger;
        // поле для сбора имен приложений, у которых не удалось записать в реестр путь
        private readonly List<string> _registryFailedApps = new();

        public Form1(ILogger<Form1> logger)
        {
            InitializeComponent();
            // Проверка на null
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            CleanupOldTemp(Properties.Settings.Default.LastInstallPath, _logger);

            // Загружаем иконку из ресурсов
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream("WindSoftInstaller.Resources.logo.ico");
                if (stream != null)
                {
                    this.Icon = new Icon(stream);
                    this.ShowIcon = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки иконки");
                // Fallback на иконку по умолчанию
                this.Icon = SystemIcons.Application;
            }

            // Используем MemoryStream для конвертации byte[] в Icon
            byte[] iconBytes = Properties.Resources.logo;
            using (var stream = new System.IO.MemoryStream(iconBytes))
            {
                // (проверка на null):
                if (stream != null)
                {
                    using var icon = new Icon(stream);
                    this.Icon = (Icon)icon.Clone(); // Явное приведение типа
                }
            }
            // Восстанавливаем последний путь, если он задан
            var saved = Properties.Settings.Default.LastInstallPath;
            if (!string.IsNullOrWhiteSpace(saved) && Directory.Exists(saved))
            {
                txtInstallPath.Text = saved;
            }
            // Задаём фон формы и единый шрифт
            this.BackColor = Color.LightSteelBlue;
            this.Font = new Font("Segoe UI", 9F);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _logger.LogInformation("Form1 загружена, начинаем извлечение иконок для {Count} приложений", apps.Count);
            // Для каждого app извлекаем иконку из .exe и сохраняем в свойство Icon
            foreach (var app in apps)
            {
                string fullPath = Path.Combine(Application.StartupPath, app.ExecutablePath);
                if (File.Exists(fullPath))
                {
                    try
                    {
                        var sysIcon = Icon.ExtractAssociatedIcon(fullPath);
                        // Явная проверка
                        if (sysIcon != null)
                        {
                            app.Icon = sysIcon.ToBitmap();
                        }
                        _logger.LogDebug("Иконка для {App} успешно извлечена", app.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Не удалось извлечь иконку для {App}", app.Name);
                    }
                }
                else
                {
                    _logger.LogWarning("Файл для иконки не найден: {Path}", fullPath);
                }
            }

            _logger.LogInformation("Иконки извлечены, инициализируем DataGridView");
            // Загружаем данные
            dataGridViewPrograms.AutoGenerateColumns = false;
            var bindingList = new BindingList<InstallableApp>(apps);
            dataGridViewPrograms.DataSource = bindingList;  // привязываем список объектов
            _logger.LogInformation("DataGridView инициализирована");

        }

        private void CreateShortcut(string targetPath, string shortcutName)
        {
            _logger.LogDebug("Создаём ярлык {Shortcut} → {Target}", shortcutName, targetPath);

            if (!File.Exists(targetPath))
            {
                _logger.LogError("Целевой файл {Target} для ярлыка {Shortcut} не найден.", targetPath, shortcutName);
                throw new FileNotFoundException($"Целевой файл для ярлыка не найден: {targetPath}");
            }

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string shortcutPath = Path.Combine(desktopPath, $"{shortcutName}.lnk");

            object? shellObject = null;
            object? shortcutObject = null;

            try
            {
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell") ?? throw new InvalidOperationException("COM-класс WScript.Shell не найден");
                shellObject = Activator.CreateInstance(shellType);
                if (shellObject == null)
                {
                    throw new InvalidOperationException("Ошибка создания COM-объекта");
                }

                dynamic shell = shellObject;
                shortcutObject = shell.CreateShortcut(shortcutPath);

                if (shortcutObject == null)
                {
                    throw new InvalidOperationException("Ошибка создания ярлыка");
                }

                dynamic shortcut = shortcutObject;

                shortcut.TargetPath = targetPath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
                shortcut.WindowStyle = 1;
                // shortcut.IconLocation = targetPath;
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
                // корректное освобождение COM-объектов
                if (shortcutObject != null)
                    Marshal.ReleaseComObject(shortcutObject);

                if (shellObject != null)
                    Marshal.ReleaseComObject(shellObject);
            }
        }

        private void OnAboutClick(object sender, EventArgs e)
        {
            using var about = new AboutForm();
            about.ShowDialog(this);
        }
        private void BtnBrowse_Click(object sender, EventArgs e)
        {

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                txtInstallPath.Text = folderBrowserDialog.SelectedPath;
                // Сохраняем в пользовательских настройках
                Properties.Settings.Default.LastInstallPath = txtInstallPath.Text;
                Properties.Settings.Default.Save();
                _logger.LogInformation("Пользователь выбрал путь установки: {Path}", txtInstallPath.Text);
            }
        }

        private async Task InstallAppAsync(InstallableApp app, string installPath, CancellationToken token)
        {
            _logger.LogDebug("Начало InstallAppAsync для {App}", app.Name);
            // Проверим права Админа:
            bool isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            _logger.LogInformation("Запущено с правами администратора: {IsAdmin}", isAdmin);
            string? tempDir = null;

            try
            {
                // 1. Проверка архива
                string archivePath = Path.Combine(Application.StartupPath, "Installers.7z");
                if (!File.Exists(archivePath))
                    throw new FileNotFoundException($"Архив не найден: {archivePath}");

                // 2. Создаём временную папку для распаковки
                tempDir = Path.Combine(installPath, "Temp", $"WSI_{Guid.NewGuid()}");
                Directory.CreateDirectory(Path.GetDirectoryName(tempDir)!);
                Directory.CreateDirectory(tempDir);
                _logger.LogDebug("Создана временная папка: {TempDir}", tempDir);

                // 3. Извлечение нужного EXE из архива
                string sourcePath;
                using (var archive = ArchiveFactory.Open(archivePath))
                {
                    var entry = archive.Entries
                        .FirstOrDefault(e => e.Key.Equals(app.ExecutablePath, StringComparison.OrdinalIgnoreCase))
                        ?? throw new FileNotFoundException($"Файл {app.ExecutablePath} не найден в архиве");

                    sourcePath = Path.Combine(tempDir, app.ExecutablePath);
                    entry.WriteToFile(sourcePath);
                    _logger.LogInformation("Файл {File} извлечён в {Path}", app.ExecutablePath, sourcePath);
                }

                if (!File.Exists(sourcePath))
                    throw new FileNotFoundException("Ошибка: файл не найден после извлечения");

                token.ThrowIfCancellationRequested();

                // 4. Если портативная — просто копируем
                if (app.IsPortable)
                {
                    string targetDir = Path.Combine(installPath, app.Name);
                    Directory.CreateDirectory(targetDir);
                    string destinationFile = Path.Combine(targetDir, Path.GetFileName(sourcePath));

                    await Task.Run(() =>
                    {
                        _logger.LogDebug("{App} — переносим как портативную программу", app.Name);
                        token.ThrowIfCancellationRequested();
                        File.Copy(sourcePath, destinationFile, overwrite: true);

                        if (!string.IsNullOrWhiteSpace(app.ShortcutName))
                            CreateShortcut(destinationFile, app.ShortcutName);
                    }, token);

                    return;
                }

                // 5. НЕ портативная (например, VLC, GIMP и т.д.)
                _logger.LogDebug("{App} — запуск установщика", app.Name);

                // 5.1. Гарантируем существование папки назначения
                string appInstallPath = Path.Combine(installPath, app.Name);
                Directory.CreateDirectory(appInstallPath);

                // 5.2. Если это VLC (определяем по имени, можно уточнить условие),
                //      то создаём ключ в HKLM\SOFTWARE\VideoLAN\VLC\InstallDir и не передаем /D=… в аргументах.
                bool isVlcInstaller = app.Name.StartsWith("vlc", StringComparison.OrdinalIgnoreCase);

                if (isVlcInstaller)
                {
                    try
                    {
                        // Создаём (или открываем) ветку HKEY_LOCAL_MACHINE\SOFTWARE\VideoLAN\VLC
                        using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\VideoLAN\VLC"))
                        {
                            key.SetValue("InstallDir", appInstallPath, RegistryValueKind.String);
                        }
                        _logger.LogInformation("Написали в реестр HKLM\\SOFTWARE\\VideoLAN\\VLC\\InstallDir = {Path}", appInstallPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Не удалось записать ключ InstallDir в реестр для VLC");
                        // Сохраняем имя этого приложения в список неудачных
                        _registryFailedApps.Add(app.Name);
                        return; // <- сразу выходим, установка VLC пропущена
                    }
                }

                // 5.3. Формируем аргументы для запуска инсталлятора
                var argsList = new List<string>();

                // 5.3.1. Добавляем кастомные параметры ("/S", "/L=ru" и т.п.)
                foreach (var paramValue in app.CustomParameters.Values
                             .Where(v => !string.IsNullOrWhiteSpace(v)))
                {
                    argsList.Add(paramValue.Trim());
                }

                // 5.3.2. Если НЕ VLC, добавляем ключ "/D=<путь>"
                if (!isVlcInstaller)
                {
                    string pathArg = app.PathParameterKey + appInstallPath;
                    if (appInstallPath.Contains(" "))
                        pathArg = $"{app.PathParameterKey}\"{appInstallPath}\"";
                    argsList.Add(pathArg);
                }

                // 5.3.3. Склеиваем всё через пробел (никаких кавычек вокруг finalArgs!)
                string finalArgs = string.Join(" ", argsList);

                _logger.LogDebug("Формирование аргументов для \"{App}\": {Args}", app.Name, finalArgs);
                // Если isVlcInstaller == true, finalArgs будет, например, "/S /L=ru"
                // И VLC возьмёт путь из реестра, а не из /D=

                // 5.4. Настраиваем ProcessStartInfo
                var startInfo = new ProcessStartInfo
                {
                    FileName = sourcePath,
                    Arguments = finalArgs,
                    UseShellExecute = true,
                    Verb = "runas",  // поднимаем права для запуска экзешника (UAC)
                    WorkingDirectory = Path.GetDirectoryName(sourcePath) ?? string.Empty
                };

                _logger.LogDebug("Командная строка: {FileName} {Arguments}", startInfo.FileName, startInfo.Arguments);

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                        throw new InvalidOperationException("Не удалось запустить процесс установки");

                    try
                    {
                        await process.WaitForExitAsync(token);
                        _logger.LogDebug("{App} процесс завершён с кодом {Code}", app.Name, process.ExitCode);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("Установка {App} отменена", app.Name);
                        if (!process.HasExited)
                        {
                            process.Kill();
                            _logger.LogDebug("Процесс {App} принудительно завершён", app.Name);
                        }
                        throw;
                    }
                }

                // 5.5. После завершения установки VLC удаляем ключ из реестра
                if (isVlcInstaller)
                {
                    try
                    {
                        Registry.LocalMachine.DeleteSubKeyTree(@"SOFTWARE\VideoLAN\VLC", throwOnMissingSubKey: false);
                        _logger.LogInformation("Ключ реестра HKLM\\SOFTWARE\\VideoLAN\\VLC удалён");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Не удалось удалить ключ реестра HKLM\\SOFTWARE\\VideoLAN\\VLC");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Установка {App} отменена", app.Name);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка установки {App}", app.Name);
                throw new InvalidOperationException($"Ошибка установки {app.Name}", ex);
            }
            finally
            {
                // 6. Очистка временных папок
                if (tempDir != null && Directory.Exists(tempDir))
                {
                    try
                    {
                        Directory.Delete(tempDir, recursive: true);
                        _logger.LogDebug("Временная папка удалена: {TempDir}", tempDir);

                        var parentTempDir = Path.GetDirectoryName(tempDir);
                        if (Directory.Exists(parentTempDir)
                            && !Directory.EnumerateFileSystemEntries(parentTempDir).Any())
                        {
                            Directory.Delete(parentTempDir);
                            _logger.LogDebug("Родительская временная папка удалена: {ParentTempDir}", parentTempDir);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка удаления временной папки");
                    }
                }
            }
        }

        //Дополнительный чистильщик временной папки (на случай аварийного завершения)
        private static void CleanupOldTemp(string installPath, ILogger<Form1> logger)
        {
            string tempRoot = Path.Combine(installPath, "Temp");
            if (!Directory.Exists(tempRoot)) return;

            logger.LogWarning("Обнаружены остаточные данные в папке Temp {tempRoot}", tempRoot);
            foreach (var dir in Directory.GetDirectories(tempRoot, "WSI_*"))
            {
                try
                {
                    Directory.Delete(dir, recursive: true);
                    logger.LogDebug("Временная папка удалена: {dir}", dir);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Ошибка удаления остаточной временной папки");
                }
            }

            // Пытаемся удалить саму папку Temp, если пустая
            try
            {
                if (!Directory.EnumerateFileSystemEntries(tempRoot).Any())
                {
                    Directory.Delete(tempRoot);
                    logger.LogDebug("Родительская временная папка удалена: {tempRoot}", tempRoot);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Не удалось удалить остаточную папку Temp");
            }
        }

        private void BtnToggleSelection_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridViewPrograms.Rows)
                row.Cells["colSelect"].Value = !allSelected;

            allSelected = !allSelected;
            btnToggleSelection.Text = allSelected ? "Снять выделение" : "Выбрать все";
        }

        private async void BtnInstall_Click(object sender, EventArgs e)
        {
            _logger.LogInformation("Нажата кнопка «Установить»");
            // Блокируем кнопки: Запустить и Снять выделение
            btnInstall.Enabled = false;
            btnToggleSelection.Enabled = false;
            btnCancelInstall.Enabled = true;   // активируем «Отменить»
            // Сбрасываем список неудачных записей в реестр перед началом
            _registryFailedApps.Clear();

            _cts = new CancellationTokenSource();

            try
            {
                string installPath = txtInstallPath.Text.Trim();
                _logger.LogInformation("Путь установки: {Path}", installPath);
                if (string.IsNullOrWhiteSpace(installPath))
                {
                    MessageBox.Show("Пожалуйста, выберите путь установки.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var checkedApps = dataGridViewPrograms.Rows
                    .Cast<DataGridViewRow>()
                    .Where(r => Convert.ToBoolean(r.Cells["colSelect"].Value))
                    .Select(r => r.DataBoundItem as InstallableApp)
                    .ToList();

                if (checkedApps.Count == 0)
                {
                    MessageBox.Show("Не выбрана ни одна программа для установки.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int total = checkedApps.Count;
                _logger.LogInformation("Начинаем установку {Count} приложений", total);

                for (int i = 0; i < total; i++)
                {
                    if (_cts.Token.IsCancellationRequested)
                    {
                        _logger.LogWarning("Прерывание после {Index}/{Total}", i, total);
                        lblStatus.Text = "Отменено";
                        break;
                    }

                    var app = checkedApps[i];
                    if (app is null) // Защита от null
                    {
                        _logger.LogError("Элемент {Index} в списке установки равен null", i);
                        continue;
                    }

                    _logger.LogInformation("Устанавливается {AppName} ({Index}/{Total})", app.Name, i + 1, total);
                    lblStatus.Text = $"Устанавливается {app.Name} ({i + 1}/{total})";
                    progressBar.Value = (int)(((i + 1f) / total) * 100);

                    try
                    {
                        await InstallAppAsync(app, installPath, _cts.Token);
                        _logger.LogInformation("{AppName} успешно установлен", app.Name);
                    }
                    catch (OperationCanceledException)
                    {
                        lblStatus.Text = "Отменено";
                        _logger.LogWarning("Установка {AppName} отменена", app.Name);
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка при установке {AppName}", app.Name);
                        MessageBox.Show($"Ошибка при установке {app.Name}:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        break;
                    }
                }

                // После цикла проверяем, есть ли приложения, для которых не удалось записать в реестр
                if (_registryFailedApps.Count > 0)
                {
                    string failedList = string.Join(", ", _registryFailedApps);
                    MessageBox.Show(
                        $"Нет доступа к реестру для установки следующих программ:\n{failedList}",
                        "Внимание",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                // Если не было отмены или критических ошибок — показываем, что всё готово
                if (!_cts.Token.IsCancellationRequested)
                {
                    _logger.LogInformation("Все приложения установлены успешно");
                    MessageBox.Show("Установка завершена!", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            finally
            {
                _logger.LogInformation("Процесс установки завершён");
                // Сброс состояний
                btnInstall.Enabled = true;
                btnToggleSelection.Enabled = true;
                btnCancelInstall.Enabled = false;
                statusTimer.Stop();
                lblStatus.Text = "Готово";
                _cts?.Dispose();
                _cts = null;
            }
        }


        private void BtnCancelInstall_Click(object sender, EventArgs e)
        {
            _logger.LogWarning("Пользователь нажал «Отменить»");
            btnCancelInstall.Enabled = false;  // чтобы несколько раз не нажали
            _cts?.Cancel();
        }

        private void StatusTimer_Tick(object sender, EventArgs e)
        {
            // dotCount циклически принимает значения 0,1,2,3
            dotCount = (dotCount + 1) % 4;
            // Формируем строку: "Установка", "Установка.", "Установка..", "Установка..."
            lblStatus.Text = "Установка" + new string('.', dotCount);
        }

        private void DataGridViewPrograms_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = dataGridViewPrograms.Rows[e.RowIndex];
            if (row.DataBoundItem is not InstallableApp app || app == null) return;

            // Открываем диалог с копией текущих параметров
            using var dlg = new ParamForm(new Dictionary<string, string>(app.CustomParameters));
            dlg.StartPosition = FormStartPosition.CenterParent;
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                // Применяем изменения
                app.CustomParameters = dlg.Parameters;
                // Обновляем грид, чтобы отобразить новые ParametersDisplay
                dataGridViewPrograms.Refresh();
            }
        }
    }
}