using System.Net;
using System.Net.Http.Json;
using BeOnLookout.Api.Data;
using BeOnLookout.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BeOnLookout.Api.Tests;

public sealed class CustomersEndpointTests
{
    private static readonly Customer ExpectedCustomer = new(
        42,
        "TEST-CUSTOMER-1",
        "Test Customer",
        new DateTimeOffset(2026, 11, 7, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task GetLatest_WhenCustomersExist_ReturnsCustomersInDescendingOrder()
    {
        var customers = new[]
        {
            new Customer(7, "TEST-CUSTOMER-2", "Second Customer", new DateTimeOffset(2026, 11, 7, 11, 0, 0, TimeSpan.Zero)),
            ExpectedCustomer,
        };

        using var factory = CreateFactory(new StubCustomerRepository(customers));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/customers?take=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            customers,
            await response.Content.ReadFromJsonAsync<Customer[]>());
    }

    [Fact]
    public async Task GetById_WhenCustomerExists_ReturnsCustomerAndOk()
    {
        using var factory = CreateFactory(new StubCustomerRepository(ExpectedCustomer));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/customers/42");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            ExpectedCustomer,
            await response.Content.ReadFromJsonAsync<Customer>());
    }

    [Fact]
    public async Task GetById_WhenCustomerDoesNotExist_ReturnsNotFound()
    {
        using var factory = CreateFactory(new StubCustomerRepository((Customer?)null));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/customers/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WhenRequestIsValid_ReturnsCreatedCustomer()
    {
        using var factory = CreateFactory(new StubCustomerRepository(ExpectedCustomer));
        using var client = factory.CreateClient();

        var request = new CreateCustomerRequest
        {
            CustomerReference = "TEST-CUSTOMER-1",
            DisplayName = "Test Customer"
        };

        var response = await client.PostAsJsonAsync("/api/customers", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(
            ExpectedCustomer,
            await response.Content.ReadFromJsonAsync<Customer>());
    }

    private static WebApplicationFactory<Program> CreateFactory(
        ICustomerRepository repository) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICustomerRepository>();
                services.AddSingleton(repository);
            }));

    private sealed class StubCustomerRepository : ICustomerRepository
    {
        private readonly Customer? _customer;
        private readonly IReadOnlyList<Customer> _customers;

        public StubCustomerRepository(Customer? customer)
        {
            _customer = customer;
            _customers = customer is null ? Array.Empty<Customer>() : new[] { customer };
        }

        public StubCustomerRepository(IReadOnlyList<Customer> customers)
        {
            _customers = customers;
        }

        public Task<Customer> CreateAsync(
            CreateCustomerRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(_customer ?? new Customer(42, request.CustomerReference, request.DisplayName, DateTimeOffset.UtcNow));

        public Task<IReadOnlyList<Customer>> GetLatestAsync(
            int take,
            CancellationToken cancellationToken) =>
            Task.FromResult(_customers);

        public Task<Customer?> GetByIdAsync(
            long id,
            CancellationToken cancellationToken) =>
            Task.FromResult(_customer?.Id == id ? _customer : null);
    }
}
