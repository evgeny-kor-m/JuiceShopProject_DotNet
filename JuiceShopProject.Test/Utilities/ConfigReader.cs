// ==============================================
// Utilities/ConfigReader.cs
// ==============================================


using Newtonsoft.Json.Linq;
using System.IO;


namespace JuiceShopProject.Test.Utilities
{
    public static class ConfigReader
    {
        private static readonly JObject configData;
        private static string projectRoot;

        static ConfigReader()
        {
            projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\"));
            var configPath = Path.Combine(projectRoot, "appsettings.json");
            if (!File.Exists(configPath))
                throw new FileNotFoundException($"Configuration file not found: {configPath}");

            var json = File.ReadAllText(configPath);
            configData = JObject.Parse(json);
        }

        // Retrieves the base URL from the configuration
        public static string? GetBaseUrl() =>
            configData["BaseUrl"]?.ToString();

        // Retrieves the browser name from the configuration
        public static string? GetBrowser() =>
            configData["Browser"]?.ToString();

        // Retrieves the explicit wait timeout
        public static int GetExplicitWait()
        {
            return (int)configData["Timeouts"]["ExplicitWait"];
        }
        // Retrieves the implicit wait timeout
        public static int GetImplicitWait()
        {
            return (int)configData["Timeouts"]["ImplicitWait"];
        }
        public static string GetScreenshotsPath()
        {
            string relativePath = configData["Paths"]?["Screenshots"]?.ToString() ?? "TestResults/Screenshots";
            return Path.Combine(projectRoot, relativePath);
        }
        public static string GetLogsPath()
        {
            string relativePath = configData["Paths"]?["Logs"]?.ToString() ?? "TestResults/Logs";
            return Path.Combine(projectRoot, relativePath);
        }
        public static string GetProjectRoot() => projectRoot;
        
        // --- НОВЫЕ МЕТОДЫ ДЛЯ ЧТЕНИЯ УРОВНЕЙ ЛОГА ---
        public static string GetRunLogLevel() =>
            configData["LogLevels"]?["RunLog"]?.ToString() ?? "Information";

        public static string GetTestLogLevel() =>
            configData["LogLevels"]?["TestLog"]?.ToString() ?? "Debug";
        // --- КОНЕЦ НОВЫХ МЕТОДОВ ---
    }
}