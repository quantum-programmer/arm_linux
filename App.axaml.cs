using ARM.ViewModels;
using ARM.Views;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using MsBox.Avalonia;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Runtime.Intrinsics.Arm;

namespace ARM
{
    public partial class App : Application
    {
        private static readonly string MetrologyFileName =
#if VERSION_2B
    "denscalc.dll";
#elif VERSION_2C
    "oildenscalc.dll";
#else
     "default.dll";
#endif

        private static readonly string ValidHash =
#if VERSION_2B
    "C27BD1A545D27B2FAE0A9B81E2AB7CD7";
#elif VERSION_2C
    "99E992D40A2E7FEA5B4C7F3BBE815AC9";   
#else
            "DEFAULT_HASH";
#endif
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override async void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                bool metrologyOk = await CheckMetrologyFile();
                if (!metrologyOk)
                {
                    desktop.Shutdown();
                    return;
                }

                // Отключаем встроенные Avalonia data validation
                BindingPlugins.DataValidators.RemoveAt(0);

                var mainWindow = new MainWindow 
                {
                    DataContext = new MainWindowViewModel(),
                };

#if DEBUG || VERSION_2B || VERSION_2C
                DevToolsExtensions.AttachDevTools(mainWindow);
#endif

                desktop.MainWindow = mainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }
        private async Task<bool> CheckMetrologyFile()
        {
            if (!File.Exists(MetrologyFileName))
            {
                await ShowError( "Файл метрологии не найден!");
                return false;
            }

            var fileHash = GetFileHash(MetrologyFileName);
            if (!string.Equals(fileHash, ValidHash, StringComparison.OrdinalIgnoreCase))
            {
                await ShowError("Ошибка метрологии: файл поврежден или изменен.");
                return false;
            }

            return true;
        }

        private string GetFileHash(string filePath)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            var hashBytes = md5.ComputeHash(stream);
            return BitConverter.ToString(hashBytes).Replace("-", "");
        }

        private async Task ShowError( string message)
        {
            var messageBox = MessageBoxManager
                .GetMessageBoxStandard("Ошибка", message);
            await messageBox.ShowAsync();
        }
    }
}