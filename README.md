# DummyJSON API Functional System Tests

This project contains Gherkin-defined functional system tests for DummyJSON. The public API is the system under test; scenarios send real HTTP requests and validate status codes and response payloads.

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

In PowerShell on Windows or Terminal on macOS, replace `<your-repository-url>` with the clone URL for your GitHub repository, then run:

```powershell
git clone <your-repository-url> api-test-automation
cd api-test-automation
dotnet --version
dotnet restore
dotnet test src/ApiTests/ApiTests.csproj
```

Confirm `dotnet --version` reports a `10.x` SDK. `dotnet test` restores packages, builds the project, and runs all Gherkin scenarios.

Test results and step output are shown by the test runner. To also write a Visual Studio Test Results file under `TestResults`, run:

```powershell
dotnet test src/ApiTests/ApiTests.csproj --logger "trx;LogFileName=results.trx"
```

No separate HTML reporter or reporting service is configured.

## Architecture

DummyJSON is called directly; the repository contains the test harness, not a local mock or copy of the API.

### Execution Flow

1. Scenarios are authored only in `.feature` files under `src/ApiTests/Features`. The current `Posts.feature` covers retrieving, searching, limiting/selecting, adding, updating, partially updating, and deleting posts.
2. Reqnroll's build-time generator creates executable test cases from the feature files. Generated `*.feature.cs` files are build artifacts and should not be edited.
3. Reqnroll matches Gherkin steps to C# bindings in `src/ApiTests/StepDefinitions/PublicApiSteps.cs`. These bindings send requests and assert response status and JSON values.
4. `src/ApiTests/Clients/ApiClient.cs` sends HTTP requests using .NET `HttpClient`. `src/ApiTests/Configuration/TestSettings.cs` supplies the base URL, which defaults to `https://dummyjson.com` and can be overridden with `DUMMYJSON_BASE_URL`.
5. NUnit is the execution adapter for the generated Reqnroll cases; scenarios are not authored as NUnit test methods. `dotnet test` discovers and runs the generated cases.

### Dependencies

The following framework and packages are required for the current test pipeline:

- `.NET 10 LTS` provides the supported target framework and runtime used to build and execute this test project.
- `Reqnroll.NUnit` 3.3.4 connects Gherkin scenarios and step bindings to NUnit's test execution model.
- `Reqnroll.Tools.MsBuild.Generation` 3.3.4 generates executable test cases from `.feature` files during the build.
- `NUnit` 5.0.0 provides assertions and test metadata for the generated scenarios and their step bindings.
- `NUnit3TestAdapter` 6.3.1 exposes NUnit test cases to `dotnet test` and Visual Studio's test explorer.
- `Microsoft.NET.Test.Sdk` 18.11.0 provides the .NET test host and build integration used by `dotnet test`.

The HTTP and JSON implementation uses `HttpClient` and `System.Text.Json` from the .NET runtime; no separate HTTP or JSON package is required.

### Design and Tradeoffs

- Gherkin is the sole scenario-definition format. C# is used for reusable step bindings and HTTP client behavior.
- `HttpClient` keeps dependencies small. A client is created and disposed per scenario for isolation and simplicity; a larger suite may benefit from managed connection reuse through `IHttpClientFactory`.
- Tests call the live public service, so they require network access and can be affected by service availability or changes in sample data. They are system-level functional tests, not isolated or hermetic tests.
- DummyJSON simulates POST, PUT, PATCH, and DELETE. Each scenario validates only its own response and makes no assumptions that writes persist or affect later requests.

## Add Scenarios

Add Gherkin scenarios to a `.feature` file under `src/ApiTests/Features`. Reuse the bindings in `PublicApiSteps.cs` where possible; add a C# binding when a scenario needs a new step. Run `dotnet test src/ApiTests/ApiTests.csproj` to execute all scenarios.

## AI Use

Generative AI was used as a coding assistant to scaffold the framework, implement the HTTP client and Reqnroll step bindings, draft Gherkin scenarios, and update this documentation. The Gherkin suite was run against DummyJSON. AI-generated code and assertions should be reviewed and maintained by the project owner.

## License

No license file is currently included. Add a `LICENSE` file before distributing this project if you intend to grant usage rights under an open-source license.