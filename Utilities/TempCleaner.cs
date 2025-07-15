using Microsoft.Extensions.Logging;

namespace WindSoftInstaller.Utilities
{
    public static class TempCleaner
    {
        public static void Cleanup(string installPath, ILogger logger)
        {
            string tempRoot = Path.Combine(installPath, "Temp");
            if (!Directory.Exists(tempRoot)) return;

            logger.LogWarning("Обнаружены остаточные данные в папке Temp {tempRoot}", tempRoot);
            foreach (var dir in Directory.GetDirectories(tempRoot, "WSI_*", SearchOption.TopDirectoryOnly))
                TryDelete(dir, logger, $"Временная папка удалена: {dir}");

            if (!Directory.EnumerateFileSystemEntries(tempRoot).Any())
                TryDelete(tempRoot, logger, $"Родительская временная папка удалена: {tempRoot}", recursive: false);
        }

        private static void TryDelete(string path, ILogger logger, string successMessage, bool recursive = true)
        {
            try
            {
                Directory.Delete(path, recursive);
                logger.LogDebug(successMessage);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Не удалось удалить папку {Path}", path);
            }
        }
    }
}