// ==============================================
// Base/BasePage.cs
// ==============================================
using Allure.NUnit.Attributes;
using JuiceShopProject.Test.Utilities;
using OpenQA.Selenium;
using OpenQA.Selenium.BiDi.BrowsingContext;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using Serilog;
using System;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using static System.Net.Mime.MediaTypeNames;

namespace JuiceShopProject.Test.Base
{
    /// <summary>
    /// Base class for all Page Objects.
    /// Contains common logic for interacting with web elements.
    /// </summary>
    public abstract class BasePage
    {
        protected IWebDriver Driver;
        protected WebDriverWait Wait;
        protected string baseUrl;

        protected BasePage(IWebDriver driver)
        {
            Driver = driver;

            int explicitWaitSeconds = ConfigReader.GetExplicitWait();
            Wait = new WebDriverWait(driver, TimeSpan.FromSeconds(explicitWaitSeconds));
            baseUrl = ConfigReader.GetBaseUrl();
        }

        /// <summary>
        /// Waits for an element to be visible on the page.
        /// </summary>
        [AllureStep("Waiting for element visibility: {locator}")]
        protected IWebElement WaitForElementVisible(By locator)
        {
            Log.Debug($"Waiting for element visibility: {locator}");
            return Wait.Until(ExpectedConditions.ElementIsVisible(locator));
        }

        /// <summary>
        /// Waits for an element to be clickable on the page.
        /// </summary>
        [AllureStep("Waiting for element clickable: {locator}")]
        protected IWebElement WaitForElementClickable(By locator)
        {
            Log.Debug($"Waiting for element clickable: {locator}");
            return Wait.Until(ExpectedConditions.ElementToBeClickable(locator));
        }

        /// <summary>
        /// Checks if the page is fully loaded (document.readyState is 'complete').
        /// </summary>
        public virtual bool IsPageLoaded()
        {
            return ((IJavaScriptExecutor)Driver).ExecuteScript("return document.readyState").Equals("complete");
        }

        /// <summary>
        /// Retrieves the page title.
        /// </summary>
        [AllureStep("Retrieves the page title")]
        public string GetPageTitle()
        {
            Log.Debug("Retrieves the page title");
            return Driver.Title;
        }

        /// <summary>
        /// Retrieves the current page URL.
        /// </summary>
        [AllureStep("Retrieves the current page URL")]
        public string GetCurrentUrl()
        {
            Log.Debug("Retrieves the current page URL");
            return Driver.Url;
        }

        /// <summary>
        /// Clicks an element after waiting for it to be clickable.
        /// </summary>
        [AllureStep("Clicking element: {locator}")]
        protected void ClickElement(By locator)
        {
            try
            {
                Log.Debug($"Clicking element: {locator}");
                WaitForElementClickable(locator).Click();
                Log.Information($"Элемент успешно нажат: {locator}");
            }
            catch (TimeoutException ex){
                Log.Error(ex, $"Ошибка ожидания. Элемент не стал кликабельным: {locator}.");
                throw new WebDriverException($"Таймаут ожидания: элемент {locator} не был кликабельным.", ex);
            }
            catch (WebDriverException ex)
            {
                Log.Error(ex, $"Error interacting with element: {locator}.");
                throw; // Перебрасываем оригинальное исключение WebDriver
            }
        }

        /// <summary>
        /// Clears the input field and enters the specified text after waiting for visibility.
        /// </summary>
        [AllureStep("Clears the input field and text '{text}' in element: {locator}")]
        protected void EnterText(By locator, string text)
        {
            Log.Debug($"Clears the input field and enters the specified text '{text}' after waiting for visibility element: '{locator}'");
            var element = WaitForElementVisible(locator);
            element.Clear();
            element.SendKeys(text);
        }

        /// <summary>
        /// Retrieves the text content of a visible element.
        /// </summary>
        [AllureStep("Retrieves the text content of a visible element: {locator}")]
        protected string GetElementText(By locator)
        {
            Log.Debug($"Retrieves the text content of a visible element: '{locator}'");
            return WaitForElementVisible(locator).Text;
        }

        /// <summary>
        /// Checks if an element is currently displayed on the page.
        /// Handles NoSuchElementException safely.
        /// </summary>
        [AllureStep("Checks if an element is currently displayed on the page: {locator}")]
        protected bool IsElementDisplayed(By locator)
        {
            try
            {
                // This requires a separate find call, which respects the implicit wait set on the driver
                Log.Debug($"Checks if an element is currently displayed on the page: '{locator}'");
                return Driver.FindElement(locator).Displayed;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        /// <summary>
        /// Scrolls the page until the specified element is visible in the viewport.
        /// </summary>
        [AllureStep("Scrolls the page until the specified element is visible in the viewport: {locator}")]
        protected void ScrollToElement(By locator)
        {
            Log.Debug($"Scrolls the page until the specified element is visible in the viewport: '{locator}'");
            var element = Driver.FindElement(locator);
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView(true);", element);
        }
    }
}