// ==============================================
// Pages/LoginPage.cs
// ==============================================

using JuiceShopProject.Test.Base;
using OpenQA.Selenium;
using Allure.Net.Commons;
using Allure.NUnit.Attributes;

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
            //AllureStep("Открываем страницу логина", () =>
            //{
                Driver.Navigate().GoToUrl($"{baseUrl}/#/login");
            //});
            return this;
        }

        /// <summary>
        /// Выполнить вход
        /// </summary>
        [AllureStep("Вводим логин '{username}' и пароль")]
        public LoginPage Login(string username, string password)
        {
            //AllureStep($"Вводим логин '{username}' и пароль", () =>
            //{
                EnterText(emailInput, username);
                EnterText(passwordInput, password);
            //});

            //AllureStep("Нажимаем кнопку Login", () =>
            //{
                ClickElement(loginButton);
            //});

            return this;
        }

        /// <summary>
        /// Получить сообщение об ошибке
        /// </summary>
        [AllureStep("Получаем текст ошибки")]
        public string GetErrorMessage()
        {
            string error = "";
            //AllureStep("Получаем текст ошибки", () =>
            //{
                error = GetElementText(errorMessage);
            //});
            return error;
        }

        /// <summary>
        /// Проверка отображения ошибки
        /// </summary>
        public bool IsErrorDisplayed()
        {
            return IsElementDisplayed(errorMessage);
        }
    }
}