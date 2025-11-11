// ==============================================
// Base/BasePage.cs
// ==============================================
using Allure.Commons;
using JuiceShopProject.Test.Utilities;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using Serilog;
using System;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

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
        protected IWebElement WaitForElementVisible(By locator)
        {
            return Wait.Until(ExpectedConditions.ElementIsVisible(locator));
        }

        /// <summary>
        /// Waits for an element to be clickable on the page.
        /// </summary>
        protected IWebElement WaitForElementClickable(By locator)
        {
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
        public string GetPageTitle()
        {
            return Driver.Title;
        }

        /// <summary>
        /// Retrieves the current page URL.
        /// </summary>
        public string GetCurrentUrl()
        {
            return Driver.Url;
        }

        /// <summary>
        /// Clicks an element after waiting for it to be clickable.
        /// </summary>
        protected void ClickElement(By locator)
        {
            try
            {
                WaitForElementClickable(locator).Click();
                Log.Information($"Элемент успешно нажат: {locator}");
            }
            catch (TimeoutException ex){
                Log.Error(ex, $"Ошибка ожидания. Элемент не стал кликабельным: {locator}.");
                throw new WebDriverException($"Таймаут ожидания: элемент {locator} не был кликабельным.", ex);
            }
            catch (WebDriverException ex)
            {
                Log.Error(ex, $"Ошибка взаимодействия с элементом {locator}.");
                throw; // Перебрасываем оригинальное исключение WebDriver
            }
        }

        /// <summary>
        /// Clears the input field and enters the specified text after waiting for visibility.
        /// </summary>
        protected void EnterText(By locator, string text)
        {
            var element = WaitForElementVisible(locator);
            element.Clear();
            element.SendKeys(text);
        }

        /// <summary>
        /// Retrieves the text content of a visible element.
        /// </summary>
        protected string GetElementText(By locator)
        {
            return WaitForElementVisible(locator).Text;
        }

        /// <summary>
        /// Checks if an element is currently displayed on the page.
        /// Handles NoSuchElementException safely.
        /// </summary>
        protected bool IsElementDisplayed(By locator)
        {
            try
            {
                // This requires a separate find call, which respects the implicit wait set on the driver
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
        protected void ScrollToElement(By locator)
        {
            var element = Driver.FindElement(locator);
            ((IJavaScriptExecutor)Driver).ExecuteScript("arguments[0].scrollIntoView(true);", element);
        }
        //protected void AllureStep(string stepName, Action action)
        //{
        //    Log.Information($"[STEP: {stepName}] started.");

        //    var stepUuid = Guid.NewGuid().ToString();
        //    var stepResult = new StepResult { name = stepName };

        //    try
        //    {
        //        // Получаем UUID текущего теста из контекста
        //        var testUuid = AllureLifecycle.Instance.Context.Test;

        //        if (string.IsNullOrEmpty(testUuid))
        //        {
        //            Log.Warning($"[STEP: {stepName}] No active test context found. Executing without Allure step.");
        //            action.Invoke();
        //            return;
        //        }

        //        // Стартуем шаг с явным указанием родителя
        //        AllureLifecycle.Instance.StartStep(testUuid, stepUuid, stepResult);

        //        try
        //        {
        //            action.Invoke();
        //            AllureLifecycle.Instance.UpdateStep(stepUuid, s => s.status = Status.passed);
        //            Log.Information($"[STEP: {stepName}] successfully finished.");
        //        }
        //        catch (Exception ex)
        //        {
        //            AllureLifecycle.Instance.UpdateStep(stepUuid, s => s.status = Status.failed);
        //            Log.Error(ex, $"[STEP: {stepName}] завершился с ошибкой.");
        //            throw;
        //        }
        //        finally
        //        {
        //            AllureLifecycle.Instance.StopStep(stepUuid);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error($"[STEP: {stepName}] Fail with Exception: {ex}");
        //        throw;
        //    }
        //}
    }
}