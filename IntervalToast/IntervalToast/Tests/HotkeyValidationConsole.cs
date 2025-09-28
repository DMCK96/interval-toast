using System;
using System.IO;
using System.Threading.Tasks;
using IntervalToast.Domain.Entities;
using IntervalToast.Infrastructure.Services;

namespace IntervalToast.Tests
{
    /// <summary>
    /// Standalone console application to run hotkey validation tests
    /// and output comprehensive results
    /// </summary>
    public class HotkeyValidationConsole
    {
        /// <summary>
        /// Main entry point for validation testing
        /// </summary>
        public static async Task<int> RunValidationMain()
        {
            try
            {
                Console.WriteLine("=== IntervalToast Hotkey System Validation ===");
                Console.WriteLine($"Starting validation at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine();

                // Initialize required components (simulate application environment)
                var notificationManager = NotificationManager.Instance;
                var hotkeyManager = GlobalHotkeyManager.Instance;
                var actionHandler = HotkeyActionHandler.Instance;

                Console.WriteLine("✓ Core components initialized");
                Console.WriteLine("✓ Starting comprehensive validation tests...");
                Console.WriteLine();

                // Run the validation tests
                var report = await HotkeyValidationTest.RunValidationAsync();

                // Output results to console
                Console.WriteLine("=== VALIDATION RESULTS ===");
                foreach (var result in report.Results)
                {
                    Console.WriteLine(result);
                }

                Console.WriteLine();
                Console.WriteLine("=== FINAL SUMMARY ===");
                Console.WriteLine($"Overall Success: {(report.OverallSuccess ? "PASS" : "FAIL")}");
                Console.WriteLine($"Tests Run: {report.TestsRun}");
                Console.WriteLine($"Tests Passed: {report.TestsPassed}");
                Console.WriteLine($"Tests Failed: {report.TestsFailed}");
                Console.WriteLine($"Success Rate: {report.SuccessRate:F1}%");
                Console.WriteLine($"Generated At: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");

                // Save detailed report to file
                var reportPath = Path.Combine(Path.GetTempPath(), "IntervalToast_HotkeyValidation_Report.txt");
                await File.WriteAllTextAsync(reportPath, report.ToString());
                Console.WriteLine();
                Console.WriteLine($"Detailed report saved to: {reportPath}");

                // Clean up
                hotkeyManager?.Dispose();

                return report.OverallSuccess ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CRITICAL ERROR during validation: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return -1;
            }
        }
    }
}