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

        private readonly By ACTIVE_OVERLAY_BACKDROP = By.CssSelector("cdk-overlay-backdrop");
        private By WelcomeBannerCloseButton => By.CssSelector("button[aria-label='Close Welcome Banner']");
        private By AccountButton = By.Id("navbarAccount");
        protected By LogoutButton = By.Id("navbarLogoutButton");

        private readonly By MatSnackBar = By.CssSelector("snack-bar-container");
        private readonly By GenericModalOverlay = By.CssSelector("div.mat-dialog-container");

        protected IWebDriver Driver;
        protected WebDriverWait Wait;
        protected string baseUrl;
        private List<By> BlockingOverlays = new List<By>();

        protected BasePage(IWebDriver driver)
        {
            Driver = driver;

            int explicitWaitSeconds = ConfigReader.GetExplicitWait();
            Wait = new WebDriverWait(driver, TimeSpan.FromSeconds(explicitWaitSeconds));
            baseUrl = ConfigReader.GetBaseUrl();
            BlockingOverlays.Add(MatSnackBar);
            BlockingOverlays.Add(GenericModalOverlay);
        }

        /// <summary>
        /// Waits for an element to be visible on the page.
        /// </summary>
        [AllureStep("Waiting for element visibility: {locator}")]
        protected IWebElement WaitForElementVisible(By locator)
        {
            Log.Debug($"⏳ Waiting for element visibility: {locator}");
            try
            {
                var element = Wait.Until(ExpectedConditions.ElementIsVisible(locator));
                Log.Debug($"✅ Element visible: {locator}");
                return element;
            }
            catch (Exception ex)
            {
                Log.Error($"❌ Element not visible: {locator}. Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Waits for an element to be clickable on the page.
        /// </summary>
        [AllureStep("Waiting for element clickable: {locator}")]
        protected IWebElement WaitForElementClickable(By locator)
        {
            Log.Debug($"⏳ Waiting for element clickability: {locator}");
            try
            {
                var element = Wait.Until(ExpectedConditions.ElementToBeClickable(locator));
                Log.Debug($"✅ Element clickable: {locator}");
                return element;
            }
            catch (Exception ex)
            {
                Log.Error($"❌ Element not clickable: {locator}. Error: {ex.Message}");
                throw;
            }
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
        private void CheckOverlays(By locator)
        {
            
        }
        /// <summary>
        /// Clicks an element after waiting for it to be clickable.
        /// </summary>
        [AllureStep("Clicking element: {locator}")]
        protected void ClickElement(By locator)
        {
            TryDismissBlockingOverlay();

            Log.Information($"Clicking element: {locator}");
            try
            {
                WaitForElementClickable(locator).Click();
                Log.Debug($"The element was clicked successfully.: {locator}");
            }
            catch (TimeoutException ex){
                Log.Error(ex, $"Expectation error. Element not made clickable: {locator}.");
                throw new WebDriverException($"Timeout: Element {locator} was not clickable.", ex);
            }
            catch (WebDriverException ex)
            {
                Log.Error(ex, $"Error interacting with element: {locator}.");
                throw; // Rethrow the original WebDriver exception
            }
        }

        /// <summary>
        /// Clears the input field and enters the specified text after waiting for visibility.
        /// </summary>
        [AllureStep("Clears the input field and text '{text}' in element: {locator}")]
        protected void EnterText(By locator, string text)
        {
            Log.Information($"⌨️ Entering text into {locator}: '{text}'");
            try
            {
                var element = WaitForElementVisible(locator);
                element.Clear();
                element.SendKeys(text);
                Log.Debug($"✅ Text entered successfully: {locator}");
            }
            catch (Exception ex)
            {
                Log.Error($"❌ Failed to enter text into {locator}. Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves the text content of a visible element.
        /// </summary>
        [AllureStep("Retrieves the text content of a visible element: {locator}")]
        protected string GetElementText(By locator)
        {
            Log.Debug($"📝 Getting text from element: {locator}");
            try
            {
                string text = WaitForElementVisible(locator).Text;
                Log.Information($"✅ Got text from {locator}: '{text}'");
                return text;
            }
            catch (Exception ex)
            {
                Log.Error($"❌ Failed to get text from {locator}. Error: {ex.Message}");
                throw;
            }
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
        
        /// <summary>
        /// Closes the initial 'Welcome Banner' overlay common in Juice Shop.
        /// It uses a short, dedicated wait time to prevent blocking subsequent tests.
        /// </summary>
        [AllureStep("Closing the Welcome Banner (if present)")]
        public void DismissWelcomeBanner()
        {
            // Use a short local WebDriverWait so we don't wait the full ExplicitWait
            // when the banner does not appear (for example, after a successful login).
            WebDriverWait shortWait = new WebDriverWait(Driver, TimeSpan.FromSeconds(ConfigReader.GetImplicitWait()));

            Log.Information("Attempting to close the Welcome Banner.");

            try
            {
                IWebElement closeButton = shortWait.Until(ExpectedConditions.ElementToBeClickable(WelcomeBannerCloseButton));
                closeButton.Click();
                shortWait.Until(ExpectedConditions.InvisibilityOfElementLocated(WelcomeBannerCloseButton));
                Log.Information("Welcome Banner successfully closed.");
            }
            catch (TimeoutException)
            {
                // Expected when the banner does not appear (for example, already on the login page or after login)
                Log.Debug("Welcome Banner did not appear or was not clickable within 5 seconds, skipping dismissal.");
            }
            catch (Exception ex)
            {
                // Log any other unexpected errors
                Log.Error(ex, "An unexpected error occurred while trying to dismiss the Welcome Banner.");
            }
        }

        /// <summary>
        /// Checks that the user is logged in by looking for the 'Logout' button.
        /// The "Account" menu has to be opened first.
        /// </summary>
        /// <returns>True if the Logout button is visible.</returns>
        [AllureStep("Checking if button (Logout) exists")]
        public bool IsUserLoggedIn()
        {
        
            ClickElement(AccountButton);

            try
            {
                WaitForElementVisible(LogoutButton);
                
            }
            catch (TimeoutException ex)
            {
                Log.Error(ex, $"Timeout error. Element not visible: {LogoutButton}.");
                return false;
            }
            catch (Exception)
            {
                // Not found or not displayed means the user is not logged in
                Log.Debug($"Element not found: {LogoutButton}");
            }
            try
            {
                ClickElement(AccountButton);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An unexpected error occurred while trying to close Account menu.");
            }
            //finally
            //{
            //    // Optional, but useful: close the menu by clicking Account again
            //    try
            //    {
            //        ClickElement(AccountButton);
            //    }
            //    catch (Exception ex) 
            //    {
            //        Log.Error(ex, "An unexpected error occurred while trying to close Account menu."); 
            //    }
            //}
            return true;
        }

        [AllureStep("Checking and removing blocking overlays before clicking.")]

        private void TryDismissBlockingOverlay()
        {
            // 1. Save the original implicit wait timeout
            TimeSpan originalImplicitWait = Driver.Manage().Timeouts().ImplicitWait;
            // 2. Set it to 0 seconds for an INSTANT check with FindElements
            Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(0);

            WebDriverWait longWait = new WebDriverWait(Driver, TimeSpan.FromSeconds(10));
            bool wasOverlayFound = false;

            try
            {
                Log.Debug("Starting quick check (Implicit Wait = 0s) for blocking overlays...");

                foreach (var locator in BlockingOverlays)
                {
                    // INSTANT CHECK: the element is present in the DOM (Count > 0)
                    if (Driver.FindElements(locator).Count > 0)
                    {
                        wasOverlayFound = true;
                        Log.Information($"Blocking overlay found: {locator}. Waiting for its invisibility (max 10s).");

                        // If the element is found, use a LONG EXPLICIT wait for it to disappear
                        try
                        {
                            longWait.Until(ExpectedConditions.InvisibilityOfElementLocated(locator));
                            Log.Information($"Blocking overlay {locator} successfully dismissed.");
                        }
                        catch (TimeoutException)
                        {
                            Log.Error($"Timeout waiting for invisibility of blocking element {locator}. Click may still fail.");
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, $"Error while waiting for overlay {locator} to dismiss.");
                        }

                        // Once one blocker is gone, treat the path as clear.
                        break;
                    }
                }
            }
            finally
            {
                // 3. Restore the original implicit timeout in any case
                Driver.Manage().Timeouts().ImplicitWait = originalImplicitWait;
            }

            if (!wasOverlayFound)
            {
                Log.Debug("No known blocking overlays found. Proceeding with click.");
            }
        }
    }

}