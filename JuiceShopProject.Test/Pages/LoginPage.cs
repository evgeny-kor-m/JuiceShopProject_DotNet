// ==============================================
// Pages/LoginPage.cs
// ==============================================

using JuiceShopProject.Test.Base;
using OpenQA.Selenium;
using Allure.Net.Commons;
using Allure.NUnit.Attributes;
using Serilog;

namespace JuiceShopProject.Test.Pages
{
    public class LoginPage : BasePage
    {
        private readonly By emailInput = By.Id("email");
        private readonly By passwordInput = By.Id("password");
        private readonly By loginButton = By.Id("loginButton");
        private readonly By errorMessage = By.CssSelector(".error-message");

        public LoginPage(IWebDriver driver) : base(driver) { }

        /// <summary>
        /// Opens the login page
        /// </summary>
        [AllureStep("Open Login Page")]
        public LoginPage Open()
        {
            Driver.Navigate().GoToUrl($"{baseUrl}/#/login");
            Log.Information($"Navigated to login page: {baseUrl}/#/login");
            return this;
        }

        /// <summary>
        /// Logs in with the given credentials
        /// </summary>
        [AllureStep("Enter username '{username}' and password '{password}'")]
        public LoginPage Login(string username, string password)
        {
            EnterText(emailInput, username);
            EnterText(passwordInput, password);
            ClickElement(loginButton);
            Log.Information($"Attempting login with user: '{username}' and password: '{password}'");

            return this;
        }

        /// <summary>
        /// Gets the error message text
        /// </summary>
        [AllureStep("Get the error message text")]
        public string GetErrorMessage()
        {
            // GetElementText already has its own AllureStep,
            // so no extra step is needed here; just call the base method.
            return GetElementText(errorMessage);
        }

        /// <summary>
        /// Checks whether the error message is displayed
        /// </summary>
        [AllureStep("Check whether the error message is displayed")]
        public bool IsErrorDisplayed()
        {
            return IsElementDisplayed(errorMessage);
        }
    }
}