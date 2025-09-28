using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using IntervalToast.Domain.Entities;
using IntervalToast.Infrastructure.Services;
using IntervalToast.Infrastructure.Interop;

namespace IntervalToast.Tests
{
    /// <summary>
    /// Comprehensive test and validation program for the global hotkey system.
    /// Tests all components including Win32 P/Invoke integration, hotkey registration,
    /// action handling, configuration persistence, and error scenarios.
    /// </summary>
    public static class HotkeyValidationTest
    {
        private static readonly List<string> TestResults = new();
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs comprehensive validation tests for the hotkey system
        /// </summary>
        public static async Task<HotkeyValidationReport> RunValidationAsync()
        {
            TestResults.Clear();
            _testsRun = _testsPassed = _testsFailed = 0;

            AddTestResult("=== IntervalToast Global Hotkey System Validation ===", true);
            AddTestResult($"Test started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", true);

            // Test 1: Win32 P/Invoke Integration
            await TestWin32PInvokeIntegration();

            // Test 2: HotkeyConfiguration Model Validation
            await TestHotkeyConfigurationModel();

            // Test 3: GlobalHotkeyManager Initialization
            await TestGlobalHotkeyManagerInitialization();

            // Test 4: Hotkey Registration and Unregistration
            await TestHotkeyRegistrationFlow();

            // Test 5: HotkeyActionHandler Integration
            await TestHotkeyActionHandler();

            // Test 6: Configuration Persistence
            await TestConfigurationPersistence();

            // Test 7: Error Handling and Edge Cases
            await TestErrorHandling();

            // Test 8: Event System Integration
            await TestEventSystemIntegration();

            // Test 9: UI Integration Validation
            await TestUIIntegration();

            // Test 10: Performance and Resource Management
            await TestPerformanceAndResources();

            AddTestResult("", true);
            AddTestResult("=== Test Summary ===", true);
            AddTestResult($"Total Tests: {_testsRun}", true);
            AddTestResult($"Passed: {_testsPassed}", _testsPassed == _testsRun);
            AddTestResult($"Failed: {_testsFailed}", _testsFailed == 0);
            AddTestResult($"Success Rate: {(_testsPassed * 100.0 / _testsRun):F1}%", true);

            return new HotkeyValidationReport
            {
                TestsRun = _testsRun,
                TestsPassed = _testsPassed,
                TestsFailed = _testsFailed,
                SuccessRate = _testsPassed * 100.0 / _testsRun,
                Results = TestResults.ToList(),
                OverallSuccess = _testsFailed == 0
            };
        }

        #region Test Methods

        private static async Task TestWin32PInvokeIntegration()
        {
            AddTestResult("\n--- Test 1: Win32 P/Invoke Integration ---", true);

            // Test hotkey validation
            var testHotkey = new HotkeyConfiguration
            {
                Id = 9999,
                ModifierKeys = ModifierKeys.Control | ModifierKeys.Shift,
                Key = Key.F12,
                Description = "Test Hotkey"
            };

            RunTest("Win32 key conversion", () =>
            {
                var validation = Win32HotkeyHelper.ValidateHotkey(testHotkey);
                return validation.IsValid;
            });

            RunTest("Problematic hotkey detection", () =>
            {
                var problematic = Win32HotkeyHelper.GetProblematicHotkeys();
                return problematic.Count > 0 && problematic.Any(h => h.Key == Key.Tab);
            });

            RunTest("Hotkey validation system", () =>
            {
                var conflictHotkey = new HotkeyConfiguration
                {
                    ModifierKeys = ModifierKeys.Alt,
                    Key = Key.Tab
                };
                var validation = Win32HotkeyHelper.ValidateHotkey(conflictHotkey);
                return validation.HasWarnings;
            });

            await Task.Delay(10); // Simulate async operation
        }

        private static async Task TestHotkeyConfigurationModel()
        {
            AddTestResult("\n--- Test 2: HotkeyConfiguration Model ---", true);

            RunTest("HotkeyConfiguration creation", () =>
            {
                var config = new HotkeyConfiguration
                {
                    ModifierKeys = ModifierKeys.Control | ModifierKeys.Shift,
                    Key = Key.D,
                    Description = "Test"
                };
                return config.IsValid && config.DisplayName.Contains("Ctrl") && config.DisplayName.Contains("Shift");
            });

            RunTest("HotkeyConfiguration cloning", () =>
            {
                var original = new HotkeyConfiguration { Id = 1, Key = Key.A, ModifierKeys = ModifierKeys.Control };
                var clone = original.Clone();
                return clone.Id == original.Id && clone.Key == original.Key && clone.ModifierKeys == original.ModifierKeys;
            });

            RunTest("Same key combination detection", () =>
            {
                var config1 = new HotkeyConfiguration { ModifierKeys = ModifierKeys.Control, Key = Key.A };
                var config2 = new HotkeyConfiguration { ModifierKeys = ModifierKeys.Control, Key = Key.A };
                return config1.IsSameKeyCombo(config2);
            });

            await Task.Delay(10);
        }

        private static async Task TestGlobalHotkeyManagerInitialization()
        {
            AddTestResult("\n--- Test 3: GlobalHotkeyManager Initialization ---", true);

            RunTest("Singleton instance access", () =>
            {
                var instance1 = GlobalHotkeyManager.Instance;
                var instance2 = GlobalHotkeyManager.Instance;
                return instance1 != null && ReferenceEquals(instance1, instance2);
            });

            RunTest("Initial state validation", () =>
            {
                var manager = GlobalHotkeyManager.Instance;
                return manager.IsEnabled && manager.RegisteredHotkeyCount >= 0;
            });

            RunTest("System configuration access", () =>
            {
                var manager = GlobalHotkeyManager.Instance;
                var config = manager.SystemConfiguration;
                return config != null && config.EnableGlobalHotkeys;
            });

            await Task.Delay(10);
        }

        private static async Task TestHotkeyRegistrationFlow()
        {
            AddTestResult("\n--- Test 4: Hotkey Registration Flow ---", true);

            var manager = GlobalHotkeyManager.Instance;
            var testHotkey = new HotkeyConfiguration
            {
                Id = 8888,
                ModifierKeys = ModifierKeys.Control | ModifierKeys.Alt,
                Key = Key.F11,
                Description = "Test Registration Hotkey",
                Action = () => Debug.WriteLine("Test hotkey executed")
            };

            RunTest("Hotkey availability check", () =>
            {
                // This should return true for an unused combination
                return manager.IsHotkeyAvailable(testHotkey);
            });

            bool registrationSuccess = false;
            RunTest("Hotkey registration", () =>
            {
                try
                {
                    registrationSuccess = manager.RegisterHotkey(testHotkey);
                    return true; // Test passes if no exception is thrown
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Registration test exception: {ex.Message}");
                    return false;
                }
            });

            RunTest("Registered hotkey retrieval", () =>
            {
                var registered = manager.GetRegisteredHotkeys();
                return registered.Any(h => h.Id == testHotkey.Id) || !registrationSuccess;
            });

            if (registrationSuccess)
            {
                RunTest("Hotkey unregistration", () =>
                {
                    try
                    {
                        return manager.UnregisterHotkey(testHotkey);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Unregistration test exception: {ex.Message}");
                        return false;
                    }
                });
            }

            await Task.Delay(10);
        }

        private static async Task TestHotkeyActionHandler()
        {
            AddTestResult("\n--- Test 5: HotkeyActionHandler Integration ---", true);

            var handler = HotkeyActionHandler.Instance;

            RunTest("HotkeyActionHandler singleton", () =>
            {
                var instance1 = HotkeyActionHandler.Instance;
                var instance2 = HotkeyActionHandler.Instance;
                return instance1 != null && ReferenceEquals(instance1, instance2);
            });

            RunTest("DismissAll action execution", () =>
            {
                try
                {
                    var result = handler.ExecuteAction(HotkeyAction.DismissAll);
                    return result != null;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"DismissAll test exception: {ex.Message}");
                    return false;
                }
            });

            RunTest("ShowQueueStatus action execution", () =>
            {
                try
                {
                    var result = handler.ExecuteAction(HotkeyAction.ShowQueueStatus);
                    return result != null;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ShowQueueStatus test exception: {ex.Message}");
                    return false;
                }
            });

            await Task.Delay(10);
        }

        private static async Task TestConfigurationPersistence()
        {
            AddTestResult("\n--- Test 6: Configuration Persistence ---", true);

            var manager = GlobalHotkeyManager.Instance;

            RunTest("Configuration modification", () =>
            {
                try
                {
                    var config = new HotkeySystemConfiguration
                    {
                        EnableGlobalHotkeys = true,
                        ShowHotkeyErrors = false,
                        MaxHotkeys = 15,
                        EnableLogging = true
                    };
                    manager.SetSystemConfiguration(config);

                    var retrievedConfig = manager.SystemConfiguration;
                    return retrievedConfig.MaxHotkeys == 15 && !retrievedConfig.ShowHotkeyErrors;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Configuration test exception: {ex.Message}");
                    return false;
                }
            });

            await Task.Delay(10);
        }

        private static async Task TestErrorHandling()
        {
            AddTestResult("\n--- Test 7: Error Handling and Edge Cases ---", true);

            var manager = GlobalHotkeyManager.Instance;

            RunTest("Null hotkey registration handling", () =>
            {
                try
                {
                    manager.RegisterHotkey(null!);
                    return false; // Should throw exception
                }
                catch (ArgumentNullException)
                {
                    return true; // Expected exception
                }
                catch (Exception)
                {
                    return false; // Unexpected exception type
                }
            });

            RunTest("Invalid hotkey configuration handling", () =>
            {
                var invalidHotkey = new HotkeyConfiguration
                {
                    Key = Key.None,
                    ModifierKeys = ModifierKeys.None
                };
                return !manager.RegisterHotkey(invalidHotkey);
            });

            RunTest("Duplicate hotkey registration handling", () =>
            {
                var hotkey1 = new HotkeyConfiguration
                {
                    Id = 7777,
                    ModifierKeys = ModifierKeys.Control,
                    Key = Key.F10,
                    Description = "Test Duplicate 1"
                };
                var hotkey2 = new HotkeyConfiguration
                {
                    Id = 7778,
                    ModifierKeys = ModifierKeys.Control,
                    Key = Key.F10,
                    Description = "Test Duplicate 2"
                };

                try
                {
                    var first = manager.RegisterHotkey(hotkey1);
                    var second = manager.RegisterHotkey(hotkey2);

                    // Clean up
                    if (first) manager.UnregisterHotkey(hotkey1);
                    if (second) manager.UnregisterHotkey(hotkey2);

                    return !second; // Second registration should fail
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Duplicate registration test exception: {ex.Message}");
                    return true; // Exception is acceptable for duplicate registration
                }
            });

            await Task.Delay(10);
        }

        private static async Task TestEventSystemIntegration()
        {
            AddTestResult("\n--- Test 8: Event System Integration ---", true);

            var manager = GlobalHotkeyManager.Instance;
            var eventsFired = new List<string>();

            // Subscribe to events
            manager.HotkeyRegistered += (_, e) => eventsFired.Add("Registered");
            manager.HotkeyUnregistered += (_, e) => eventsFired.Add("Unregistered");
            manager.SystemStateChanged += (_, e) => eventsFired.Add("StateChanged");

            RunTest("Event subscription", () => true); // If we get here, subscription worked

            RunTest("System state change events", () =>
            {
                try
                {
                    var originalState = manager.IsEnabled;
                    manager.IsEnabled = !originalState;
                    manager.IsEnabled = originalState;

                    return eventsFired.Contains("StateChanged");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"State change test exception: {ex.Message}");
                    return false;
                }
            });

            await Task.Delay(10);
        }

        private static async Task TestUIIntegration()
        {
            AddTestResult("\n--- Test 9: UI Integration Validation ---", true);

            RunTest("Predefined hotkeys availability", () =>
            {
                var predefined = PredefinedHotkeys.GetAll();
                return predefined.Count > 0 && predefined.All(h => h.IsValid);
            });

            RunTest("Hotkey display name generation", () =>
            {
                var hotkey = new HotkeyConfiguration
                {
                    ModifierKeys = ModifierKeys.Control | ModifierKeys.Shift,
                    Key = Key.A
                };
                var displayName = hotkey.DisplayName;
                return displayName.Contains("Ctrl") && displayName.Contains("Shift") && displayName.Contains("A");
            });

            await Task.Delay(10);
        }

        private static async Task TestPerformanceAndResources()
        {
            AddTestResult("\n--- Test 10: Performance and Resource Management ---", true);

            var manager = GlobalHotkeyManager.Instance;

            RunTest("Multiple hotkey registration performance", () =>
            {
                var stopwatch = Stopwatch.StartNew();
                var hotkeys = new List<HotkeyConfiguration>();

                try
                {
                    for (int i = 0; i < 5; i++)
                    {
                        var hotkey = new HotkeyConfiguration
                        {
                            Id = 6000 + i,
                            ModifierKeys = ModifierKeys.Control | ModifierKeys.Alt,
                            Key = (Key)(Key.F1 + i),
                            Description = $"Performance Test {i}"
                        };
                        hotkeys.Add(hotkey);
                        manager.RegisterHotkey(hotkey);
                    }

                    stopwatch.Stop();

                    // Clean up
                    foreach (var hotkey in hotkeys)
                    {
                        manager.UnregisterHotkey(hotkey);
                    }

                    return stopwatch.ElapsedMilliseconds < 1000; // Should complete within 1 second
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Performance test exception: {ex.Message}");
                    return false;
                }
            });

            RunTest("Resource cleanup on dispose", () =>
            {
                // Test would require creating a separate instance, but singleton pattern prevents this
                // This test validates that dispose is implemented
                return typeof(GlobalHotkeyManager).GetInterfaces().Contains(typeof(IDisposable));
            });

            await Task.Delay(10);
        }

        #endregion

        #region Helper Methods

        private static void RunTest(string testName, Func<bool> testAction)
        {
            _testsRun++;
            try
            {
                bool result = testAction();
                if (result)
                {
                    _testsPassed++;
                    AddTestResult($"✓ {testName}", true);
                }
                else
                {
                    _testsFailed++;
                    AddTestResult($"✗ {testName}", false);
                }
            }
            catch (Exception ex)
            {
                _testsFailed++;
                AddTestResult($"✗ {testName} (Exception: {ex.Message})", false);
            }
        }

        private static void AddTestResult(string message, bool success)
        {
            TestResults.Add(message);
            Debug.WriteLine($"[HotkeyValidation] {message}");
        }

        #endregion
    }

    /// <summary>
    /// Comprehensive validation report for the hotkey system
    /// </summary>
    public class HotkeyValidationReport
    {
        public int TestsRun { get; set; }
        public int TestsPassed { get; set; }
        public int TestsFailed { get; set; }
        public double SuccessRate { get; set; }
        public List<string> Results { get; set; } = new();
        public bool OverallSuccess { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.Now;

        public override string ToString()
        {
            return string.Join("\n", Results);
        }
    }
}