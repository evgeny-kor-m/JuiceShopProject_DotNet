// ==============================================
// Base/BaseTest.cs
// ==============================================
using Allure.Commons;
using JuiceShopProject.Test.Drivers;
using JuiceShopProject.Test.Utilities;
using OpenQA.Selenium;
using Serilog;
using System;
using System.IO;
using System.Runtime.CompilerServices;

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

        [SetUp]
        public void SetUp()
        {
            Log.Information("TEST SETUP STARTED.");

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
            Log.Information($" Тест: Allure Results Directory: {AllureLifecycle.Instance.ResultsDirectory}");
        }

        [TearDown]
        public void TearDown()
        {
            var outcome = TestContext.CurrentContext.Result.Outcome.Status;
            var testName = TestContext.CurrentContext.Test.MethodName;

            if ((Driver != null) && outcome == NUnit.Framework.Interfaces.TestStatus.Failed)
            {
                try
                {
                    string screenshotsDir = ConfigReader.GetScreenshotsPath();
                    Directory.CreateDirectory(screenshotsDir);
                    string screenshotPath = Path.Combine(screenshotsDir, $"{testName}_{DateTime.Now:yyyyMMdd_HHmmss}.png");

                    var screenshot = ((ITakesScreenshot)Driver).GetScreenshot();
                    screenshot.SaveAsFile(screenshotPath);

                    // Простое добавление скриншота без проверки контекста
                    try
                    {
                        byte[] screenshotBytes = File.ReadAllBytes(screenshotPath);
                        AllureLifecycle.Instance.AddAttachment(
                            name: $"Screenshot_{testName}",
                            type: "image/png",
                            content: screenshotBytes,
                            fileExtension: ".png"
                        );
                        Log.Information($"Screenshot attached to Allure report.");
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

            try
            {
                if (Driver != null)
                {
                    Driver.Quit();
                    Driver.Dispose();
                }
            }
            catch { /* Игнорировать ошибки при закрытии */ }

            Log.Information("TEST TEARDOWN COMPLETED.");
        }

    }
}   