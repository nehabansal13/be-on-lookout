# be-on-lookout
This is a learning project on how i would design a banking scenario to file CTR cases and use AI to review them for suspicious activities. 

## First project: C# Banking API

The API currently has liveness/readiness checks and endpoints for recording and
reading cash deposits. Customer management, React, and AI assessments are not
implemented yet.

Database access uses ADO.NET through `Microsoft.Data.SqlClient`, not Entity
Framework. The readiness check only runs `SELECT 1`; it does not create tables
or change data. Future transaction queries will use SQL parameters.
An explicit `System.Security.Cryptography.Pkcs` reference upgrades a transitive
dependency of the SQL authentication packages to a patched release.

### Requirements

Install the .NET SDK selected in `global.json`. GitHub and Azure accounts are not
required to start the API or call `/health`. The readiness endpoint requires an
Azure SQL database and an authorized Microsoft Entra account.

### Run the API

From the repository directory, use PowerShell:

```powershell
dotnet restore .\BeOnLookout.slnx
dotnet build .\BeOnLookout.slnx --no-restore
dotnet user-secrets set "ConnectionStrings:BankingDatabase" "Server=(localdb)\BeOnLookout;Database=BeOnLookout;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;" --project .\src\BeOnLookout.Api
dotnet run --project .\src\BeOnLookout.Api --launch-profile http
```

The user-secrets command stores the local development connection string outside
the repository. Re-run it if you previously configured the Azure connection string.
The LocalDB instance and schema must be installed before database-backed endpoints
can be used. To create the schema, open
`database\local\001-create-cash-deposits.sql` in SSMS, connect to
`(localdb)\BeOnLookout` using Windows Authentication, and execute the script.

The API endpoints are:

| Method | Path | Purpose |
| --- | --- | --- |
| `GET` | `/health` | Confirms the API process is running |
| `GET` | `/health/ready` | Checks whether the configured SQL database responds |
| `POST` | `/api/cash-deposits` | Records a deposit and returns `201 Created` |
| `GET` | `/api/cash-deposits?take=50` | Lists up to 100 newest deposits |
| `GET` | `/api/cash-deposits/{id}` | Returns one deposit or `404 Not Found` |

### Run the API tests

From the repository directory, run:

```powershell
dotnet test .\BeOnLookout.slnx
```

The GET-by-ID tests send HTTP requests through an in-memory ASP.NET test server.
They use a fake repository, so they verify the `200` JSON response and `404`
status without connecting to or changing LocalDB.

For example, after starting the API, use another PowerShell terminal:

```powershell
Invoke-RestMethod http://localhost:5180/health
Invoke-RestMethod -Method Post http://localhost:5180/api/cash-deposits `
  -ContentType "application/json" `
  -Body '{"customerReference":"DEMO-1001","amount":12500.00}'
Invoke-RestMethod http://localhost:5180/api/cash-deposits
```

Use fictional references only. The API records amounts but does not decide
whether a transaction is suspicious, determine CTR filing eligibility, or report
anyone to a government agency. Invalid requests return `400`; database failures
return `503`. Press `Ctrl+C` in the API terminal to stop the application.

### Optional: switch to Azure SQL later

When you are ready to test against Azure, replace the local connection string in
user secrets with the Azure SQL connection string. The database uses Microsoft
Entra-only authentication. No SQL username or password is needed. Browser sign-in
to Azure Portal does not sign in the local API.

1. Install the latest stable Azure CLI if it is not already installed.
2. In the PowerShell terminal that will run the API, use a separate CLI profile
   for this learning project so other Azure accounts on the machine are untouched:

   ```powershell
   $env:AZURE_CONFIG_DIR = Join-Path $env:LOCALAPPDATA 'BeOnLookout\AzureCli'
   $env:AZURE_TOKEN_CREDENTIALS = 'AzureCliCredential'
   az login --tenant YOUR_TENANT_ID --use-device-code
   ```

   Replace `YOUR_TENANT_ID` with the Directory (tenant) ID from your Azure
   subscription. Complete sign-in yourself using the same account that connected
   in the portal Query editor. Keep these environment variables set when running
   the API; they must be set again in a new terminal.

3. Configure the Azure connection outside the repository using .NET user secrets:

   ```powershell
   dotnet user-secrets set "ConnectionStrings:BankingDatabase" "Server=tcp:YOUR_SERVER.database.windows.net,1433;Database=YOUR_DATABASE;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connect Timeout=10;ConnectRetryCount=0;" --project .\src\BeOnLookout.Api
   ```

   Use the server name and database name shown on your database Overview page.
   Apply the database schema to Azure SQL before starting the API.
   The Azure authentication extension package enables the connection string's
   Entra authentication mode. `AZURE_TOKEN_CREDENTIALS` limits local authentication
   to your isolated Azure CLI login rather than another cached developer account.
   User secrets are local development configuration, not an encrypted vault.

4. On the SQL logical server's **Networking** page, ensure your current public
   IP is allowed and save changes if needed. Keep broad access for Azure services
   disabled. A successful portal Query editor connection alone does not verify
   access from your local API.
5. Run the API from the same authenticated terminal:

   ```powershell
   dotnet run --project .\src\BeOnLookout.Api --launch-profile http
   ```

6. In another terminal, check:

   ```powershell
   Invoke-WebRequest http://localhost:5180/health/ready
   ```

   A successful SQL connection and query return HTTP `200` with `Healthy`.
   Missing configuration, denied access, or an unavailable database return HTTP
   `503` with `Unhealthy`. `/health` remains HTTP `200` while the API is running,
   even when SQL is unavailable.

For deployment later, supply `ConnectionStrings__BankingDatabase` through the
hosting environment and configure managed identity access separately. Do not copy
developer CLI credentials or user-secrets files to Azure.

### Understand the files

| File | Purpose |
| --- | --- |
| `BeOnLookout.slnx` | Groups the project's .NET projects into one solution |
| `global.json` | Selects the .NET SDK used to build the solution |
| `src\BeOnLookout.Api\BeOnLookout.Api.csproj` | Targets .NET 10 and the ASP.NET Core Web SDK |
| `src\BeOnLookout.Api\Program.cs` | Starts the application and maps liveness and readiness endpoints |
| `src\BeOnLookout.Api\Health\DatabaseHealthCheck.cs` | Opens an ADO.NET connection and executes a read-only SQL probe |
| `src\BeOnLookout.Api\Controllers\CashDepositsController.cs` | Defines the cash deposit `GET` and `POST` HTTP endpoints |
| `src\BeOnLookout.Api\Data\CashDepositRepository.cs` | Runs parameterized ADO.NET queries for cash deposits |
| `src\BeOnLookout.Api\Models\CreateCashDepositRequest.cs` | Defines and validates the deposit request body |
| `src\BeOnLookout.Api\Models\CashDeposit.cs` | Defines the deposit returned by the API |
| `database\local\001-create-cash-deposits.sql` | Creates the local learning database and cash deposit table |
| `src\BeOnLookout.Api\Properties\launchSettings.json` | Sets local development ports and environment |
| `src\BeOnLookout.Api\appsettings.json` | Configures application and framework logging |
| `.gitignore` | Excludes generated build output and local environment files from Git |

### Understand the C# startup code

1. `WebApplication.CreateBuilder(args)` loads configuration, logging, and services.
2. `AddControllers()` enables controller-based API routes; dependency injection
   supplies the ADO.NET repository to the controller.
3. `AddHealthChecks().AddCheck<DatabaseHealthCheck>(...)` registers the SQL
   check with the `ready` tag and a 15-second timeout.
4. `builder.Build()` creates the application.
5. `/health` excludes dependency checks; `/health/ready` runs checks tagged `ready`.
6. `MapControllers()` exposes the routes declared by controller attributes.
7. `app.Run()` starts the server and waits for requests.

Readiness creates and disposes a SQL connection and command on each probe;
SqlClient manages connection pooling. Connection and command timeouts bound waits.
The fixed `SELECT 1` probe needs no parameters because it accepts no user input.
It verifies connectivity, not transaction-table permissions or schema.

Deposit endpoints open and dispose a connection per operation. SQL values are
passed as typed parameters rather than concatenated into SQL. The list endpoint
is bounded to at most 100 results. SQL Server records the deposit timestamp in
UTC. The same ADO.NET code can later target Azure SQL by changing the local
connection configuration; the database schema script will also need to be applied
to that Azure database.

Request/startup logs use ASP.NET Core's built-in logging. Readiness failures log
a configuration warning or exception type, without connection strings, SQL error
details, tokens, or credentials. Public responses expose only health status.

### Review workflow

Changes are made on user branches and proposed through pull requests to `main`.
The next learning step begins only after the current pull request has been reviewed
and approved. This starter does not configure GitHub branch-protection rules.
