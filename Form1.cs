using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SharpCompress.Archives;
using SharpCompress.Common;
using WindSoftInstaller.Services;
using WindSoftInstaller.Utilities;
using File = System.IO.File;

namespace WindSoftInstaller
{
    [SupportedOSPlatform("windows")]
    public partial class Form1 : Form
    {
        // Список приложений, загружаемый из AppRepository
        private readonly List<InstallableApp> apps = AppRepository.LoadApps();
        private readonly Dictionary<string, InstallableApp> appLookup;
        private bool allSelected = false; // флаг для кнопки «Выбрать/Снять выделение»
        int dotCount = 0; // счётчик для анимации статуса «Установка...»
        private CancellationTokenSource? _cts; // Делаем nullable // источник токена отмены
        private readonly ILogger<Form1> _logger;
        // Список приложений, для которых не удалось записать ключ в реестр
        private readonly List<string> _registryFailedApps = [];
        int exitCode = -1; // Инициализируем код выхода значением по умолчанию
        private readonly ShortcutService shortcutService;
        private readonly KeePassConfigurator kpConfigurator;
        private readonly InstallationManager installationManager;

        public Form1(ILogger<Form1> logger)
        {
            // Сначала инициализируем логгер
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            shortcutService = new ShortcutService(_logger);
            kpConfigurator = new KeePassConfigurator(_logger);
            installationManager = new InstallationManager(_logger);
            // Строим словарь для быстрого поиска по имени
            appLookup = apps
                .Where(a => !string.IsNullOrWhiteSpace(a.Name))
                .ToDictionary(a => a.Name, StringComparer.OrdinalIgnoreCase);

            _logger.LogInformation("Form1 constructor: старт");
            InitializeComponent();
            // Проверка на null
            _logger.LogInformation("Form1 constructor: объект формы создаётся");

            // Очищаем старые временные папки, если они остались от предыдущих неудачных запусков
            TempCleaner.Cleanup(Properties.Settings.Default.LastInstallPath, _logger);

            // Загружаем иконку из ресурсов
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream("WindSoftInstaller.Resources.logo.ico");
                if (stream != null)
                {
                    this.Icon?.Dispose();
                    using (var ico = new Icon(stream))
                        this.Icon = (Icon)ico.Clone();
                    this.ShowIcon = true;
                    _logger.LogDebug("Иконка формы загружена из manifest‑ресурса");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки иконки");
                // Если не удалось, задаём дефолтную иконку Windows
                this.Icon = SystemIcons.Application;
            }

            // Восстанавливаем последний путь, если он задан
            var saved = Properties.Settings.Default.LastInstallPath;
            if (!string.IsNullOrWhiteSpace(saved) && Directory.Exists(saved))
            {
                txtInstallPath.Text = saved;
                _logger.LogDebug("Восстановлен последний путь установки: {Path}", saved);
            }
            // Задаём фон формы и единый шрифт
            this.BackColor = Color.LightSteelBlue;
            this.Font = new Font("Segoe UI", 9F);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Подписываемся на форматирование ячеек
            dataGridViewPrograms.CellFormatting += DataGridViewPrograms_CellFormatting;
            // Добавляем обработчик изменения значений
            dataGridViewPrograms.CellValueChanged += DataGridViewPrograms_CellValueChanged;

            // Инициализируем сумму при загрузке
            CalculateAndShowTotalSize();

            _logger.LogInformation("Form1_Load: форма загружена, начинаем извлечение иконок для {Count} приложений", apps.Count);

            // 1. Папка с иконками (relative к тому, где лежит exe)
            // На этом этапе мы не лезем в архив — иконки уже лежат в Icons\<имя>.ico
            string iconsFolder = Path.Combine(Application.StartupPath, "Icons");
            _logger.LogDebug("Иконки будут искаться в папке: {IconsFolder}", iconsFolder);

            foreach (var app in apps)
            {
                try
                {
                    // Формируем имя .ico по тому же имени EXE
                    // Например, "vlc-3.0.21.exe" → "vlc-3.0.21.ico"
                    string icoName = Path.ChangeExtension(app.ExecutablePath, ".ico");
                    string icoPath = Path.Combine(iconsFolder, icoName);

                    if (File.Exists(icoPath))
                    {
                        // Если нашли файл, загружаем его
                        // Загружаем иконку и сохраняем в app.Icon
                        //и гарантированно освобождаем старые Bitmap
                        using var ico = new Icon(icoPath);
                        var bmp = ico.ToBitmap();
                        app.Icon?.Dispose();
                        app.Icon = bmp;
                        _logger.LogDebug("Иконка для {App} загружена из {Path}", app.Name, icoPath);
                    }
                    else
                    {
                        _logger.LogWarning("Не найден .ico для {App} по пути {Path}", app.Name, icoPath);
                        app.Icon = null; // если иконки нет, просто оставляем пустой
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось загрузить иконку для {App}", app.Name);
                    app.Icon = null;
                }
            }

            _logger.LogInformation("Иконки загружены, инициализируем DataGridView");

            dataGridViewPrograms.AutoGenerateColumns = false;
            var bindingList = new BindingList<InstallableApp>(apps);
            dataGridViewPrograms.DataSource = bindingList;

            _logger.LogInformation("DataGridView инициализирована");
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

        

        private void BtnToggleSelection_Click(object sender, EventArgs e)
        {
            bool chromeRequested = false;
            DialogResult chromeDialogResult = DialogResult.None;

            foreach (DataGridViewRow row in dataGridViewPrograms.Rows)
            {
                if (row.DataBoundItem is InstallableApp app)
                {
                    if (app.Name.Equals("Google Chrome", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!allSelected) // если сейчас снимаем выделение — не спрашиваем
                        {
                            chromeRequested = true;
                            continue; // временно пропускаем, обработаем позже
                        }
                    }

                    row.Cells["colSelect"].Value = !allSelected;
                }
            }

            // Отдельно обрабатываем Chrome, если он был найден и мы сейчас выделяем всё
            if (!allSelected && chromeRequested)
            {
                chromeDialogResult = MessageBox.Show(
                    "Установка Google Chrome возможна только в папку по умолчанию (обычно на диск C:). Продолжить?",
                    "Ограничение установки Chrome",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Warning
                );

                foreach (DataGridViewRow row in dataGridViewPrograms.Rows)
                {
                    if (row.DataBoundItem is InstallableApp app &&
                        app.Name.Equals("Google Chrome", StringComparison.OrdinalIgnoreCase))
                    {
                        row.Cells["colSelect"].Value = chromeDialogResult == DialogResult.OK;
                        break;
                    }
                }
            }

            allSelected = !allSelected;
            btnToggleSelection.Text = allSelected ? "Снять выделение" : "Выбрать все";
            CalculateAndShowTotalSize();
        }

        private void CalculateAndShowTotalSize()
        {
            double totalSize = 0;

            foreach (DataGridViewRow row in dataGridViewPrograms.Rows)
            {
                // Проверяем, что строка не является новой (пустой) строкой
                if (!row.IsNewRow &&
                    row.Cells["colSelect"].Value != null &&
                    Convert.ToBoolean(row.Cells["colSelect"].Value) &&
                    row.DataBoundItem is InstallableApp app)
                {
                    totalSize += app.SizeMB;
                }
            }

            lblTotalSize.Text = $"Общий размер выбранных программ: {totalSize:N2} МБ";
        }

        private void DataGridViewPrograms_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // Обрабатываем клик только по колонке с чекбоксом
            if (e.RowIndex >= 0 && e.ColumnIndex == colSelect.Index)
            {
                var row = dataGridViewPrograms.Rows[e.RowIndex];
                var cell = row.Cells[e.ColumnIndex];
                if (row.DataBoundItem is not InstallableApp app) return;

                // Получаем текущее значение до клика
                bool currentValue = cell.Value is bool val && val;

                // Chrome — особая проверка при попытке включения
                if (app.Name.Equals("Google Chrome", StringComparison.OrdinalIgnoreCase) && !currentValue)
                {
                    var result = MessageBox.Show(
                        "Установка Google Chrome возможна только в папку по умолчанию (обычно на диск C:). Продолжить?",
                        "Ограничение установки Chrome",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Warning
                    );

                    if (result == DialogResult.Cancel)
                    {
                        // Принудительно отменяем автоматическую установку галочки
                        dataGridViewPrograms.CancelEdit();
                        cell.Value = false; // На всякий случай
                        return;
                    }
                }

                // Принудительно завершаем текущее редактирование
                dataGridViewPrograms.CommitEdit(DataGridViewDataErrorContexts.Commit);

                // Инвертируем значение вручную
                cell.Value = !currentValue;

                // Обновляем сумму выбранных
                CalculateAndShowTotalSize();
            }

            // Обработка кликов по ссылкам лицензии
            if (e.RowIndex >= 0 && e.ColumnIndex == colLicense.Index)
            {
                if (dataGridViewPrograms.Rows[e.RowIndex].DataBoundItem is not InstallableApp app) return;

                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = app.LicenseUrl,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка открытия ссылки на лицензию");
                    MessageBox.Show($"Ошибка: {ex.Message}\nURL: {app.LicenseUrl}",
                        "Ошибка",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
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

                // Собираем из грида список выбранных приложений
                var checkedApps = dataGridViewPrograms.Rows
                    .Cast<DataGridViewRow>()
                    .Where(r => Convert.ToBoolean(r.Cells["colSelect"].Value))
                    .Select(r => r.DataBoundItem as InstallableApp)
                    .ToList();


                if (checkedApps.Any(a => a.Name == "Marble"))
                {
                    if (appLookup.TryGetValue("VC++ 2013 Redistributable (x86)", out var vc2013x86)
                        && !checkedApps.Contains(vc2013x86))
                    {
                        checkedApps.Insert(0, vc2013x86);
                    }
                    if (appLookup.TryGetValue("VC++ 2013 Redistributable (x64)", out var vc2013x64)
                        && !checkedApps.Contains(vc2013x64))
                    {
                        checkedApps.Insert(vc2013x86 != null ? 1 : 0, vc2013x64);
                    }
                }

                if (checkedApps.Any(a => a.Name == "MSI Afterburner") || checkedApps.Any(a => a.Name == "RivaTuner Statistics Server"))
                {
                    if (appLookup.TryGetValue("Microsoft VC++ 2015-2022 Redistributable (x86)", out var vc2015x86)
                     && !checkedApps.Contains(vc2015x86))
                    {
                        checkedApps.Insert(0, vc2015x86);
                    }
                    if (appLookup.TryGetValue("Microsoft VC++ 2015-2022 Redistributable (x64)", out var vc2015x64)
                     && !checkedApps.Contains(vc2015x64))
                    {
                        checkedApps.Insert(vc2015x86 != null ? 1 : 0, vc2015x64);
                    }
                }

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
                    Invoke(() => {
                        lblStatus.Text = $"Устанавливается {app.Name} ({i + 1}/{total})";
                        progressBar.Value = (int)(((i + 1f) / total) * 100);
                    });

                    try
                    {
                        await installationManager.InstallAppAsync(app, installPath, _cts.Token);
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

        private void DataGridViewPrograms_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != colLicense.Index)
                return;

            e.Value = "Просмотр";
            e.FormattingApplied = true;
        }

        private void DataGridViewPrograms_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            // Обновляем сумму при изменении состояния чекбокса
            if (e.ColumnIndex == colSelect.Index && e.RowIndex >= 0)
            {
                CalculateAndShowTotalSize();
            }
        }

        private void DataGridViewPrograms_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex == colSelect.Index) return;

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