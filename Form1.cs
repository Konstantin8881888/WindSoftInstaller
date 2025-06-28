using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SharpCompress.Archives;
using SharpCompress.Common;
using File = System.IO.File;

namespace WindSoftInstaller
{
    [SupportedOSPlatform("windows")]
    public partial class Form1 : Form
    {
        // Список приложений, загружаемый из AppRepository
        private readonly List<InstallableApp> apps = AppRepository.LoadApps();
        private bool allSelected = false; // флаг для кнопки «Выбрать/Снять выделение»
        int dotCount = 0; // счётчик для анимации статуса «Установка...»
        private CancellationTokenSource? _cts; // Делаем nullable // источник токена отмены
        private readonly ILogger<Form1> _logger;
        // Список приложений, для которых не удалось записать ключ в реестр
        private readonly List<string> _registryFailedApps = [];
        int exitCode = -1; // Инициализируем код выхода значением по умолчанию

        public Form1(ILogger<Form1> logger)
        {
            // Сначала инициализируем логгер
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("Form1 constructor: старт");
            InitializeComponent();
            // Проверка на null
            _logger.LogInformation("Form1 constructor: объект формы создаётся");

            // Очищаем старые временные папки, если они остались от предыдущих неудачных запусков
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
                    _logger.LogDebug("Иконка формы загружена из ресурсов");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки иконки");
                // Если не удалось, задаём дефолтную иконку Windows
                this.Icon = SystemIcons.Application;
            }

            // Используем MemoryStream для конвертации byte[] в Icon
            byte[] iconBytes = Properties.Resources.logo;
            using var iconStream = new MemoryStream(iconBytes);
            this.Icon = new Icon(iconStream);// Явное указание типа Icon
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
                        using var ico = new Icon(icoPath);
                        app.Icon = ico.ToBitmap();
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


        private void CreateShortcut(string targetPath, string shortcutName)
        {
            _logger.LogDebug("Создаём ярлык {Shortcut} → {Target}", shortcutName, targetPath);

            // Проверяем, что файл, на который ссылаемся, существует
            if (!File.Exists(targetPath))
            {
                _logger.LogError("Целевой файл {Target} для ярлыка {Shortcut} не найден.", targetPath, shortcutName);
                throw new FileNotFoundException($"Целевой файл для ярлыка не найден: {targetPath}");
            }

            string? desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (string.IsNullOrEmpty(desktopPath))
            {
                _logger.LogError("Не удалось получить путь к рабочему столу.");
                throw new InvalidOperationException("Не удалось получить путь к рабочему столу");
            }
            string shortcutPath = Path.Combine(desktopPath, $"{shortcutName}.lnk");

            object? shellObject = null;
            object? shortcutObject = null;

            try
            {
                // Создаём COM-объект WScript.Shell для работы с ярлыками
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
                string? directory = Path.GetDirectoryName(targetPath);
                shortcut.WorkingDirectory = directory;
                shortcut.WindowStyle = 1; // обычный режим окна
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

        /// Основной метод установки одного приложения (app). 
        /// Если app.IsPortable == true, то просто копируем папку и создаём ярлык.
        /// Иначе — распаковываем инсталлятор, формируем аргументы, запускаем процесс, ждём завершения.
        private async Task InstallAppAsync(InstallableApp app, string installPath, CancellationToken token)
        {
            _logger.LogDebug("Начало InstallAppAsync для {App}", app.Name);
            // Проверяем, запущено ли приложение от имени администратора (важно для записи в реестр и некоторых инсталляторов)
            bool isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            _logger.LogInformation("Запущено с правами администратора: {IsAdmin}", isAdmin);
            string? tempDir = null; // папка, куда распакуем exe из архива

            try
            {
                // 1. Формируем путь к архиву и проверяем его наличие
                string archivePath = Path.Combine(Application.StartupPath, "Installers.7z");
                if (!File.Exists(archivePath))
                {
                    _logger.LogError("Installers.7z не найден в пути {Archive}", archivePath);
                    throw new FileNotFoundException($"Архив не найден: {archivePath}");
                }


                // 2. Создаём временную папку для распаковки (WSI_<GUID>)
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

                // Ещё раз проверяем, что файл действительно оказался где нужно
                if (!File.Exists(sourcePath))
                    throw new FileNotFoundException("Ошибка: файл не найден после извлечения");

                token.ThrowIfCancellationRequested();

                // 4. Если портативная — распаковываем ZIP/7z
                if (app.IsPortable)
                {
                    string targetDir = Path.Combine(installPath, app.Name);
                    Directory.CreateDirectory(targetDir);

                    // Выясняем расширение
                    string ext = Path.GetExtension(sourcePath).ToLowerInvariant();
                    if (ext == ".zip" || ext == ".7z")
                    {
                        _logger.LogDebug("{App} — распаковка архива {Zip}", app.Name, Path.GetFileName(sourcePath));
                        using var archive = ArchiveFactory.Open(sourcePath);
                        foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
                        {
                            string outPath = Path.Combine(targetDir, entry.Key);
                            Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
                            entry.WriteToFile(outPath, new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
                        }
                    }
                    else if (app.Name.Equals("Bitwarden", StringComparison.OrdinalIgnoreCase))
                    {
                        string destinationFile = Path.Combine(targetDir, Path.GetFileName(sourcePath)); // Путь к файлу Bitwarden

                        // Копируем портативную версию Bitwarden в папку назначения
                        File.Copy(sourcePath, destinationFile, overwrite: true);
                        _logger.LogDebug("Файл {File} скопирован в {TargetDir}", sourcePath, targetDir);

                        // Создаем ярлык для Bitwarden
                        if (!string.IsNullOrWhiteSpace(app.ShortcutName))
                        {
                            string exePath = destinationFile; // Путь к исполняемому файлу
                            CreateShortcut(exePath, app.ShortcutName);
                            _logger.LogDebug("Создан ярлык для {App}: {ExePath}", app.Name, exePath);
                        }
                    }

                    else
                    {
                        // Для других portable (exe) просто копируем
                        string destFile = Path.Combine(targetDir, Path.GetFileName(sourcePath));
                        _logger.LogDebug("{App} — копирование portable EXE {File}", app.Name, Path.GetFileName(sourcePath));
                        File.Copy(sourcePath, destFile, overwrite: true);
                    }
                    // Правка языка для MSI Afterburner
                    if (app.Name.Equals("MSI Afterburner", StringComparison.OrdinalIgnoreCase))
                    {
                        string templateCfg = AppRepository.ExtractTemplate("MSIAfterburner.cfg", tempDir);
                        string profilesDir = Path.Combine(targetDir, "Profiles");
                        Directory.CreateDirectory(profilesDir);
                        File.Copy(templateCfg,
                                  Path.Combine(profilesDir, "MSIAfterburner.cfg"),
                                  overwrite: true);
                    }

                    // После распаковки создаём ярлык на главный exe
                    // 1) Определяем относительный путь к EXE
                    string relPath = app.ShortcutRelativePath
                        ?? Directory.EnumerateFiles(targetDir, "*.exe", SearchOption.TopDirectoryOnly)
                                    .Select(Path.GetFileName)
                                    .FirstOrDefault()
                        ?? string.Empty;

                    if (!string.IsNullOrEmpty(relPath))
                    {
                        string exePath = Path.Combine(targetDir, relPath);
                        if (File.Exists(exePath))
                        {
                            _logger.LogDebug("Создаём ярлык для {App}: {Exe}", app.Name, exePath);
                            CreateShortcut(exePath, app.ShortcutName!);
                        }
                        else
                        {
                            _logger.LogWarning("Не найден {Rel} для {App} в {Dir}", relPath, app.Name, targetDir);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Не удалось определить EXE для создания ярлыка в {Dir}", targetDir);
                    }

                    return;
                }

                // 5. НЕ портативная (например, VLC, GIMP и т.д.)
                _logger.LogDebug("{App} — запуск установщика", app.Name);

                // 5.1. Гарантируем существование папки назначения
                string appInstallPath = Path.Combine(installPath, app.Name);
                Directory.CreateDirectory(appInstallPath);
                _logger.LogInformation("Путь установки для {App}: {Path}", app.Name, appInstallPath);

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

                // 5.3.1. Добавляем кастомные параметры ("/S", "/L=ru" и т.п.), подставляя {InstallDir} для MSI
                foreach (var paramValue in app.CustomParameters.Values
                    .Where(v => !string.IsNullOrWhiteSpace(v)))
                {
                    // Заменяем плейсхолдер {InstallDir} на реальный путь
                    string p = paramValue.Replace("{InstallDir}", appInstallPath);
                    argsList.Add(p.Trim());
                }

                // 5.3.2-b. Спец-обработка для RivaTuner Statistics Server (NSIS) и FastStone Image Viewer (Inno Setup): всегда без кавычек
                if (app.Name.Equals("RivaTuner Statistics Server", StringComparison.OrdinalIgnoreCase)
                    || app.Name.Equals("FastStone Image Viewer", StringComparison.OrdinalIgnoreCase))
                {
                    // просто /D=<путь> без оборачивания
                    argsList.Add(app.PathParameterKey + appInstallPath);
                }

                // 5.3.2. Добавляем путь только для НЕ‑MSI и НЕ‑VLC и НЕ-RivaTuner Statistics Server и НЕ-FastStone Image Viewer
                else if (!isVlcInstaller
                    && !string.IsNullOrWhiteSpace(app.PathParameterKey)
                    && !sourcePath.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
                {
                    string pathArg = app.PathParameterKey + appInstallPath;
                    if (appInstallPath.Contains(' '))
                        pathArg = $"{app.PathParameterKey}\"{appInstallPath}\"";
                    argsList.Add(pathArg);
                }

                // 5.3.3. Склеиваем всё через пробел (никаких кавычек вокруг finalArgs!)
                string finalArgs = string.Join(" ", argsList);

                _logger.LogDebug("Формирование аргументов для \"{App}\": {Args}", app.Name, finalArgs);
                // Если isVlcInstaller == true, finalArgs будет, например, "/S /L=ru"
                // И VLC возьмёт путь из реестра, а не из /D=

                // 5.4. Настраиваем ProcessStartInfo в зависимости от расширения файла
                var startInfo = new ProcessStartInfo();
                string extension = Path.GetExtension(sourcePath) ?? string.Empty;

                if (extension.Equals(".msi", StringComparison.OrdinalIgnoreCase))
                {
                    startInfo.FileName = "msiexec.exe";

                    // Для Calibre и для PDFsam Basic используем административную распаковку (/a)
                    if (app.Name.Equals("Calibre", StringComparison.OrdinalIgnoreCase)
                     || app.Name.Equals("PDFsam Basic", StringComparison.OrdinalIgnoreCase))
                    {
                        // собираем все свойства из CustomParameters (в том числе TARGETDIR для PDFsam)
                        var props = string.Join(" ",
                            app.CustomParameters.Values.Select(p => p.Replace("{InstallDir}", appInstallPath).Trim()));

                        // /a — административная распаковка, /qn — тихий режим
                        startInfo.Arguments = $"/a \"{sourcePath}\" /qn {props}";
                    }
                    else
                    {
                        // обычная установка для всех остальных MSI
                        startInfo.Arguments =
                            $"/i \"{sourcePath}\" {finalArgs}";
                    }

                    startInfo.UseShellExecute = true;
                    startInfo.Verb = "runas";
                    startInfo.WorkingDirectory = Path.GetDirectoryName(sourcePath) ?? string.Empty;
                }
                else
                {
                    // EXE-инсталляторы
                    startInfo.FileName = sourcePath;
                    startInfo.Arguments = finalArgs;
                    startInfo.UseShellExecute = true;
                    startInfo.Verb = "runas";
                    startInfo.WorkingDirectory = Path.GetDirectoryName(sourcePath) ?? string.Empty;
                }

                _logger.LogDebug("Командная строка: {FileName} {Arguments}", startInfo.FileName, startInfo.Arguments);

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                        throw new InvalidOperationException("Не удалось запустить процесс установки");

                    try
                    {
                        await process.WaitForExitAsync(token);
                        _logger.LogDebug("{App} процесс завершён с кодом {Code}", app.Name, process.ExitCode);
                        exitCode = process.ExitCode; // Сохраняем код выхода
                                                     // После process.WaitForExitAsync(token) и до создания ярлыка
                        if (app.Name.Equals("RivaTuner Statistics Server", StringComparison.OrdinalIgnoreCase)
                            && process.ExitCode == 0)
                        {
                            string templateCfg = AppRepository.ExtractTemplate("Config", tempDir);
                            string profilesDir = Path.Combine(appInstallPath, "Profiles");
                            Directory.CreateDirectory(profilesDir);
                            File.Copy(templateCfg,
                                      Path.Combine(profilesDir, "Config"),
                                      overwrite: true);
                        }

                        // После await process.WaitForExitAsync(token) и перед удалением Temp-директории
                        if (process.ExitCode == 0)
                        {
                            // 1) Удаляем MSI из папки установки, если он там оказался
                            string installedMsi = Path.Combine(appInstallPath, Path.GetFileName(sourcePath));
                            if (File.Exists(installedMsi))
                            {
                                try
                                {
                                    File.Delete(installedMsi);
                                    _logger.LogDebug("Удалён установщик {Msi} из {Dir}", installedMsi, appInstallPath);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Не удалось удалить установщик {Msi}", installedMsi);
                                }
                            }

                            // 2) Создаём ярлык на главный exe, беря относительный путь из ShortcutRelativePath
                            if (!string.IsNullOrWhiteSpace(app.ShortcutRelativePath))
                            {
                                string exePath = Path.Combine(appInstallPath, app.ShortcutRelativePath);
                                if (File.Exists(exePath))
                                {
                                    _logger.LogDebug("Создаём ярлык для {App}: {Exe}", app.Name, exePath);
                                    CreateShortcut(exePath, app.ShortcutName ?? app.Name);
                                }
                                else
                                {
                                    _logger.LogWarning("Не найден {Exe} для {App}", exePath, app.Name);
                                }
                            }
                        }


                        // <<< Здесь добавляем создание ярлыка для LMMS >>>
                        if (app.Name.Equals("LMMS", StringComparison.OrdinalIgnoreCase) && process.ExitCode == 0)
                        {
                            // Предполагаем, что главный exe-файл называется "lmms.exe"
                            string exePath = Path.Combine(appInstallPath, "lmms.exe");
                            if (File.Exists(exePath))
                            {
                                _logger.LogDebug("Создаём ярлык для LMMS: {ExePath}", exePath);
                                CreateShortcut(exePath, "LMMS");
                            }
                            else
                            {
                                _logger.LogWarning("Не удалось найти lmms.exe по пути {ExePath}", exePath);
                            }
                        }
                        // <<< Создание ярлыка для HandBrake >>>
                        if (app.Name.Equals("HandBrake", StringComparison.OrdinalIgnoreCase) && process.ExitCode == 0)
                        {
                            // Путь к exe в папке установки
                            string exePath = Path.Combine(appInstallPath, "HandBrake.exe");
                            if (File.Exists(exePath))
                            {
                                _logger.LogDebug("Создаём ярлык для HandBrake: {ExePath}", exePath);
                                CreateShortcut(exePath, "HandBrake");
                            }
                            else
                            {
                                _logger.LogWarning("Не найден HandBrake.exe по пути {ExePath}", exePath);
                            }
                        }
                        // <<< Создание ярлыка для Clementine >>>
                        if (app.Name.Equals("Clementine", StringComparison.OrdinalIgnoreCase) && process.ExitCode == 0)
                        {
                            // Путь к exe в папке установки
                            string exePath = Path.Combine(appInstallPath, "Clementine.exe");
                            if (File.Exists(exePath))
                            {
                                _logger.LogDebug("Создаём ярлык для Clementine: {ExePath}", exePath);
                                CreateShortcut(exePath, "Clementine");
                            }
                            else
                            {
                                _logger.LogWarning("Не найден Clementine.exe по пути {ExePath}", exePath);
                            }
                        }
                        // <<< Создание ярлыка для ClamWin >>>
                        if (app.Name.Equals("ClamWin", StringComparison.OrdinalIgnoreCase) && process.ExitCode == 0)
                        {
                            // Путь к ClamWin.exe (обычно находится в подкаталоге \bin)
                            string exePath = Path.Combine(appInstallPath, "bin", "ClamWin.exe");
                            if (File.Exists(exePath))
                            {
                                _logger.LogDebug("Создаём ярлык для ClamWin: {ExePath}", exePath);
                                CreateShortcut(exePath, "ClamWin");
                            }
                            else
                            {
                                _logger.LogWarning("Не найден ClamWin.exe по пути {ExePath}", exePath);
                            }
                        }
                        // <<< Создание ярлыка для Cryptomator >>>
                        if (app.Name.Equals("Cryptomator", StringComparison.OrdinalIgnoreCase) && process.ExitCode == 0)
                        {
                            string exePath = Path.Combine(appInstallPath, "Cryptomator.exe");
                            if (!File.Exists(exePath))
                                exePath = Path.Combine(appInstallPath, "bin", "Cryptomator.exe");
                            if (File.Exists(exePath))
                            {
                                _logger.LogDebug("Создаём ярлык для Cryptomator: {Exe}", exePath);
                                CreateShortcut(exePath, "Cryptomator");
                            }
                            else
                            {
                                _logger.LogWarning("Не найден Cryptomator.exe в {Dir}", appInstallPath);
                            }
                        }
                        // <<< Создание ярлыка для KeePass >>>
                        if (app.Name.Equals("KeePass", StringComparison.OrdinalIgnoreCase) && process.ExitCode == 0)
                        {
                            string exePath = Path.Combine(appInstallPath, "KeePass.exe");
                            if (File.Exists(exePath))
                            {
                                _logger.LogDebug("Создаём ярлык для KeePass: {Exe}", exePath);
                                CreateShortcut(exePath, "KeePass");
                            }
                            else
                            {
                                _logger.LogWarning("Файл KeePass.exe не найден в {Path}", appInstallPath);
                            }
                        }
                        // <<< Создание ярлыка для UltraDefrag >>>
                        if (app.Name.Equals("UltraDefrag", StringComparison.OrdinalIgnoreCase) && process.ExitCode == 0)
                        {
                            string exePath = Path.Combine(appInstallPath, "ufd.gui.exe");
                            if (File.Exists(exePath))
                            {
                                _logger.LogDebug("Создаём ярлык для UltraDefrag: {ExePath}", exePath);
                                CreateShortcut(exePath, app.ShortcutName!);
                            }
                            else
                            {
                                _logger.LogWarning("Не найден ufd.gui.exe по пути {ExePath}", exePath);
                            }
                        }
                        // <<< Создание ярлыка для RivaTuner Statistics Server >>>
                        if (app.Name.Equals("RivaTuner Statistics Server", StringComparison.OrdinalIgnoreCase) && process.ExitCode == 0)
                        {
                            // Путь к exe в папке установки
                            string exePath = Path.Combine(appInstallPath, "RTSS.exe");
                            if (File.Exists(exePath))
                            {
                                _logger.LogDebug("Создаём ярлык для RivaTuner Statistics Server: {ExePath}", exePath);
                                CreateShortcut(exePath, "RivaTuner Statistics Server");
                            }
                            else
                            {
                                _logger.LogWarning("Не найден RTSS.exe по пути {ExePath}", exePath);
                            }
                        }
                        // <<< Создание ярлыка для Zotero >>>
                        if (app.Name.Equals("Zotero", StringComparison.OrdinalIgnoreCase) && process.ExitCode == 0)
                        {
                            // Путь к exe в папке установки
                            string exePath = Path.Combine(appInstallPath, "zotero.exe");
                            if (File.Exists(exePath))
                            {
                                _logger.LogDebug("Создаём ярлык для Zotero: {ExePath}", exePath);
                                CreateShortcut(exePath, "Zotero");
                            }
                            else
                            {
                                _logger.LogWarning("Не найден zotero.exe по пути {ExePath}", exePath);
                            }
                        }
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
                if (exitCode == 0)
                {
                    // 5.6. Копирование дополнительных файлов (языковых пакетов и т.д.)
                    if (app.AdditionalFiles != null && app.AdditionalFiles.Count > 0)
                    {
                        _logger.LogDebug("Копирование дополнительных файлов для {App}", app.Name);
                        using var archive = ArchiveFactory.Open(archivePath);
                        foreach (string file in app.AdditionalFiles)
                        {
                            // Ищем файл без учета регистра и пути
                            var entry = archive.Entries.FirstOrDefault(e =>
                                e.Key.EndsWith(file, StringComparison.OrdinalIgnoreCase));

                            if (entry == null)
                            {
                                _logger.LogWarning("Файл {File} не найден в архиве для {App}", file, app.Name);
                                continue;
                            }

                            // Определяем путь назначения
                            string destFileName = app.AdditionalFilesDestinations.TryGetValue(file, out string? dest)
                                ? dest
                                : Path.GetFileName(file);

                            string destPath = Path.Combine(appInstallPath, destFileName);

                            // Создаем целевую директорию при необходимости
                            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

                            // Извлекаем файл
                            entry.WriteToFile(destPath);
                            _logger.LogInformation("Файл {File} скопирован в {Path}", file, destPath);
                        }
                    }

                    // 5.7. Настройка языка - ТОЛЬКО ДЛЯ KEEPASS!
                    if (app is { Name: "KeePass" })
                    {
                        ApplyKeePassConfiguration(appInstallPath);
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
            dataGridViewPrograms.CellFormatting += DataGridViewPrograms_CellFormatting;
        }

        private void ApplyKeePassConfiguration(string installPath)
        {
            try
            {
                _logger.LogInformation("Применение конфигурации KeePass");

                // Путь к скопированному конфигу в папке установки
                string sourceConfig = Path.Combine(installPath, "KeePass.config.xml");

                // Проверяем существование файла
                if (!File.Exists(sourceConfig))
                {
                    _logger.LogError("ФАЙЛ КОНФИГУРАЦИИ НЕ НАЙДЕН В ПАПКЕ УСТАНОВКИ: {Path}", sourceConfig);

                    // Попробуем найти файл вручную
                    var allFiles = Directory.GetFiles(installPath, "*.config.xml", SearchOption.AllDirectories);
                    _logger.LogWarning("Найденные файлы конфигурации: {Files}", string.Join(", ", allFiles));

                    return;
                }

                // Путь к целевому конфигу в AppData
                string targetConfig = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "KeePass", "KeePass.config.xml"
                );

                if (!File.Exists(sourceConfig))
                {
                    _logger.LogWarning("Файл конфигурации KeePass не найден: {Path}", sourceConfig);
                    return;
                }

                // Проверяем содержимое конфига (опционально)
                string configContent = File.ReadAllText(sourceConfig);
                if (!configContent.Contains("Russian.lngx"))
                {
                    _logger.LogWarning("Конфиг не содержит русский язык! Файл: {Path}", sourceConfig);
                }

                // Создаем целевую директорию
                Directory.CreateDirectory(Path.GetDirectoryName(targetConfig)!);

                // Копируем с заменой
                File.Copy(sourceConfig, targetConfig, overwrite: true);
                _logger.LogInformation("Конфиг KeePass успешно скопирован в AppData");

                // Удаляем временную копию из папки установки
                try
                {
                    File.Delete(sourceConfig);
                    _logger.LogDebug("Временный конфиг удалён из папки установки");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось удалить временный конфиг");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка применения конфига KeePass");
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
            // Обновляем сумму
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
                // Обновляем значение чекбокса
                var cell = dataGridViewPrograms.Rows[e.RowIndex].Cells[e.ColumnIndex];
                cell.Value = !(cell.Value is bool val && val);

                // Обновляем сумму
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
                    if (app.LicenseUrl != null)
                    {
                        _logger.LogInformation("Ошибка открытия ссылки на лицензию {LicenseUrl}", app.LicenseUrl);
                    }
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

        private void UpdateTotalSize()
        {
            double totalSize = 0;

            foreach (DataGridViewRow row in dataGridViewPrograms.Rows)
            {
                if (row.Cells["colSelect"].Value is true &&
                    row.DataBoundItem is InstallableApp app)
                {
                    totalSize += app.SizeMB;
                }
            }

            lblTotalSize.Text = $"Общий размер выбранных программ: {totalSize:N2} МБ";
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
                UpdateTotalSize();
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