# JuiceShopProject.Test

UI test automation framework in C# / .NET 8 for the [OWASP Juice Shop](https://owasp.org/www-project-juice-shop/) web application.
Built with NUnit, Selenium WebDriver, and the Page Object Model, with Allure reporting and Serilog logging.

## Tech stack

| Area | Tool |
| --- | --- |
| Language / runtime | C#, .NET 8 |
| Test framework | NUnit 3 |
| Browser automation | Selenium WebDriver 4, ChromeDriver, WaitHelpers |
| Reporting | Allure (Allure.NUnit) |
| Logging | Serilog (console and file sinks) |
| Coverage | coverlet |

## Project structure

```
JuiceShopProject.Test/
├── Base/
│   ├── BasePage.cs            # Shared page behavior: waits, element lookups, common actions
│   └── BaseTest.cs            # Shared test setup and teardown (driver lifecycle)
├── Drivers/
│   └── WebDriverFactory.cs    # Creates and configures WebDriver instances
├── Pages/
│   └── LoginPage.cs           # Page object for the login page
├── Tests/
│   ├── GlobalSetup.cs         # One-time setup for the whole test run
│   └── LoginTests.cs          # Login scenarios
├── Utilities/
│   ├── ConfigReader.cs        # Reads settings from appsettings.json
│   └── TestNameEnricher.cs    # Adds the current test name to Serilog log entries
├── TestData/                  # Test data files
├── appsettings.json           # Environment settings (base URL, browser, timeouts)
└── allureConfig.Template.json # Template for the local Allure configuration
```

## Design

- **Page Object Model.** Each page is a class under `Pages/` that inherits from `BasePage`. Tests call page methods and never touch locators directly.
- **Driver factory.** `WebDriverFactory` is the single place where browsers are created and configured.
- **Base test class.** `BaseTest` owns the driver lifecycle, so test classes contain only scenarios.
- **Configuration over code.** Environment values live in `appsettings.json` and are read through `ConfigReader`.
- **Traceable logs.** `TestNameEnricher` tags every log line with the test that produced it.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Google Chrome
- A running Juice Shop instance, for example with Docker:

```bash
docker run --rm -p 3000:3000 bkimminich/juice-shop
```

- [Allure CLI](https://allurereport.org/docs/install/) (optional, for HTML reports)

## Setup

1. Clone the repository:

   ```bash
   git clone https://github.com/evgeny-kor-m/JuiceShopProject_DotNet.git
   cd JuiceShopProject_DotNet
   ```

2. Create the local Allure configuration from the template:

   ```bash
   cp JuiceShopProject.Test/allureConfig.Template.json JuiceShopProject.Test/allureConfig.json
   ```

3. Set the application URL and browser options in `JuiceShopProject.Test/appsettings.json`.

## Running the tests

```bash
dotnet restore
dotnet test
```

Run a single test class:

```bash
dotnet test --filter "FullyQualifiedName~LoginTests"
```

## Allure report

After a test run, generate and open the report from the results directory set in `allureConfig.json`:

```bash
allure serve <path-to-allure-results>
```

## Roadmap

- Page objects and tests for registration, product search, and basket flows
- API tests alongside the UI tests
- GitHub Actions workflow running the suite on every push
