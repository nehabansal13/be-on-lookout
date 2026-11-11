using BeOnLookout.Api.Data;
using BeOnLookout.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;

namespace BeOnLookout.Api.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController : ControllerBase
{
    private readonly ICustomerRepository _repository;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(
        ICustomerRepository repository,
        ILogger<CustomersController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<Customer>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<IReadOnlyList<Customer>>> GetLatest(
        [FromQuery, Range(1, 100)] int take = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _repository.GetLatestAsync(take, cancellationToken));
        }
        catch (SqlException exception)
        {
            return DatabaseUnavailable(exception);
        }
        catch (InvalidOperationException)
        {
            return DatabaseNotConfigured();
        }
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType<Customer>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<Customer>> GetById(
        [FromRoute, Range(1, long.MaxValue)] long id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var customer = await _repository.GetByIdAsync(id, cancellationToken);
            return customer is null ? NotFound() : Ok(customer);
        }
        catch (SqlException exception)
        {
            return DatabaseUnavailable(exception);
        }
        catch (InvalidOperationException)
        {
            return DatabaseNotConfigured();
        }
    }

    [HttpPost]
    [ProducesResponseType<Customer>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<Customer>> Create(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var customer = await _repository.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
        }
        catch (SqlException exception)
        {
            return DatabaseUnavailable(exception);
        }
        catch (InvalidOperationException)
        {
            return DatabaseNotConfigured();
        }
    }

    private ObjectResult DatabaseUnavailable(SqlException exception)
    {
        _logger.LogError(
            "Customer request failed with SQL error number {SqlErrorNumber}.",
            exception.Number);
        return Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Database unavailable",
            detail: "The customer could not be processed because the database is unavailable.");
    }

    private ObjectResult DatabaseNotConfigured()
    {
        _logger.LogError("Customer request failed because the database is not configured.");
        return Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Database not configured",
            detail: "Configure the BankingDatabase connection string before using customer endpoints.");
    }
}
