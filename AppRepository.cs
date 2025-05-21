namespace WindSoftInstaller
{
    internal class AppRepository
    {
        public static List<InstallableApp> LoadApps()
        {
            var apps = new List<InstallableApp>
            {
                new() {
                    Name = "vlc-3.0.21",
                    Description = "Мощный видеопроигрыватель с поддержкой большинства кодеков",
                    ExecutablePath = "Installers\\vlc-3.0.21.exe",
                    CustomParameters =
                    {
                        { "Режим установки", "/S" },
                        { "Язык", "/DE=ru" }
                    }
                },
                new() {
                    Name = "mplayerc",
                    Description = "Быстрый видеоплеер с минималистичным интерфейсом",
                    ExecutablePath = "Installers\\mplayerc.exe",
                    IsPortable = true,
                    ShortcutName = "MPC"
                }
            };
            foreach (var app in apps)
            {
                string fullPath = Path.Combine(Application.StartupPath, app.ExecutablePath);
                if (!File.Exists(fullPath))
                    throw new FileNotFoundException($"Installer not found: {fullPath}");
            }
            return apps;
        }
    }
}
