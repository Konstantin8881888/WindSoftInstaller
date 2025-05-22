using System;
using System.Runtime.Versioning;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions; // для NullLogger
using WindSoftInstaller;

namespace WindSoftInstaller
{
    [SupportedOSPlatform("windows")]
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Показываем EULA
            using (var eula = new EulaForm())
            {
                eula.ShowDialog();
                if (!eula.Accepted)
                    return;  // пользователь отказался — выходим
            }

            // Создаём «пустой» логгер, который ничего не делает
            ILogger<Form1> logger = NullLogger<Form1>.Instance;

            Application.Run(new Form1(logger));
        }
    }
}
