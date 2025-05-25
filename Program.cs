using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Serilog;

namespace WindSoftInstaller
{
    [SupportedOSPlatform("windows")]
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // Настройка Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("Logs/installer.log",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}")
                .CreateLogger();

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                using (var eula = new EulaForm())
                {
                    if (eula.ShowDialog() != DialogResult.OK)
                        return;
                }

                // Создаем фабрику логгеров с Serilog
                using var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddSerilog();
                });

                var logger = loggerFactory.CreateLogger<Form1>();

                Application.Run(new Form1(logger));
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Произошла критическая ошибка");
                MessageBox.Show($"Fatal error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}