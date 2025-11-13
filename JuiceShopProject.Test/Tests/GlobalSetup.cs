using JuiceShopProject.Test.Utilities;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Debugging;
using NUnit.Framework;
using System;
using System.IO;
using Serilog.Events;

[SetUpFixture]
public class GlobalSetup
{
    private static string? projectRoot;
    private static string? screenshotsDir;
    private static string? logsDir;
    private static string? allureResultsPath;

    [OneTimeSetUp]
    public void RunBeforeAnyTests()
    {
        TestContext.Progress.WriteLine("\n\n=== GLOBAL SETUP STARTED ===");
        SelfLog.Enable(Console.Error);

        // Создание директорий
        projectRoot = ConfigReader.GetProjectRoot();
        screenshotsDir = ConfigReader.GetScreenshotsPath();
        logsDir = ConfigReader.GetLogsPath();

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");

        Directory.CreateDirectory(screenshotsDir);
        Directory.CreateDirectory(logsDir);

        Log.Information("\n\n===================================");
        Log.Information("====== GLOBAL SETUP STARTED =======");
        Log.Information($"Project root: {projectRoot}");
        Log.Information($"Screenshots: {screenshotsDir}");

        CreateGlobalTestLogger(timestamp);
        AllureConfiguration(timestamp);

        Log.Information($"Test run started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Log.Information("===================================");
        Log.Information("====== GLOBAL SETUP COMPLETED =======");


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
        Log.Information("=== GLOBAL TEARDOWN COMPLETED ===\n");
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
                
                if (jsonFiles.Length == 0){
                    Log.Warning("⚠️ No Allure JSON files found in results directory.");
                    return;
                }
               
                Log.Information($"Found {jsonFiles.Length} JSON result files. Generating Allure report from {allureResultsPath}...");

                var processGenerate = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo{
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
                    OpenAllureReport(reportOutput);
                else{
                    Log.Information($"⚠️ Allure report generation failed:");
                    Log.Information($"   Output: {output}");
                    Log.Information($"   Errors: {errors}");
                    Log.Warning($"⚠️ Allure report generation failed. Exit code: {processGenerate.ExitCode}");
                }
            }
            else Log.Warning($"⚠️ Allure results path not found or empty: {allureResultsPath}");
        }
        catch (Exception ex)
        {
            Log.Error($"❌ Allure report generation error: {ex.Message}");
        }
    }
    private void OpenAllureReport(string reportPath)
    {
        Log.Information($"✅ Allure report generated successfully: {reportPath}");
        Log.Information($"Opening Allure report...");

        var processOpen = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = @"E:\Git_Projects\owasp-juiceshop-vs-project\allure-2.35.1\bin\allure.bat",
                Arguments = $"open \"{reportPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        try
        {
            processOpen.Start();
        }
        catch (Exception ex)
        {
            Log.Error($"❌ Fail to open Allure report with: {ex.Message}");
            return;
        }
        Log.Information($"✅ Allure report opened in browser");
        Log.Information($"   If not opened automatically, run: allure open \"{reportPath}\"");
    }

    private void AllureConfiguration(string timestamp)
    {
        allureResultsPath = Path.Combine(projectRoot, "TestResults", $"Allure_{timestamp}");

        // ВАЖНО: Очистить старую директорию и создать новую
        if (Directory.Exists(allureResultsPath))
            Directory.Delete(allureResultsPath, true);

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

        Log.Information($"Allure results: {allureResultsPath}");
        Log.Information($"Allure config: {allureConfigPath}");

        TestContext.Progress.WriteLine($"✅ Allure results: {allureResultsPath}");
        TestContext.Progress.WriteLine($"✅ Allure config: {allureConfigPath}");
    }

    private void CreateGlobalTestLogger(string timestamp)
    {
        // Получаем уровень для ГЛОБАЛЬНОГО лога
        if (!Enum.TryParse(ConfigReader.GetRunLogLevel(), true, out LogEventLevel runLogLevel))
            runLogLevel = LogEventLevel.Information; // Fallback

        string logFilePath = Path.Combine(logsDir, $"RunLog-{timestamp}.log");
        try
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(runLogLevel) // Устанавливаем глобальный уровень
                .Enrich.With<TestNameEnricher>() // ✅ Теперь будет "GLOBAL" вместо пустоты
                .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information) // Консоль часто ограничивают
                .WriteTo.File(
                    path: logFilePath,
                    restrictedToMinimumLevel: runLogLevel, // Применяем уровень к файлу
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{TestName}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }
        catch (Exception ex)
        {
            Log.Error($"❌ Global test logger creation error: {ex.Message}");
        }
        Log.Information($"Log level (Run): {runLogLevel}"); // Логируем установленный уровень
        Log.Information($"Log file: {logFilePath}");

        TestContext.Progress.WriteLine($"✅ Log file: {logFilePath}");
    }
}