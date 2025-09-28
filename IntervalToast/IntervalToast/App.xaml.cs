using System;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace IntervalToast
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            // Check for validation test command line argument
            if (e.Args.Contains("--validate-hotkeys") || e.Args.Contains("/validate"))
            {
                // Run validation tests in console mode
                await RunValidationMode();
                Current.Shutdown(0);
                return;
            }

            // Normal startup
            base.OnStartup(e);
        }

        private async Task RunValidationMode()
        {
            try
            {
                // Allocate a console for output
                if (AllocConsole())
                {
                    Console.SetOut(new System.IO.StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
                    Console.SetError(new System.IO.StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
                }

                var exitCode = await HotkeyValidationConsole.RunValidationMain();
                Environment.Exit(exitCode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Validation failed with exception: {ex.Message}");
                Environment.Exit(-1);
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
    }

}
