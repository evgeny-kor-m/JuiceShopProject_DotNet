// ==============================================
// Base/BaseTest.cs
// ==============================================

using Allure.Net.Commons;
using JuiceShopProject.Test.Drivers;
using JuiceShopProject.Test.Pages;
using JuiceShopProject.Test.Utilities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Framework;
using OpenQA.Selenium;
using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;
using System;
using System.IO;

namespace JuiceShopProject.Test.Base
{
    /// <summary>
    /// Base class for all test fixtures.
    /// Contains common setup and teardown logic for each test.
    /// Declared abstract to prevent accidental execution by the Test Runner.
    /// </summary>
    [TestFixture]
    public abstract class BaseTest
    {
        protected IWebDriver Driver;
        private string _testLogFilePath;
        private ILogger _originalGlobalLogger;
        protected BasePage BasePageInstance;

        [SetUp]
        public void SetUp()
        {

            // Assign a new temporary logger to the static Log.Logger field
            Log.Logger = CreateLocalTestLoger();

            Log.Information($"TEST SETUP STARTED.");

            // Driver initialization using the Factory, which reads the browser from config
            Driver = WebDriverFactory.CreateDriver();

            // Apply implicit wait timeout from configuration
            Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(ConfigReader.GetImplicitWait());
            Driver.Manage().Window.Maximize();

            Log.Information("Driver initialized successfully.");

            

            // Navigation to the base URL from configuration
            string baseUrl = ConfigReader.GetBaseUrl() ?? "http://localhost:3000";
            Driver.Navigate().GoToUrl(baseUrl);
            Log.Information($"Navigated to: {baseUrl}");

            BasePageInstance = new LoginPage(Driver);
            BasePageInstance.DismissWelcomeBanner();

        }

        [TearDown]
        public void TearDown()
        {
            var outcome = TestContext.CurrentContext.Result.Outcome.Status;
            var testName = TestContext.CurrentContext.Test.FullName;
            
            // Log all failures using the temporary logger
            if ((Driver != null) && outcome == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                AttachScreenshotToAllureReport(testName);
                AttachLogFileToAllureReport(testName);
            }
            Log.Information("TEST TEARDOWN COMPLETED.");
            try
            {
                if (Driver != null)
                {
                    Driver.Quit();
                    Driver.Dispose();
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"Error during driver teardown: {ex.Message}");
            }

            CloseLocalTestLoger();

        }
        private void AttachScreenshotToAllureReport(string testName)
        {
            string? screenshotPath = null;
            try
            {
                string screenshotsDir = ConfigReader.GetScreenshotsPath();
                Directory.CreateDirectory(screenshotsDir);
                string safeTestName = string.Join("_", testName.Split(Path.GetInvalidFileNameChars()));
                string screenshotFilename = $"{safeTestName}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                screenshotPath = Path.Combine(screenshotsDir, screenshotFilename);

                var screenshot = ((ITakesScreenshot)Driver).GetScreenshot();
                screenshot.SaveAsFile(screenshotPath);

                // Attach the screenshot directly, without checking the context
                try
                {
                    // 1. Copy the file to the Allure folder so it stays available after the test finishes
                    // (A GUID keeps the file name unique inside the Allure report)
                    string attachmentFileName = Guid.NewGuid().ToString() + ".png";
                    string attachmentSourcePath = AllureLifecycle.Instance.ResultsDirectory + Path.DirectorySeparatorChar + attachmentFileName;

                    File.Copy(screenshotPath, attachmentSourcePath, true);

                    // 2. Add the attachment info to the current test
                    AllureLifecycle.Instance.UpdateTestCase(testResult =>
                    {
                        testResult.attachments.Add(new Attachment
                        {
                            name = $"Screenshot: {safeTestName}",
                            source = attachmentFileName, // File name Allure will look for in its own folder
                            type = "image/png"
                        });
                    });

                    Log.Information($"Screenshot saved locally and manually attached to Allure: {screenshotPath}");
                }
                catch (Exception allureEx)
                {
                    Log.Warning($"Could not attach screenshot to Allure: {allureEx.Message}");
                }
                Log.Error($"Test {testName} failed. Screenshot saved: {screenshotPath}");
            }
            catch (Exception e)
            {
                Log.Error($"Error while capturing screenshot: {e.Message}");
            }
        }
        private void AttachLogFileToAllureReport(string testName)
        {
            // --- Attach the log file ---
            if (File.Exists(_testLogFilePath))
            {
                try
                {
                    // 1. Copy the log file to the Allure folder
                    string attachmentFileName = Guid.NewGuid().ToString() + ".log";
                    string attachmentSourcePath = AllureLifecycle.Instance.ResultsDirectory + Path.DirectorySeparatorChar + attachmentFileName;
                    File.Copy(_testLogFilePath, attachmentSourcePath, true);

                    // 2. Add the attachment info to the current test
                    AllureLifecycle.Instance.UpdateTestCase(testResult =>
                    {
                        testResult.attachments.Add(new Attachment
                        {
                            name = $"Test Log: {testName}",
                            source = attachmentFileName,
                            type = "text/plain" // MIME type for log files
                        });
                    });

                    Log.Information($"Log file attached to the Allure report.");
                }
                catch (Exception allureEx)
                {
                    Log.Warning($"Could not attach log file to Allure: {allureEx.Message}");
                }
            }
        }
        private ILogger CreateLocalTestLoger()
        {
            // 1. Build a unique log file path for the current test
            var safeTestName = string.Join("_", TestContext.CurrentContext.Test.MethodName.Split(Path.GetInvalidFileNameChars()));
            _testLogFilePath = Path.Combine(ConfigReader.GetLogsPath(), $"{safeTestName}_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            Directory.CreateDirectory(Path.GetDirectoryName(_testLogFilePath));

            // Get the level for the ISOLATED (per-test) log
            if (!Enum.TryParse(ConfigReader.GetTestLogLevel(), true, out LogEventLevel testLogLevel))
            {
                testLogLevel = LogEventLevel.Debug; // Fallback
            }
            _originalGlobalLogger = Log.Logger;

            // 3. Configure a temporary Serilog logger for the current test:
            // Create a new logger that writes to a unique file AND forwards ALL messages to the saved global logger
            var testLogger = new LoggerConfiguration()
                .MinimumLevel.Is(testLogLevel)
                .Enrich.With<TestNameEnricher>() // Add the enricher
                .WriteTo.File(_testLogFilePath,
                              restrictedToMinimumLevel: testLogLevel,
                              //outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                              outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{TestName}] {Message:lj}{NewLine}{Exception}")
                // Forward all logs to the original global logger
                .WriteTo.Logger(_originalGlobalLogger)
                .CreateLogger();

            Log.Information($"(Test) Log Level : {testLogLevel}. Logs writes to: {_testLogFilePath}.");
            return testLogger;
        }
        private void CloseLocalTestLoger()
        {
            // Close and flush the temporary logger
            Log.CloseAndFlush();

            // Restore the original global logger
            if (_originalGlobalLogger != null)
            {
                Log.Logger = _originalGlobalLogger;
            }
            // Delete the temporary file after it has been attached
            try
            {
                if (File.Exists(_testLogFilePath))
                    File.Delete(_testLogFilePath);

            }
            catch (Exception delEx)
            {
                Log.Warning($"Could not delete temporary log file: {delEx.Message}");
            }
        }
    }
}   