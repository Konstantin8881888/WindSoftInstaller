using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using WindSoftInstaller.Utilities;
using Xunit;

namespace WindSoftInstaller.Tests
{
    public class TempCleanerTests
    {
        private sealed class StubLogger : ILogger
        {
            public readonly List<string> Messages = new();
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                => Messages.Add(formatter(state, exception));
        }

        [Fact]
        public void Cleanup_DeletesWsiTempFolders()
        {
            string root = Path.Combine(Path.GetTempPath(), "TWSTest_" + Guid.NewGuid().ToString("N"));
            string temp = Path.Combine(root, "Temp");
            var wsiFolder = Path.Combine(temp, "WSI_123");
            Directory.CreateDirectory(wsiFolder);
            var logger = new StubLogger();

            try
            {
                TempCleaner.Cleanup(root, logger);
                Assert.False(Directory.Exists(wsiFolder), "WSI_* папка должна быть удалена");
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Fact]
        public void Cleanup_RemovesParentTemp_WhenEmptyAfter()
        {
            string root = Path.Combine(Path.GetTempPath(), "TWSTest_" + Guid.NewGuid().ToString("N"));
            string temp = Path.Combine(root, "Temp");
            var wsiFolder = Path.Combine(temp, "WSI_1");
            Directory.CreateDirectory(wsiFolder);
            var logger = new StubLogger();

            try
            {
                TempCleaner.Cleanup(root, logger);
                Assert.False(Directory.Exists(wsiFolder));
                Assert.False(Directory.Exists(temp), "Родительская Temp-папка должна быть удалена, если стала пустой");
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Fact]
        public void Cleanup_KeepsParentTemp_WhenOtherFilesRemain()
        {
            string root = Path.Combine(Path.GetTempPath(), "TWSTest_" + Guid.NewGuid().ToString("N"));
            string temp = Path.Combine(root, "Temp");
            Directory.CreateDirectory(temp);
            var wsiFolder = Path.Combine(temp, "WSI_x");
            Directory.CreateDirectory(wsiFolder);
            File.WriteAllText(Path.Combine(temp, "keep.txt"), "x");
            var logger = new StubLogger();

            try
            {
                TempCleaner.Cleanup(root, logger);
                Assert.False(Directory.Exists(wsiFolder));
                Assert.True(Directory.Exists(temp), "Temp с остатками файлов не должен удаляться");
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Fact]
        public void Cleanup_DoesNothing_WhenNoTempFolder()
        {
            string root = Path.Combine(Path.GetTempPath(), "TWSTest_" + Guid.NewGuid().ToString("N"));
            var logger = new StubLogger();

            try
            {
                TempCleaner.Cleanup(root, logger); // нет папки Temp - ничего не происходит
                Assert.False(Directory.Exists(Path.Combine(root, "Temp")));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Fact]
        public void Cleanup_LogsWarning_WhenTempExists()
        {
            string root = Path.Combine(Path.GetTempPath(), "TWSTest_" + Guid.NewGuid().ToString("N"));
            string temp = Path.Combine(root, "Temp");
            Directory.CreateDirectory(temp);
            var logger = new StubLogger();

            try
            {
                TempCleaner.Cleanup(root, logger);
                Assert.Contains(logger.Messages, m => m.Contains("остаточные данные"));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Fact]
        public void Cleanup_LogsWarning_WhenDeleteFails()
        {
            string root = Path.Combine(Path.GetTempPath(), "TWSTest_" + Guid.NewGuid().ToString("N"));
            string temp = Path.Combine(root, "Temp");
            var wsiFolder = Path.Combine(temp, "WSI_locked");
            Directory.CreateDirectory(wsiFolder);
            string lockedFile = Path.Combine(wsiFolder, "data.bin");
            File.WriteAllText(lockedFile, "occupied");

            var logger = new StubLogger();
            FileStream? fs = null;
            try
            {
                // Держим файл открытым, чтобы Directory.Delete не смог удалить папку.
                fs = new FileStream(lockedFile, FileMode.Open, FileAccess.Read, FileShare.None);

                TempCleaner.Cleanup(root, logger);

                Assert.True(Directory.Exists(wsiFolder), "папка с занятым файлом должна сохраниться");
                Assert.Contains(logger.Messages, m => m.Contains("Не удалось удалить папку"));
            }
            finally
            {
                fs?.Dispose();
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }
    }
}
