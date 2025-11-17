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

            // Назначаем новый временный логгер статическому полю Log.Logger
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
            
            // Логируем все ошибки, используя временный логгер
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

                // Простое добавление скриншота без проверки контекста
                try
                {
                    // 1. Копируем файл в папку Allure, чтобы он был доступен после завершения теста
                    // (Используем GUID для уникальности имени файла внутри отчета Allure)
                    string attachmentFileName = Guid.NewGuid().ToString() + ".png";
                    string attachmentSourcePath = AllureLifecycle.Instance.ResultsDirectory + Path.DirectorySeparatorChar + attachmentFileName;

                    File.Copy(screenshotPath, attachmentSourcePath, true);

                    // 2. Добавляем информацию о прикреплении к текущему тесту
                    AllureLifecycle.Instance.UpdateTestCase(testResult =>
                    {
                        testResult.attachments.Add(new Attachment
                        {
                            name = $"Screenshot: {safeTestName}",
                            source = attachmentFileName, // Имя файла, которое Allure будет искать в своей папке
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
            // --- НОВОЕ: Прикрепление Лог-файла ---
            if (File.Exists(_testLogFilePath))
            {
                try
                {
                    // 1. Копируем файл лога в папку Allure
                    string attachmentFileName = Guid.NewGuid().ToString() + ".log";
                    string attachmentSourcePath = AllureLifecycle.Instance.ResultsDirectory + Path.DirectorySeparatorChar + attachmentFileName;
                    File.Copy(_testLogFilePath, attachmentSourcePath, true);

                    // 2. Добавляем информацию о прикреплении к текущему тесту
                    AllureLifecycle.Instance.UpdateTestCase(testResult =>
                    {
                        testResult.attachments.Add(new Attachment
                        {
                            name = $"Test Log: {testName}",
                            source = attachmentFileName,
                            type = "text/plain" // Тип для лог-файлов
                        });
                    });

                    Log.Information($"Лог-файл успешно прикреплен к Allure отчету.");
                }
                catch (Exception allureEx)
                {
                    Log.Warning($"Не удалось прикрепить лог к Allure: {allureEx.Message}");
                }
            }
        }
        private ILogger CreateLocalTestLoger()
        {
            // 1. Создание уникального пути для лог-файла текущего теста
            var safeTestName = string.Join("_", TestContext.CurrentContext.Test.MethodName.Split(Path.GetInvalidFileNameChars()));
            _testLogFilePath = Path.Combine(ConfigReader.GetLogsPath(), $"{safeTestName}_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            Directory.CreateDirectory(Path.GetDirectoryName(_testLogFilePath));

            // Получаем уровень для ИЗОЛИРОВАННОГО лога
            if (!Enum.TryParse(ConfigReader.GetTestLogLevel(), true, out LogEventLevel testLogLevel))
            {
                testLogLevel = LogEventLevel.Debug; // Fallback
            }
            _originalGlobalLogger = Log.Logger;

            // 3. Настройка временного Serilog для текущего теста:
            // Создаем новый логгер, который пишет в уникальный файл И направляет ВСЕ сообщения в сохраненный глобальный логгер
            var testLogger = new LoggerConfiguration()
                .MinimumLevel.Is(testLogLevel)
                .Enrich.With<TestNameEnricher>() // ✅ Добавляем enricher
                .WriteTo.File(_testLogFilePath,
                              restrictedToMinimumLevel: testLogLevel,
                              //outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                              outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{TestName}] {Message:lj}{NewLine}{Exception}")
                // Перенаправляем все логи в исходный глобальный логгер
                .WriteTo.Logger(_originalGlobalLogger)
                .CreateLogger();

            Log.Information($"(Test) Log Level : {testLogLevel}. Logs writes to: {_testLogFilePath}.");
            return testLogger;
        }
        private void CloseLocalTestLoger()
        {
            // Закрываем и очищаем буферы временного логгера
            Log.CloseAndFlush();

            // Восстанавливаем оригинальный глобальный логгер
            if (_originalGlobalLogger != null)
            {
                Log.Logger = _originalGlobalLogger;
            }
            // Удаляем временный файл после прикрепления
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