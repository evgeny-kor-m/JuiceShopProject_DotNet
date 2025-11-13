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
        /// Открыть страницу логина
        /// </summary>
        [AllureStep("Открываем страницу логина")]
        public LoginPage Open()
        {
            Driver.Navigate().GoToUrl($"{baseUrl}/#/login");
            Log.Information($"Navigated to login page: {baseUrl}/#/login");
            return this;
        }

        /// <summary>
        /// Выполнить вход
        /// </summary>
        [AllureStep("Вводим логин '{username}' и пароль '{password}'")]
        public LoginPage Login(string username, string password)
        {
            EnterText(emailInput, username);
            EnterText(passwordInput, password);
            ClickElement(loginButton);
            Log.Information($"Attempting login with user: {username}");

            return this;
        }

        /// <summary>
        /// Получить сообщение об ошибке
        /// </summary>
        [AllureStep("Получаем текст ошибки")]
        public string GetErrorMessage()
        {
            // Поскольку GetElementText уже имеет AllureStep, 
            // дополнительный шаг здесь не нужен, просто вызываем базовый метод.
            return GetElementText(errorMessage);
        }

        /// <summary>
        /// Проверка отображения ошибки
        /// </summary>
        [AllureStep("Проверяем, отображается ли сообщение об ошибке")]
        public bool IsErrorDisplayed()
        {
            return IsElementDisplayed(errorMessage);
        }
    }
}