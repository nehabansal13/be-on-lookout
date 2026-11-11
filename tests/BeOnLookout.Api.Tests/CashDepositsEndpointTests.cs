using System.Net;
using System.Net.Http.Json;
using BeOnLookout.Api.Data;
using BeOnLookout.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BeOnLookout.Api.Tests;

public sealed class CashDepositsEndpointTests
{
    private static readonly CashDeposit ExpectedDeposit = new(
        42,
        7,
        "TEST-CUSTOMER-1",
        12500.00m,
        new DateTimeOffset(2026, 11, 7, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task GetById_WhenDepositExists_ReturnsDepositAndOk()
    {
        using var factory = CreateFactory(new StubCashDepositRepository(ExpectedDeposit));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/cash-deposits/42");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            ExpectedDeposit,
            await response.Content.ReadFromJsonAsync<CashDeposit>());
    }

    [Fact]
    public async Task GetById_WhenDepositDoesNotExist_ReturnsNotFound()
    {
        using var factory = CreateFactory(new StubCashDepositRepository(null));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/cash-deposits/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetByCustomerId_WhenCustomerHasDeposits_ReturnsDeposits()
    {
        var deposits = new[] { ExpectedDeposit };
        using var factory = CreateFactory(new StubCashDepositRepository(null, deposits));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/customers/7/cash-deposits");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(deposits, await response.Content.ReadFromJsonAsync<CashDeposit[]>());
    }

    [Fact]
    public async Task GetByCustomerId_WhenCustomerHasNoDeposits_ReturnsEmptyList()
    {
        using var factory = CreateFactory(
            new StubCashDepositRepository(null, Array.Empty<CashDeposit>()));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/customers/7/cash-deposits");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var deposits = await response.Content.ReadFromJsonAsync<CashDeposit[]>();
        Assert.NotNull(deposits);
        Assert.Empty(deposits);
    }

    [Fact]
    public async Task GetByCustomerId_WhenCustomerDoesNotExist_ReturnsNotFound()
    {
        using var factory = CreateFactory(new StubCashDepositRepository(null));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/customers/999/cash-deposits");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetCustomerSummary_WhenCustomerExists_ReturnsSummary()
    {
        var fromUtc = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
        var toUtc = new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero);
        var summary = new CashDepositSummary(7, fromUtc, toUtc, 2, 650.00m);
        using var factory = CreateFactory(new StubCashDepositRepository(null, summary: summary));
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/customers/7/cash-deposits/summary?fromUtc=2026-11-01T00:00:00Z&toUtc=2026-12-01T00:00:00Z");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(summary, await response.Content.ReadFromJsonAsync<CashDepositSummary>());
    }

    [Fact]
    public async Task GetCustomerSummary_WhenNoDepositsMatch_ReturnsZeroTotals()
    {
        var fromUtc = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);
        var toUtc = new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero);
        var summary = new CashDepositSummary(7, fromUtc, toUtc, 0, 0m);
        using var factory = CreateFactory(new StubCashDepositRepository(null, summary: summary));
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/customers/7/cash-deposits/summary?fromUtc=2026-11-01T00:00:00Z&toUtc=2026-12-01T00:00:00Z");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(summary, await response.Content.ReadFromJsonAsync<CashDepositSummary>());
    }

    [Fact]
    public async Task GetCustomerSummary_WhenDateRangeIsInvalid_ReturnsBadRequest()
    {
        using var factory = CreateFactory(new StubCashDepositRepository(null));
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/customers/7/cash-deposits/summary?fromUtc=2026-12-01T00:00:00Z&toUtc=2026-11-01T00:00:00Z");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCustomerSummary_WhenCustomerDoesNotExist_ReturnsNotFound()
    {
        using var factory = CreateFactory(new StubCashDepositRepository(null));
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/customers/999/cash-deposits/summary?fromUtc=2026-11-01T00:00:00Z&toUtc=2026-12-01T00:00:00Z");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WhenCustomerExists_ReturnsCreatedDeposit()
    {
        using var factory = CreateFactory(new StubCashDepositRepository(ExpectedDeposit));
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/cash-deposits",
            new CreateCashDepositRequest { CustomerId = 7, Amount = 12500.00m });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(
            ExpectedDeposit,
            await response.Content.ReadFromJsonAsync<CashDeposit>());
    }

    [Fact]
    public async Task Create_WhenCustomerDoesNotExist_ReturnsNotFound()
    {
        using var factory = CreateFactory(new StubCashDepositRepository(null));
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/cash-deposits",
            new CreateCashDepositRequest { CustomerId = 999, Amount = 12500.00m });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        ICashDepositRepository repository) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICashDepositRepository>();
                services.AddSingleton(repository);
            }));

    private sealed class StubCashDepositRepository(
        CashDeposit? deposit,
        IReadOnlyList<CashDeposit>? customerDeposits = null,
        CashDepositSummary? summary = null) : ICashDepositRepository
    {
        private readonly IReadOnlyList<CashDeposit>? _customerDeposits = customerDeposits;
        private readonly CashDepositSummary? _summary = summary;

        public Task<CashDeposit?> CreateAsync(
            CreateCashDepositRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(deposit);

        public Task<IReadOnlyList<CashDeposit>> GetLatestAsync(
            int take,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<CashDeposit>?> GetByCustomerIdAsync(
            long customerId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_customerDeposits);

        public Task<CashDepositSummary?> GetSummaryByCustomerIdAsync(
            long customerId,
            DateTimeOffset fromUtc,
            DateTimeOffset toUtc,
            CancellationToken cancellationToken) =>
            Task.FromResult(_summary);

        public Task<CashDeposit?> GetByIdAsync(
            long id,
            CancellationToken cancellationToken) =>
            Task.FromResult(deposit?.Id == id ? deposit : null);
    }
}
