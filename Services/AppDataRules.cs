using System;
using System.Collections.Generic;

namespace WindSoftInstaller.Services
{
    /// <summary>
    /// Чистые правила формирования данных приложений (размер, локализованные имена,
    /// языковая фильтрация строк параметров). Не зависит от файловой системы и UI.
    /// </summary>
    internal static class AppDataRules
    {
        /// <summary>Количество байт в одном мегабайте.</summary>
        public const double BytesPerMb = 1024.0 * 1024.0;

        /// <summary>Переводит размер в байтах в мегабайты (с округлением до 2 знаков).</summary>
        public static double ToMegaBytes(long sizeInBytes)
            => Math.Round(sizeInBytes / BytesPerMb, 2);

        /// <summary>
        /// Выбирает имя файла в зависимости от локали: для "ru" — русское, иначе — английское.
        /// </summary>
        public static string GetLocalizedFileName(string? currentLang, string baseNameEn, string baseNameRu)
            => string.Equals(currentLang, "ru", StringComparison.OrdinalIgnoreCase) ? baseNameRu : baseNameEn;

        /// <summary>
        /// Удаляет ключ "param.Language" из параметров приложения, если язык не русский.
        /// Возвращает true, если было удаление; иначе false.
        /// </summary>
        public static bool RemoveLanguageParamIfNotRussian(string? currentLang, IDictionary<string, string> parameters)
        {
            if (parameters == null || string.Equals(currentLang, "ru", StringComparison.OrdinalIgnoreCase))
                return false;

            return parameters.Remove("param.Language");
        }
    }
}
