using IntervalToast.Presentation.ViewModels;
using System;
using System.Windows;

namespace IntervalToast.Presentation.Windows;

/// <summary>
/// Modern MainWindow with dependency injection and MVVM
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        // Setup window behavior
        SetupWindowBehavior();
    }

    /// <summary>
    /// Configures window behavior and events
    /// </summary>
    private void SetupWindowBehavior()
    {
        // Handle window state changes for system tray
        StateChanged += OnWindowStateChanged;

        // Handle window closing
        Closing += OnWindowClosing;
    }

    /// <summary>
    /// Handles window state changes
    /// </summary>
    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            // Hide from taskbar when minimized
            ShowInTaskbar = false;
            Hide();
        }
        else
        {
            ShowInTaskbar = true;
        }
    }

    /// <summary>
    /// Handles window closing
    /// </summary>
    private void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Minimize to tray instead of closing
        e.Cancel = true;
        WindowState = WindowState.Minimized;
    }

    /// <summary>
    /// Shows the window from system tray
    /// </summary>
    public void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }
}