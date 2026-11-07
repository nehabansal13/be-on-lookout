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

    private static WebApplicationFactory<Program> CreateFactory(
        ICashDepositRepository repository) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICashDepositRepository>();
                services.AddSingleton(repository);
            }));

    private sealed class StubCashDepositRepository(CashDeposit? deposit)
        : ICashDepositRepository
    {
        public Task<CashDeposit> CreateAsync(
            CreateCashDepositRequest request,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<CashDeposit>> GetLatestAsync(
            int take,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CashDeposit?> GetByIdAsync(
            long id,
            CancellationToken cancellationToken) =>
            Task.FromResult(deposit?.Id == id ? deposit : null);
    }
}
