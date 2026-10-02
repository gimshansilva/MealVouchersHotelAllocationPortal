using Dapper;
using MealVouchersHotelAllocation.Api.Data;
using MealVouchersHotelAllocation.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealVouchersHotelAllocation.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/flights")]
public sealed class FlightsController(Db db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FlightDto>>> GetFlights()
    {
        using var conn = db.OpenConnection();
        var rows = await conn.QueryAsync<FlightDto>("""
            SELECT Id, FlightNo, [From], [To], Pax, Std, Etd
            FROM Flights WHERE IsActive = 1 ORDER BY Std
        """);
        return Ok(rows);
    }
}
