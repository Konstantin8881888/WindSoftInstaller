using System.Diagnostics;

namespace WindSoftInstaller
{
    public class InstallerArgumentsBuilder
    {
        private readonly InstallableApp _app;
        private readonly string _installDir;
        private readonly string _sourcePath;

        public InstallerArgumentsBuilder(InstallableApp app, string sourcePath, string installDir)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _sourcePath = sourcePath ?? throw new ArgumentNullException(nameof(sourcePath));
            _installDir = installDir ?? throw new ArgumentNullException(nameof(installDir));
        }

        /// <summary>
        /// Формирует строку аргументов для запуска инсталлятора или распаковщика.
        /// </summary>
        public string BuildArguments()
        {
            var argsList = new List<string>();

            // 1) Кастомные параметры
            foreach (var paramValue in _app.CustomParameters.Values
                         .Where(v => !string.IsNullOrWhiteSpace(v)))
            {
                argsList.Add(paramValue.Replace("{InstallDir}", _installDir).Trim());
            }

            // 2) Спец-обработка для некоторых NSIS/Inno приложений
            if (_app.Name.Equals("RivaTuner Statistics Server", StringComparison.OrdinalIgnoreCase)
                || _app.Name.Equals("FastStone Image Viewer", StringComparison.OrdinalIgnoreCase))
            {
                argsList.Add(_app.PathParameterKey + _installDir);
            }
            // 3) Для остальных НЕ MSI, НЕ VLC
            else if (!IsVlcInstaller && !IsMsiPackage)
            {
                var pathArg = _app.PathParameterKey + _installDir;
                if (_installDir.Contains(' '))
                    pathArg = $"{_app.PathParameterKey}\"{_installDir}\"";
                argsList.Add(pathArg);
            }

            return string.Join(" ", argsList);
        }

        /// <summary>
        /// Собирает ProcessStartInfo для запуска установки.
        /// </summary>
        public ProcessStartInfo BuildStartInfo()
        {
            var extension = Path.GetExtension(_sourcePath) ?? string.Empty;
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(_sourcePath) ?? string.Empty
            };

            if (extension.Equals(".msi", StringComparison.OrdinalIgnoreCase))
            {
                startInfo.FileName = "msiexec.exe";
                var args = BuildArguments();

                // Спец-случаи административной распаковки
                if (_app.Name.Equals("Calibre", StringComparison.OrdinalIgnoreCase)
                 || _app.Name.Equals("PDFsam Basic", StringComparison.OrdinalIgnoreCase))
                {
                    // все параметры включительно TARGETDIR
                    startInfo.Arguments = $"/a \"{_sourcePath}\" /qn {args}";
                }
                else
                {
                    startInfo.Arguments = $"/i \"{_sourcePath}\" {args}";
                }
            }
            else
            {
                startInfo.FileName = _sourcePath;
                startInfo.Arguments = BuildArguments();
            }

            return startInfo;
        }

        private bool IsVlcInstaller => _app.Name.StartsWith("vlc", StringComparison.OrdinalIgnoreCase);
        private bool IsMsiPackage => Path.GetExtension(_sourcePath)
                                        .Equals(".msi", StringComparison.OrdinalIgnoreCase);
    }
}
