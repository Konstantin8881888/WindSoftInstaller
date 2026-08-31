namespace WindSoftInstaller
{
    public class InstallableApp
    {
        public required string Name { get; set; }
        public string DescriptionKey { get; set; } = "";
        public string Description => Localization.T(DescriptionKey);
        public required string ExecutablePath { get; set; }
        public bool IsPortable { get; set; } // True для портативных программ
        public string? ShortcutName { get; set; } // Имя ярлыка
        public Dictionary<string, string> CustomParameters { get; set; } = [];
        public string ParametersDisplay => string.Join("; ", CustomParameters.Select(p => $"{Localization.T(p.Key)}: {p.Value}")); //свойство для отображения параметров
        public Image? Icon { get; set; }
        public string PathParameterKey { get; set; } = "/D=";
        public required double SizeMB { get; set; } // Размер в мегабайтах
        public required string LicenseUrl { get; set; } // Ссылка на лицензию
        public string? ShortcutRelativePath { get; set; }  // путь к EXE внутри папки портативки, например "Start Scanner.exe"
        public List<string> AdditionalFiles { get; set; } = []; // Свойство для дополнительных файлов, не включённых в основной инсталятор/архив
        // Новые свойства для локализованных архивов
        public string ArchivePathEn { get; set; } = "";
        public string ArchivePathRu { get; set; } = "";
        // Метод для получения правильного архива в зависимости от языка
        public string GetLocalizedArchivePath()
        {
            // Если задан локализованный архив для текущего языка – используем его
            if (Localization.Current == "ru" && !string.IsNullOrEmpty(ArchivePathRu))
                return ArchivePathRu;
            else if (!string.IsNullOrEmpty(ArchivePathEn))
                return ArchivePathEn;
            else
                // Если архивы не заданы – возвращаем ExecutablePath как запасной вариант
                return ExecutablePath;
        }
    }
}