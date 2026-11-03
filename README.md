# be-on-lookout
This is a learning project on how i would design a banking scenario to file CTR cases and use AI to review them for suspicious activities. 

## First project: C# Banking API

This first pull request contains only an ASP.NET Core application and a health
endpoint. No customers, transactions, database, React frontend, Azure services,
or AI assessments are implemented yet.

When database access is introduced, this project will use ADO.NET with
parameterized SQL commands, not Entity Framework. No database packages are
installed in this foundation.

### Requirements

Install the .NET SDK selected in `global.json`. GitHub and Azure accounts are not
required to run the API locally.

### Run the API

From the repository directory, use PowerShell:

```powershell
dotnet restore .\BeOnLookout.slnx
dotnet build .\BeOnLookout.slnx --no-restore
dotnet run --project .\src\BeOnLookout.Api --launch-profile http
```

In another terminal:

```powershell
Invoke-WebRequest http://localhost:5180/health
```

Expected response: HTTP `200` with the body `Healthy`.
Press `Ctrl+C` in the API terminal to stop the application.

### Understand the files

| File | Purpose |
| --- | --- |
| `BeOnLookout.slnx` | Groups the project's .NET projects into one solution |
| `global.json` | Selects the .NET SDK used to build the solution |
| `src\BeOnLookout.Api\BeOnLookout.Api.csproj` | Targets .NET 10 and the ASP.NET Core Web SDK |
| `src\BeOnLookout.Api\Program.cs` | Starts the application and defines its health endpoint |
| `src\BeOnLookout.Api\Properties\launchSettings.json` | Sets local development ports and environment |
| `src\BeOnLookout.Api\appsettings.json` | Configures application and framework logging |
| `.gitignore` | Excludes generated build output and local environment files from Git |

### Understand the C# startup code

1. `WebApplication.CreateBuilder(args)` loads configuration, logging, and services.
2. `builder.Services.AddHealthChecks()` registers ASP.NET Core's health-check services.
3. `builder.Build()` creates the application.
4. `app.MapHealthChecks("/health")` exposes the registered checks through an HTTP endpoint.
5. `app.Run()` starts the server and waits for requests.

The endpoint is a liveness check only: it does not test database, queue, or AI
connectivity. Request/startup logs use ASP.NET Core's built-in logging.

### Review workflow

Changes are made on user branches and proposed through pull requests to `main`.
The next learning step begins only after the current pull request has been reviewed
and approved. This starter does not configure GitHub branch-protection rules.
