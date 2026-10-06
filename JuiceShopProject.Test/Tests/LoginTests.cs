// ==============================================
// Tests/LoginTests.cs
// ==============================================

using NUnit.Framework;
using Allure.Net.Commons;
using Allure.NUnit.Attributes;
using OpenQA.Selenium;
using NUnit.Allure.Core;  // IMPORTANT: this using is required
using JuiceShopProject.Test.Base;
using JuiceShopProject.Test.Pages;
using Serilog;

namespace JuiceShopProject.Test.Tests
{
    [TestFixture]
    [AllureNUnit]  // REQUIRED FOR ALLURE TO WORK!
    [AllureSuite("UI Tests")]
    [AllureSubSuite("Login Suite")]
    public class LoginTests : BaseTest
    {
        [Test, Order(1)]
        [AllureName("Home page opens")]
        [AllureDescription("Verifies that the home page opens")]
        [AllureSeverity(SeverityLevel.critical)]
        public void Test_OpenHomePage()
        {

            Log.Information("Verifying home page title.");

            string stepId = Guid.NewGuid().ToString();
           
            Assert.That(Driver.Title, Does.Contain("OWASP Juice Shop"), "The page title does not contain 'OWASP Juice Shop'.");
            Log.Information("Home page title verification passed.");
        }
        
        [Test]
        [AllureSeverity(SeverityLevel.critical)]
        [AllureTag("Smoke", "Login")]
        [AllureDescription("Verifies successful login with valid credentials")]
        public void Test_SuccessfulLogin()
        {
            // Arrange
            var loginPage = new LoginPage(Driver);

            // Act
            loginPage.Open()
                     .Login("admin@juice-sh.op", "admin123");

            // Assert
            bool isLoggedIn = BasePageInstance.IsUserLoggedIn(); // Use the BasePageInstance created in SetUp

            Assert.That(isLoggedIn, Is.True,
                "User should be logged in, but the Logout button is not visible.");

            Log.Information("Login successful.");
           
        }
        [Ignore("Test temporarily disabled")]
        [Test]
        [AllureSeverity(SeverityLevel.normal)]
        [AllureTag("Regression", "Login")]
        [AllureDescription("Verifies that an error is shown for invalid credentials")]
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

        [Ignore("Test temporarily disabled")]
        [Test]
        [AllureTag("Negative", "Login")]
        [AllureDescription("Verifies validation of empty fields")]
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