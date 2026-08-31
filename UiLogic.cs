using System;
using System.Collections.Generic;
using System.Linq;

namespace WindSoftInstaller
{
    /// <summary>
    /// Чистая логика интерфейса (скрытие, зависимости, размер, иконки, предусловия).
    /// Не содержит ввода-вывода, грида и диалогов — полностью тестируемо.
    /// </summary>
    internal static class UiLogic
    {
        /// <summary>Приложения, скрытые из грида (системные, ставятся автоматически).</summary>
        public static readonly string[] HiddenApps =
        {
            "Microsoft VC++ 2015-2022 Redistributable (x64)",
            "Microsoft VC++ 2015-2022 Redistributable (x86)",
            "VC++ 2013 Redistributable (x86)",
            "VC++ 2013 Redistributable (x64)"
        };

        /// <summary>Зависимости приложений (имя → обязательные пререквизиты).</summary>
        public static readonly IReadOnlyDictionary<string, string[]> Dependencies =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Marble"] = new[]
                {
                    "VC++ 2013 Redistributable (x86)",
                    "VC++ 2013 Redistributable (x64)"
                },
                ["MSI Afterburner"] = new[]
                {
                    "Microsoft VC++ 2015-2022 Redistributable (x86)",
                    "Microsoft VC++ 2015-2022 Redistributable (x64)"
                },
                ["XMedia Recode"] = new[]
                {
                    "Microsoft VC++ 2015-2022 Redistributable (x86)",
                    "Microsoft VC++ 2015-2022 Redistributable (x64)"
                },
                ["RivaTuner Statistics Server"] = new[]
                {
                    "Microsoft VC++ 2015-2022 Redistributable (x86)",
                    "Microsoft VC++ 2015-2022 Redistributable (x64)"
                }
            };

        /// <summary>Приложения, после установки которых требуется пакет VC++ 2013.</summary>
        private static readonly string[] Vc2013RequiringApps = { "Marble" };

        /// <summary>Приложения, после установки которых требуется пакет VC++ 2015-2022.</summary>
        private static readonly string[] Vc2015RequiringApps =
        {
            "MSI Afterburner",
            "RivaTuner Statistics Server",
            "XMedia Recode"
        };

        public static bool IsHidden(string? appName)
            => !string.IsNullOrEmpty(appName)
               && HiddenApps.Contains(appName, StringComparer.OrdinalIgnoreCase);

        public static bool HasDependencies(string? appName)
            => !string.IsNullOrEmpty(appName)
               && Dependencies.ContainsKey(appName);

        /// <summary>Возвращает пререквизиты приложения (или пустой массив).</summary>
        public static string[] GetDependencies(string? appName)
            => !string.IsNullOrEmpty(appName) && Dependencies.TryGetValue(appName, out var deps)
                ? deps!
                : Array.Empty<string>();

        /// <summary>Имя файла иконки для установщика (ExecutablePath → .ico).</summary>
        public static string GetIconFileName(string? executablePath)
            => System.IO.Path.ChangeExtension(executablePath, ".ico") ?? ".ico";

        /// <summary>Путь к иконке в папке Icons относительно каталога запуска.</summary>
        public static string GetIconPath(string iconsFolder, string? executablePath)
            => System.IO.Path.Combine(iconsFolder, GetIconFileName(executablePath));

        /// <summary>Сумма размеров выбранных приложений (SizeMB).</summary>
        public static double SumSelectedSizes(IEnumerable<double> selectedSizes)
            => selectedSizes?.Sum() ?? 0.0;

        /// <summary>
        /// Дополняет список выбранных приложений недостающими пререквизитами (VC++),
        /// если выбраны Marble / MSI Afterburner / RivaTuner / XMedia Recode.
        /// Возвращает новый упорядоченный список; пререквизиты вставляются в начало.
        /// </summary>
        public static List<InstallableApp?> AddMissingPrerequisites(
            IEnumerable<InstallableApp?> checkedApps,
            IReadOnlyDictionary<string, InstallableApp> lookup)
        {
            var result = checkedApps.ToList();
            if (result.Count == 0 || lookup == null)
                return result;

            var selectedNames = result
                .Where(a => a != null)
                .Select(a => a!.Name)
                .ToList();

            var allRequired = new List<string>();

            if (selectedNames.Any(n => Vc2013RequiringApps.Contains(n, StringComparer.OrdinalIgnoreCase)))
            {
                allRequired.Add("VC++ 2013 Redistributable (x86)");
                allRequired.Add("VC++ 2013 Redistributable (x64)");
            }

            if (selectedNames.Any(n => Vc2015RequiringApps.Contains(n, StringComparer.OrdinalIgnoreCase)))
            {
                allRequired.Add("Microsoft VC++ 2015-2022 Redistributable (x86)");
                allRequired.Add("Microsoft VC++ 2015-2022 Redistributable (x64)");
            }

            // Вставляем недостающие пререквизиты в начало (x86 перед x64 внутри группы).
            for (int i = allRequired.Count - 1; i >= 0; i--)
            {
                string name = allRequired[i];
                if (lookup.TryGetValue(name, out var prereq) && !result.Contains(prereq))
                    result.Insert(0, prereq);
            }

            return result;
        }
    }
}
