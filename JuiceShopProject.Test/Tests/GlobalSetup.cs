using JuiceShopProject.Test.Utilities;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Debugging;
using NUnit.Framework;
using System;
using System.IO;

[SetUpFixture]
public class GlobalSetup
{
    private static string allureResultsPath;

    [OneTimeSetUp]
    public void RunBeforeAnyTests()
    {
        TestContext.Progress.WriteLine("\n\n=== GLOBAL SETUP STARTED ===");
        SelfLog.Enable(Console.Error);

        // Создание директорий
        string projectRoot = ConfigReader.GetProjectRoot();
        string screenshotsDir = ConfigReader.GetScreenshotsPath();
        string logsDir = ConfigReader.GetLogsPath();

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd");
        allureResultsPath = Path.Combine(projectRoot, "TestResults", $"Allure_{timestamp}");

        Directory.CreateDirectory(screenshotsDir);
        Directory.CreateDirectory(logsDir);

        // ВАЖНО: Очистить старую директорию и создать новую
        if (Directory.Exists(allureResultsPath))
        {
            Directory.Delete(allureResultsPath, true);
        }
        Directory.CreateDirectory(allureResultsPath);

        // Конфигурация Allure - ЭТО ОБЯЗАТЕЛЬНО!
        string allureConfigPath = Path.Combine(projectRoot, "allureConfig.json");
        var allureConfig = new JObject(
            new JProperty("allure", new JObject(
                new JProperty("directory", allureResultsPath)
            ))
        );
        File.WriteAllText(allureConfigPath, allureConfig.ToString());

        // Устанавливаем переменные окружения
        Environment.SetEnvironmentVariable("ALLURE_CONFIG", allureConfigPath);
        Environment.SetEnvironmentVariable("allure_results_directory", allureResultsPath);

        // Инициализация логгера
        string logFilePath = Path.Combine(logsDir, $"test-run-{timestamp}.log");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(
                path: logFilePath,
                rollingInterval: RollingInterval.Infinite,
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("\n\n===================================");
        Log.Information("====== GLOBAL SETUP STARTED =======");
        Log.Information($"Project root: {projectRoot}");
        Log.Information($"Log file: {logFilePath}");
        Log.Information($"Screenshots: {screenshotsDir}");
        Log.Information($"Allure results: {allureResultsPath}");
        Log.Information($"Allure config: {allureConfigPath}");
        Log.Information($"Test run started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Log.Information("===================================");

        TestContext.Progress.WriteLine($"✅ Log file: {logFilePath}");
        TestContext.Progress.WriteLine($"✅ Allure results: {allureResultsPath}");
        TestContext.Progress.WriteLine($"✅ Allure config: {allureConfigPath}");
        TestContext.Progress.WriteLine("GLOBAL SETUP COMPLETED.\n");
    }

    [OneTimeTearDown]
    public void RunAfterAllTests()
    {
        Log.Information("===================================");
        Log.Information("GLOBAL TEARDOWN: Test execution completed");
        Log.Information($"Finished at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Log.Information("===================================");

        TestContext.Progress.WriteLine("\n=== GLOBAL TEARDOWN STARTED ===");

        // Генерация Allure отчета
        GenerateAllureReport();

        Log.CloseAndFlush();
        TestContext.Progress.WriteLine("=== GLOBAL TEARDOWN COMPLETED ===\n");
    }
    private void GenerateAllureReport()
    {
        try
        {
            string projectRoot = ConfigReader.GetProjectRoot();
            string reportOutput = Path.Combine(projectRoot, "TestResults", "AllureReport");

            if (!string.IsNullOrEmpty(allureResultsPath) && Directory.Exists(allureResultsPath))
            {
                // Проверяем наличие JSON файлов
                var jsonFiles = Directory.GetFiles(allureResultsPath, "*.json");
                TestContext.Progress.WriteLine($"Found {jsonFiles.Length} JSON result files in {allureResultsPath}");

                if (jsonFiles.Length == 0)
                {
                    TestContext.Progress.WriteLine("⚠️ No Allure result files found!");
                    Log.Warning("No Allure JSON files found in results directory.");
                    return;
                }

                TestContext.Progress.WriteLine($"Generating Allure report from {allureResultsPath}...");
                Log.Information($"Generating Allure report from {allureResultsPath}...");

                var processGenerate = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = @"E:\Git_Projects\owasp-juiceshop-vs-project\allure-2.35.1\bin\allure.bat",
                        Arguments = $"generate \"{allureResultsPath}\" -o \"{reportOutput}\" --clean",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                processGenerate.Start();
                string output = processGenerate.StandardOutput.ReadToEnd();
                string errors = processGenerate.StandardError.ReadToEnd();
                processGenerate.WaitForExit();

                if (processGenerate.ExitCode == 0)
                {
                    TestContext.Progress.WriteLine($"✅ Allure report generated: {reportOutput}");
                    Log.Information($"Allure report generated successfully: {reportOutput}");

                    // АВТОМАТИЧЕСКИ ОТКРЫВАЕМ ОТЧЕТ
                    TestContext.Progress.WriteLine($"Opening Allure report...");

                    var processOpen = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = @"E:\Git_Projects\owasp-juiceshop-vs-project\allure-2.35.1\bin\allure.bat",
                            Arguments = $"open \"{reportOutput}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    processOpen.Start();
                    TestContext.Progress.WriteLine($"✅ Allure report opened in browser");
                    TestContext.Progress.WriteLine($"   If not opened automatically, run: allure open \"{reportOutput}\"");
                }
                else
                {
                    TestContext.Progress.WriteLine($"⚠️ Allure report generation failed:");
                    TestContext.Progress.WriteLine($"   Output: {output}");
                    TestContext.Progress.WriteLine($"   Errors: {errors}");
                    Log.Warning($"Allure report generation failed. Exit code: {processGenerate.ExitCode}");
                }
            }
            else
            {
                TestContext.Progress.WriteLine($"⚠️ Allure results path not found or empty: {allureResultsPath}");
                Log.Warning($"Allure results path issue: {allureResultsPath}");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"❌ Allure report generation error: {ex.Message}");
            TestContext.Progress.WriteLine($"❌ Allure report generation error: {ex.Message}");
        }
    }
}