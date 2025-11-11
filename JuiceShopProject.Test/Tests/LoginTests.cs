// ==============================================
// Tests/LoginTests.cs
// ==============================================

using NUnit.Framework;
using Allure.Net.Commons;
using Allure.NUnit.Attributes;
using OpenQA.Selenium;
using NUnit.Allure.Core;  // ВАЖНО: Добавить этот using
using JuiceShopProject.Test.Base;
using JuiceShopProject.Test.Pages;
using Serilog;

namespace JuiceShopProject.Test.Tests
{
    [TestFixture]
    [AllureNUnit]  // ЭТО КРИТИЧНО ДЛЯ РАБОТЫ ALLURE!
    [AllureSuite("UI Tests")]
    [AllureSubSuite("Login Suite")]
    public class LoginTests : BaseTest
    {
        private AllureLifecycle _allure;

        [SetUp]
        public void SetupAllure()
        {
            _allure = AllureLifecycle.Instance;
        }
        [Test, Order(1)]
        [AllureName("Проверка открытия главной страницы")]
        [AllureDescription("Проверка открытия главной страницы")]
        [AllureSeverity(SeverityLevel.critical)]
        public void Test_OpenHomePage()
        {

            Log.Information("Verifying home page title.");

            string stepId = Guid.NewGuid().ToString();
            _allure.StartStep(new StepResult { name = "Проверка заголовка страницы" });



            Assert.That(Driver.Title, Does.Contain("OWASP Juice Shop"),
                "The page title does not contain 'OWASP Juice Shop'.");

            _allure.StopStep();
            Log.Information("Home page title verification passed.");
        }
        
        [Test]
        [AllureSeverity(SeverityLevel.critical)]
        [AllureTag("Smoke", "Login")]
        [AllureDescription("Проверка успешного входа в систему с валидными данными")]
        public void Test_SuccessfulLogin()
        {
            // Arrange
            var loginPage = new LoginPage(Driver);

            // Act
            loginPage.Open()
                     .Login("admin@juice-sh.op", "admin123");

            // Assert
            Assert.That(Driver.Url, Does.Contain("account"),
                "User should be redirected to account page after successful login.");

            Log.Information("Login successful.");
           
        }
        [Ignore("Тест временно отключен")]
        [Test]
        [AllureSeverity(SeverityLevel.normal)]
        [AllureTag("Regression", "Login")]
        [AllureDescription("Проверка отображения ошибки при неверных данных")]
        public void Test_InvalidLogin()
        {

            // Arrange
            var loginPage = new LoginPage(Driver);

            // Act
            loginPage.Open()
                     .Login("wrong@example.com", "badpass");

            // Assert
            Assert.That(loginPage.IsErrorDisplayed(), Is.True,
                "Error message should be displayed for invalid credentials.");

            string errorText = loginPage.GetErrorMessage();
            Assert.That(errorText, Does.Contain("Invalid").Or.Contain("incorrect"),
                "Error message should indicate invalid credentials.");

            Log.Information("Invalid login validation displayed correctly.");
        }

        [Ignore("Тест временно отключен")]
        [Test]
        [AllureTag("Negative", "Login")]
        [AllureDescription("Проверка валидации пустых полей")]
        public void Test_EmptyCredentials()
        {
            // Arrange
            var loginPage = new LoginPage(Driver);

            // Act
            loginPage.Open()
                     .Login("", "");

            // Assert
            Assert.That(loginPage.IsErrorDisplayed(), Is.True,
                "Error message should be displayed for empty credentials.");

            Log.Information("Empty credentials validation works correctly.");
        }
    }
}