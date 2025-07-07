
namespace WindSoftInstaller
{
    internal static class ShortcutHelper
    {
        private static readonly Dictionary<string, string> Map = new()
        {
            ["LMMS"] = "lmms.exe",
            ["HandBrake"] = "HandBrake.exe",
            ["Clementine"] = "Clementine.exe",
            ["ClamWin"] = Path.Combine("bin", "ClamWin.exe"),  // пример вложенной папки
            ["Cryptomator"] = "Cryptomator.exe",
            ["KeePass"] = "KeePass.exe",
            ["UltraDefrag"] = "ufd.gui.exe",
            ["RivaTuner Statistics Server"] = "RTSS.exe"
        };

        // Если для данного имени приложения известно имя exe, возвращает полный путь и имя для ярлыка. Иначе — null.
        public static bool TryGetExeRelativePath(string appName, out string relativePath)
        {
            return Map.TryGetValue(appName, out relativePath!);
        }
    }
}
