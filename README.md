# DummyJSON API Functional System Tests

This project contains nine Gherkin-defined functional system tests for DummyJSON's Auth, Users, Products, and Carts resources. The public API is the system under test; scenarios send real HTTP requests and validate status codes, payloads, and response attributes.

## Prerequisites

- Git.
- The .NET 10 SDK (includes the runtime needed to run tests).
- Network access to `https://dummyjson.com`.

The test project targets .NET 10 LTS.

## Install and Run

### Windows

Install [Git for Windows](https://git-scm.com/download/win) and the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) for Windows. Open PowerShell after installation.

### macOS

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) for macOS (choose the installer matching Apple Silicon or Intel). Install Apple's Command Line Tools if Git is not already available by running `xcode-select --install` in Terminal.

### Clone and Test

In PowerShell on Windows or Terminal on macOS, run:

```sh
git clone https://github.com/fasihk/api-test-automation-framework.git api-test-automation
cd api-test-automation
git --version
dotnet --version
dotnet test src/ApiTests/ApiTests.csproj
```

Confirm `dotnet --version` reports a `10.x` SDK. `dotnet test` automatically restores packages, builds the project, and runs all Gherkin scenarios. Because this repository is private, authenticate with GitHub before cloning it.

Each `dotnet test` run automatically writes the raw results to `TestResults/ApiTests.trx` and generates a self-contained HTML summary. Open `TestResults/latest.html` for the latest run; timestamped reports are archived under `TestResults/ScenarioReports`. The report lists each Gherkin scenario, its pass/fail/skip status, duration, and any failure message. No external reporting tool needs to be installed.

### Run Tagged Scenarios

Each Gherkin scenario has a `@challenge` tag and a resource tag (`@auth`, `@users`, `@products`, or `@carts`). NUnit exposes these Reqnroll tags as `TestCategory` filters. Run the entire challenge or only one resource from PowerShell or macOS Terminal:

```sh
dotnet test src/ApiTests/ApiTests.csproj --filter "TestCategory=challenge"
dotnet test src/ApiTests/ApiTests.csproj --filter "TestCategory=products"
```

To run one scenario by its generated NUnit name, first list the names, then filter by `FullyQualifiedName`:

```sh
dotnet test src/ApiTests/ApiTests.csproj --list-tests
dotnet test src/ApiTests/ApiTests.csproj --filter "FullyQualifiedName~CountUsersWithRedHair"
```

The filtered run still writes the normal TRX and HTML reports; they contain only the selected scenarios.

You can also run one scenario from an IDE. In Visual Studio, open `ApiTestAutomation.sln`, build the solution, open **Test Explorer**, search for the scenario, and select **Run**. In VS Code, install the C# Dev Kit, open the **Testing** view, build/discover tests, then run the desired scenario. The scenarios appear as NUnit tests generated from their Gherkin names.

## Architecture

DummyJSON is called directly; the repository contains the test harness, not a local mock or copy of the API.

### Execution Flow

1. Scenarios are authored only in `.feature` files under `src/ApiTests/Features`. `Challenge.feature` contains nine scenarios: two for Auth, two for Users, three for Products, and two for Carts.
2. Reqnroll's build-time generator creates executable test cases from the feature files. Generated `*.feature.cs` files are build artifacts and should not be edited.
3. Reqnroll matches Gherkin steps to C# bindings in `src/ApiTests/StepDefinitions/PublicApiSteps.cs`. These bindings send requests and assert response status and JSON values.
4. `src/ApiTests/Clients/ApiClient.cs` sends HTTP requests using .NET `HttpClient`. `src/ApiTests/Configuration/TestSettings.cs` supplies the base URL, which defaults to `https://dummyjson.com` and can be overridden with `DUMMYJSON_BASE_URL`.
5. Authentication scenarios save the access token in Reqnroll's scenario context and use it as a bearer token for `/auth/me`; the token is not shared between scenarios.
6. NUnit is the execution adapter for the generated Reqnroll cases; scenarios are not authored as NUnit test methods. `dotnet test` discovers and runs the generated cases.
7. The test run writes TRX results, then `tools/ScenarioReport` maps generated test names back to Gherkin titles and produces the latest and timestamped HTML reports.

### Dependencies

The following framework and packages are required for the current test pipeline:

- `.NET 10 LTS` provides the supported target framework and runtime used to build and execute this test project.
- `Reqnroll.NUnit` 3.3.4 connects Gherkin scenarios and step bindings to NUnit's test execution model.
- `Reqnroll.Tools.MsBuild.Generation` 3.3.4 generates executable test cases from `.feature` files during the build.
- `NUnit` 5.0.0 provides assertions and test metadata for the generated scenarios and their step bindings.
- `NUnit3TestAdapter` 6.3.1 exposes NUnit test cases to `dotnet test` and Visual Studio's test explorer.
- `Microsoft.NET.Test.Sdk` 18.11.0 provides the .NET test host and build integration used by `dotnet test`.

The HTTP and JSON implementation uses `HttpClient` and `System.Text.Json` from the .NET runtime; no separate HTTP or JSON package is required.
The HTML report generator uses .NET's built-in XML and HTML encoding libraries and adds no third-party package dependency.

### Design and Tradeoffs

- Gherkin is the sole scenario-definition format. C# is used for reusable step bindings and HTTP client behavior.
- `HttpClient` keeps dependencies small. A client is created and disposed per scenario for isolation and simplicity; a larger suite may benefit from managed connection reuse through `IHttpClientFactory`.
- Tests call the live public service, so they require network access and can be affected by service availability or changes in sample data. They are system-level functional tests, not isolated or hermetic tests.
- DummyJSON simulates write operations. For write scenarios, the Gherkin steps compare the response status and relevant fields with explicit expected values; these checks validate the returned response, not persistence.
- **Cart discount interpretation:**  This suite interprets the challenge's phrase "total discounted percentage" as the effective percentage saved across the entire cart, not the sum of the five per-item `discountPercentage` rates. DummyJSON does not return a cart-level percentage field: it returns each product's `total`, `discountedPrice`, and `discountPercentage`, plus cart-level monetary `total` and `discountedTotal`. The test calculates A as the sum of product `total` values, B as the sum of product `discountedPrice` values, and asserts `((A - B) / A) * 100`. For product IDs 1–5 at quantity 1, A is 66.95, B is 58.00, and the result is 13.37%. Because DummyJSON rounds each `discountedPrice`, the assertion allows a 0.01 percentage-point tolerance. This is a documented interpretation, not a percentage value supplied directly by the API.

## Future Improvements

These are opportunities to consider as the suite grows, not blockers for running the current solution:

- Replace per-scenario `HttpClient` construction with `IHttpClientFactory` or a managed shared handler to reuse connections efficiently.
- Introduce typed request/response models and schema checks where useful, reducing reliance on generic JSON-path strings in step bindings.
- Separate live DummyJSON system tests from any future fast, isolated tests, so network or service outages can be distinguished from local regressions.
- Review assertions tied to the live sample dataset, such as the red-hair count and product prices, when DummyJSON changes its data; update expected values intentionally rather than treating service drift as a code defect.
- Add CI runs for supported platforms and publish the generated HTML scenario report as a build artifact.

## Add Scenarios

Add Gherkin scenarios to a `.feature` file under `src/ApiTests/Features`. Reuse the bindings in `PublicApiSteps.cs` where possible; add a C# binding when a scenario needs a new step. Run `dotnet test src/ApiTests/ApiTests.csproj` to execute all scenarios.

## AI Use

Generative AI was used to map the challenge exercises to DummyJSON's documented endpoints, implement the nine Gherkin scenarios and their Reqnroll bindings, and update this documentation. AI assistance was also used to run and refine the scenarios against the live API, including validating the cart discount-percentage calculation. The resulting suite passed all nine scenarios; AI-generated code and assertions should still be reviewed and maintained by the project owner.

## License

No license file is currently included. Add a `LICENSE` file before distributing this project if the intent is to grant usage rights under an open-source license.