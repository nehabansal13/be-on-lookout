using BeOnLookout.Api.Health;
using BeOnLookout.Api.Data;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>(
        "banking-database",
        tags: ["ready"],
        timeout: TimeSpan.FromSeconds(15));
builder.Services.AddScoped<ICashDepositRepository, CashDepositRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();

var app = builder.Build();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapControllers();

app.Run();

public partial class Program { }
