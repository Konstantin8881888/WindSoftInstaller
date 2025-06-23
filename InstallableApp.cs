namespace WindSoftInstaller
{
    public class InstallableApp
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string ExecutablePath { get; set; }
        public bool IsPortable { get; set; } // True для портативных программ
        public string? ShortcutName { get; set; } // Имя ярлыка
        public Dictionary<string, string> CustomParameters { get; set; } = [];
        public string ParametersDisplay => string.Join("; ", CustomParameters.Select(p => $"{p.Key}: {p.Value}")); //свойство для отображения параметров
        public Image? Icon { get; set; }
        public string PathParameterKey { get; set; } = "/D=";
        public required double SizeMB { get; set; } // Размер в мегабайтах
        public required string LicenseUrl { get; set; } // Ссылка на лицензию
        public string? ShortcutRelativePath { get; set; }  // путь к EXE внутри папки портативки, например "Start Scanner.exe"
        public List<string> AdditionalFiles { get; set; } = []; // Свойство для дополнительных файлов, не включённых в основной инсталятор/архив
        public string? LanguageConfig { get; set; } // Свойство для дополнительных языковых файлов, не включённых в основной инсталятор/архив
        public Dictionary<string, string> AdditionalFilesDestinations { get; set; } = [];// Свойство для дополнительных языковых файлов, не включённых в основной инсталятор/архив

    }
}