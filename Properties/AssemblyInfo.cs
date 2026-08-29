using System.Runtime.Versioning;

// Проект нацелен на net8.0-windows (WinForms, COM, Registry).
// Так как GenerateAssemblyInfo=false, атрибут платформы не генерируется SDK автоматически,
// поэтому объявляем его явно, чтобы подавить ложные CA1416 и правильно пометить сборку.
[assembly: SupportedOSPlatform("windows")]
