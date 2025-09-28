using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace IntervalToast
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// Phase 5 implementation with comprehensive configuration system integration
    /// </summary>
    public partial class App : System.Windows.Application
    {
        #region Fields

        private SettingsManager? _settingsManager;
        private bool _startMinimized = false;

        #endregion

        #region Application Lifecycle

        /// <summary>
        /// Handles application startup with Phase 5 configuration system
        /// </summary>
        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Check for command line arguments
                if (e.Args.Contains("--validate-hotkeys") || e.Args.Contains("/validate"))
                {
                    // Run validation tests in console mode
                    await RunValidationMode();
                    Current.Shutdown(0);
                    return;
                }

                if (e.Args.Contains("--minimized") || e.Args.Contains("/m"))
                {
                    _startMinimized = true;
                }

                // Initialize settings manager early
                _settingsManager = new SettingsManager();
                await _settingsManager.LoadSettingsAsync();

                // Check if we should start minimized based on settings
                var preferences = _settingsManager.CurrentSettings.ApplicationPreferences;
                if (!preferences.ShowConfigurationOnStartup || _startMinimized)
                {
                    _startMinimized = true;
                }

                // Set up exception handling
                SetupExceptionHandling();

                // Continue with normal startup
                base.OnStartup(e);

                Debug.WriteLine("IntervalToast Phase 5 application started successfully");
            }
            catch (Exception ex)
            {
                HandleStartupError(ex);
            }
        }

        /// <summary>
        /// Handles main window creation and initialization
        /// </summary>
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            // Handle startup minimization after main window is created
            if (_startMinimized && MainWindow != null)
            {
                MainWindow.WindowState = WindowState.Minimized;
                _startMinimized = false; // Only do this once
            }
        }

        /// <summary>
        /// Handles application shutdown cleanup
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                // Cleanup managers
                _settingsManager?.SaveSettings();

                Debug.WriteLine("IntervalToast application shutdown completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during application shutdown: {ex.Message}");
            }

            base.OnExit(e);
        }

        #endregion

        #region Exception Handling

        /// <summary>
        /// Sets up global exception handling for the application
        /// </summary>
        private void SetupExceptionHandling()
        {
            // Handle unhandled exceptions on the UI thread
            DispatcherUnhandledException += (sender, e) =>
            {
                HandleUnhandledException(e.Exception, "UI Thread");
                e.Handled = true; // Prevent application crash
            };

            // Handle unhandled exceptions on background threads
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                HandleUnhandledException((Exception)e.ExceptionObject, "Background Thread");
            };

            // Handle task exceptions
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                HandleUnhandledException(e.Exception, "Task");
                e.SetObserved(); // Prevent application crash
            };
        }

        /// <summary>
        /// Handles unhandled exceptions with logging and user notification
        /// </summary>
        private void HandleUnhandledException(Exception ex, string source)
        {
            try
            {
                var errorMessage = $"Unhandled exception in {source}: {ex.Message}";
                Debug.WriteLine(errorMessage);
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                // Show error notification if notification system is available
                try
                {
                    var notificationManager = NotificationManager.Instance;
                    var status = notificationManager?.GetEnhancedQueueStatus();
                    if (status?.SystemEnabled == true)
                    {
                        var errorNotification = NotificationData.CreateError(
                            "Application Error",
                            "An unexpected error occurred. Please check the logs for details."
                        );
                        notificationManager.ShowNotification(errorNotification);
                    }
                }
                catch
                {
                    // If notification system fails, show message box as fallback
                    System.Windows.MessageBox.Show(
                        $"An unexpected error occurred:\n\n{ex.Message}\n\nThe application will continue running.",
                        "IntervalToast Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }
            catch (Exception handlerEx)
            {
                Debug.WriteLine($"Error in exception handler: {handlerEx.Message}");
            }
        }

        /// <summary>
        /// Handles startup errors with user-friendly messaging
        /// </summary>
        private void HandleStartupError(Exception ex)
        {
            var errorMessage = $"Failed to start IntervalToast:\n\n{ex.Message}\n\nThe application will now exit.";

            System.Windows.MessageBox.Show(
                errorMessage,
                "IntervalToast Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );

            Debug.WriteLine($"Startup error: {ex}");
            Current.Shutdown(1);
        }

        #endregion

        #region Validation Mode

        /// <summary>
        /// Runs the application in validation mode for testing hotkeys
        /// </summary>
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

                Console.WriteLine("IntervalToast Hotkey Validation Mode");
                Console.WriteLine("====================================");

                var exitCode = await HotkeyValidationConsole.RunValidationMain();
                Environment.Exit(exitCode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Validation failed with exception: {ex.Message}");
                Environment.Exit(-1);
            }
        }

        /// <summary>
        /// Allocates a console window for validation mode
        /// </summary>
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the current settings manager instance
        /// </summary>
        public SettingsManager? SettingsManager => _settingsManager;

        /// <summary>
        /// Restarts the application with optional command line arguments
        /// </summary>
        public void RestartApplication(params string[] args)
        {
            try
            {
                var executablePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(executablePath))
                {
                    var arguments = string.Join(" ", args);
                    Process.Start(executablePath, arguments);
                }

                Shutdown(0);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to restart application: {ex.Message}");
                System.Windows.MessageBox.Show(
                    "Failed to restart the application. Please restart manually.",
                    "Restart Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }

        #endregion
    }
}
