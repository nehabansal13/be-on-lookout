using BeOnLookout.Api.Data;
using BeOnLookout.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;

namespace BeOnLookout.Api.Controllers;

[ApiController]
[Route("api/cash-deposits")]
public sealed class CashDepositsController : ControllerBase
{
    private readonly ICashDepositRepository _repository;
    private readonly ILogger<CashDepositsController> _logger;

    public CashDepositsController(
        ICashDepositRepository repository,
        ILogger<CashDepositsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CashDeposit>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<IReadOnlyList<CashDeposit>>> GetLatest(
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
    [ProducesResponseType<CashDeposit>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<CashDeposit>> GetById(
        [FromRoute, Range(1, long.MaxValue)] long id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var deposit = await _repository.GetByIdAsync(id, cancellationToken);
            return deposit is null ? NotFound() : Ok(deposit);
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
    [ProducesResponseType<CashDeposit>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<CashDeposit>> Create(
        [FromBody] CreateCashDepositRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var deposit = await _repository.CreateAsync(request, cancellationToken);
            return deposit is null
                ? NotFound()
                : CreatedAtAction(nameof(GetById), new { id = deposit.Id }, deposit);
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
            "Cash deposit request failed with SQL error number {SqlErrorNumber}.",
            exception.Number);
        return Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Database unavailable",
            detail: "The cash deposit could not be processed because the database is unavailable.");
    }

    private ObjectResult DatabaseNotConfigured()
    {
        _logger.LogError("Cash deposit request failed because the database is not configured.");
        return Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: "Database not configured",
            detail: "Configure the BankingDatabase connection string before using cash deposit endpoints.");
    }
}
