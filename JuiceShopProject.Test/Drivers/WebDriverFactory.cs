// ==============================================
// Drivers/WebDriverFactory.cs
// ==============================================

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using JuiceShopProject.Test.Utilities;

namespace JuiceShopProject.Test.Drivers
{
    public static class WebDriverFactory
    {
        public static IWebDriver CreateDriver()
        {
            string browser = ConfigReader.GetBrowser()?.ToLower();

            IWebDriver driver;

            switch (browser)
            {
                case "chrome":
                    var chromeOptions = new ChromeOptions();
                    chromeOptions.AddArgument("--start-maximized");
                    driver = new ChromeDriver(chromeOptions);
                    break;

                case "firefox":
                    var firefoxOptions = new FirefoxOptions();
                    driver = new FirefoxDriver(firefoxOptions);
                    break;

                default:
                    throw new ArgumentException($"❌ Browser '{browser}' is not supported. Use 'chrome' or 'firefox' in appsettings.json.");
            }

            return driver;
        }
    }
}