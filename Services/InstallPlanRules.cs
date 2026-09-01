using System;

namespace WindSoftInstaller.Services
{
    /// <summary>
    /// Чистые бизнес-правила планирования установки.
    /// Не содержит ввода-вывода, процессов и реестра — полностью тестируемо.
    /// </summary>
    internal static class InstallPlanRules
    {
        /// <summary>Приложения, которые устанавливаются в системное расположение (определяются по имени).</summary>
        private static readonly string[] SystemApps =
        [
            "Google Chrome",
            "Microsoft VC++ 2015-2022 Redistributable (x64)",
            "Microsoft VC++ 2015-2022 Redistributable (x86)",
            "VC++ 2013 Redistributable (x64)",
            "VC++ 2013 Redistributable (x86)"
        ];

        /// <summary>Папки, которые потенциально могут остаться пустыми после установки в системное расположение.</summary>
        public static readonly string[] PotentialEmptyFolders =
        [
            "Google Chrome",
            "Microsoft VC++ 2015-2022 Redistributable (x64)",
            "Microsoft VC++ 2015-2022 Redistributable (x86)",
            "VC++ 2013 Redistributable (x64)",
            "VC++ 2013 Redistributable (x86)"
        ];

        /// <summary>Коды выхода установщика VC++, которые считаются успешными.</summary>
        public static readonly int[] VcRedistSuccessExitCodes = [0, 1638, 3010];

        /// <summary>Порог размера файла (байт), чтобы считаться "временным" при системной очистке.</summary>
        public const long TemporaryFileMaxBytes = 2 * 1024 * 1024;

        /// <summary>Общий лимит размера (байт) для удаления системной временной папки.</summary>
        public const long TemporaryDirMaxTotalBytes = 10 * 1024 * 1024;

        /// <summary>Порог размера файла (байт) для очистки Chrome-папки.</summary>
        public const long ChromeFileMaxBytes = 1024 * 1024;

        /// <summary>Общий лимит размера (байт) для удаления Chrome-папки.</summary>
        public const long ChromeDirMaxTotalBytes = 5 * 1024 * 1024;

        private static readonly string[] TemporaryExtensions = [".tmp", ".log", ".txt", ".bak"];

        public static bool IsVlc(string appName)
            => !string.IsNullOrEmpty(appName)
               && appName.StartsWith("vlc", StringComparison.OrdinalIgnoreCase);

        public static bool IsSystemApp(string appName)
            => !string.IsNullOrEmpty(appName)
               && (Array.Exists(SystemApps, n => n.Equals(appName, StringComparison.OrdinalIgnoreCase))
                   || appName.Contains("VC++", StringComparison.OrdinalIgnoreCase));

        public static bool IsVcRedist2015_2022(string appName)
            => !string.IsNullOrEmpty(appName)
               && appName.Contains("VC++ 2015-2022", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Нужно ли копировать установщик в папку установки перед запуском
        /// (для проблемных инсталлеров, которым мешает запуск из Temp).
        /// </summary>
        public static bool ShouldCopyInstaller(string appName)
        {
            if (string.IsNullOrEmpty(appName))
                return false;

            return appName.Equals("Opera", StringComparison.OrdinalIgnoreCase)
                || appName.Contains("VC++ 2013", StringComparison.OrdinalIgnoreCase)
                || IsVcRedist2015_2022(appName)
                || appName.Equals("LMMS", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Является ли набор файлов "временным и удаляемым" для системной очистки.
        /// </summary>
        public static bool IsTemporaryFolder(long totalSize, IReadOnlyList<FileSizeInfo> files)
            => totalSize < TemporaryDirMaxTotalBytes
               && files.All(f => f.Length <= TemporaryFileMaxBytes
                                 && IsTemporaryExtension(f.Extension));

        /// <summary>Расширение считается "временным" (.tmp/.log/.txt/.bak), без учёта регистра.</summary>
        public static bool IsTemporaryExtension(string? extension)
            => !string.IsNullOrEmpty(extension)
               && TemporaryExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Является ли набор файлов "временным и удаляемым" для очистки папки Chrome.
        /// </summary>
        public static bool IsChromeTemporaryFolder(long totalSize, IReadOnlyList<long> fileLengths)
            => totalSize < ChromeDirMaxTotalBytes
               && fileLengths.All(len => len <= ChromeFileMaxBytes);

        /// <summary>
        /// Успешный ли код выхода для установщика VC++.
        /// </summary>
        public static bool IsVcRedistSuccessCode(int exitCode)
            => Array.IndexOf(VcRedistSuccessExitCodes, exitCode) >= 0;

        /// <summary>Путь, где установщик остаётся в папке приложения (для удаления).</summary>
        public static string GetInstallerLeftPath(string appDir, string sourcePath)
            => System.IO.Path.Combine(appDir, System.IO.Path.GetFileName(sourcePath));
    }

    /// <summary>Метаданные файла для принятия решений об очистке (без ввода-вывода).</summary>
    internal readonly struct FileSizeInfo(long length, string extension)
    {
        public long Length { get; } = length;

        public string Extension { get; } = extension;
    }
}

