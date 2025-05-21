using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using IWshRuntimeLibrary;
using Microsoft.Extensions.Logging;
using File = System.IO.File;

namespace WindSoftInstaller
{
    public partial class Form1 : Form
    {
        private readonly List<InstallableApp> apps = AppRepository.LoadApps();
        private bool allSelected = false;
        int dotCount = 0;
        private CancellationTokenSource? _cts; // Делаем nullable
        private readonly ILogger<Form1> _logger;

        public Form1(ILogger<Form1> logger)
        {
            InitializeComponent();

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
                this.Icon = new System.Drawing.Icon(stream);
            }
            _logger = logger;
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
                        app.Icon = sysIcon?.ToBitmap();
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
            dataGridViewPrograms.DataSource = bindingList;  // привязываем список объектов :contentReference[oaicite:12]{index=12}
            _logger.LogInformation("DataGridView инициализирована");

        }

        private void CreateShortcut(string targetPath, string shortcutName)
        {
            _logger.LogDebug("Создаём ярлык {Shortcut} → {Target}", shortcutName, targetPath);
            if (!File.Exists(targetPath))
            {
                _logger.LogError("Целевой файл {Target} для ярлыка {Shortcut} не найден.", shortcutName, targetPath);
                throw new FileNotFoundException($"Целевой файл для ярлыка не найден: {targetPath}");
            }

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string shortcutPath = Path.Combine(desktopPath, $"{shortcutName}.lnk");

            try
            {
                dynamic shell = new WshShell();
                dynamic shortcut = shell.CreateShortcut(shortcutPath);

                shortcut.TargetPath = targetPath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
                shortcut.WindowStyle = 1; // Обычное окно
                // При желании можно также:
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
            try
            {
                string sourcePath = Path.Combine(Application.StartupPath, app.ExecutablePath);

                if (!File.Exists(sourcePath))
                {
                    _logger.LogError("Исходный файл не найден для {App}: {Path}", app.Name, sourcePath);
                    throw new FileNotFoundException($"Файл установщика не найден: {sourcePath}");
                }


                token.ThrowIfCancellationRequested();

                if (app.IsPortable)
                {
                    string targetDir = Path.Combine(installPath, app.Name);
                    Directory.CreateDirectory(targetDir);

                    string destinationFile = Path.Combine(targetDir, Path.GetFileName(app.ExecutablePath));

                    await Task.Run(() =>
                    {
                        _logger.LogDebug("{App} — переносим как портативную программу", app.Name);
                        token.ThrowIfCancellationRequested();
                        File.Copy(sourcePath, destinationFile, overwrite: true);
                        token.ThrowIfCancellationRequested();
                        if (!string.IsNullOrWhiteSpace(app.ShortcutName))
                            CreateShortcut(destinationFile, app.ShortcutName);
                    }, token);
                    _logger.LogDebug("{App} скопирован в {Dir}", app.Name, Path.Combine(installPath, app.Name));
                }
                else
                {
                    _logger.LogDebug("{App} — устанавливаем через процесс", app.Name);
                    var arguments = new StringBuilder();
                    foreach (var param in app.CustomParameters.Where(p => !string.IsNullOrWhiteSpace(p.Value)))
                    {
                        arguments.Append($" {param.Value}");
                    }
                    if (arguments.Length > 0) arguments.Append($" /D=\"\"{installPath}\"\"");
                    else arguments.Append($"/D=\"\"{installPath}\"\"");

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = sourcePath,
                        Arguments = arguments.ToString(),
                        UseShellExecute = true,
                        Verb = "runas",
                        WorkingDirectory = Path.GetDirectoryName(sourcePath)
                    };

                    using var process = new Process { StartInfo = startInfo };
                    process.Start();

                    await Task.Run(() =>
                    {
                        try
                        {
                            // Ваши длительные действия:
                            while (!process.HasExited)
                            {
                                token.ThrowIfCancellationRequested();
                                Thread.Sleep(100);
                            }
                            _logger.LogDebug("{App} процесс установки запущен", app.Name);
                        }
                        catch (OperationCanceledException)
                        {
                            // Поглощаем отмену внутри задачи
                            _logger.LogWarning("InstallAppAsync для {App} отменён", app.Name);
                        }
                    }, token);
                }
            }
            catch (OperationCanceledException)
            {
                // Просто выходим, без перехвата дальше
                _logger.LogWarning("InstallAppAsync для {App} отменён", app.Name);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось установить {App}", app.Name);
                throw;
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
                    .Where(app => app != null)
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
            if (dataGridViewPrograms.Rows[e.RowIndex].DataBoundItem is not InstallableApp app) return;

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