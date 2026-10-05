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
    private const string SelectOneSql =
        "SELECT Id, FlightNo, [From], [To], Pax, Std, Etd FROM Flights WHERE Id = @Id AND IsActive = 1";
    private const string InsertSql =
        "INSERT INTO Flights (FlightNo, [From], [To], Pax, Std, Etd) OUTPUT INSERTED.Id VALUES (@FlightNo, @From, @To, @Pax, @Std, @Etd)";

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FlightDto>>> GetFlights()
    {
        using var conn = db.OpenConnection();
        var rows = await conn.QueryAsync<FlightDto>(
            "SELECT Id, FlightNo, [From], [To], Pax, Std, Etd FROM Flights WHERE IsActive = 1 ORDER BY Std");
        return Ok(rows);
    }

    // POST /api/flights  - add one flight
    [Authorize(Policy = "Admin")]
    [HttpPost]
    public async Task<ActionResult<FlightDto>> Create([FromBody] SaveFlightRequest request)
    {
        var flight = Normalize(request);
        var error = Validate(flight);
        if (error != null) return BadRequest(new { message = error });

        using var conn = db.OpenConnection();
        var id = await conn.ExecuteScalarAsync<int>(InsertSql, flight);
        var created = await conn.QuerySingleAsync<FlightDto>(SelectOneSql, new { Id = id });
        return Created($"/api/flights/{id}", created);
    }

    // POST /api/flights/bulk  - add many flights in one call (all or nothing)
    [Authorize(Policy = "Admin")]
    [HttpPost("bulk")]
    public async Task<ActionResult<IEnumerable<FlightDto>>> CreateBulk([FromBody] List<SaveFlightRequest> requests)
    {
        if (requests == null || requests.Count == 0) return BadRequest(new { message = "Send at least one flight." });
        if (requests.Count > 500) return BadRequest(new { message = "A maximum of 500 flights per request is allowed." });

        var flights = requests.Select(Normalize).ToList();
        for (var i = 0; i < flights.Count; i++)
        {
            var error = Validate(flights[i]);
            if (error != null) return BadRequest(new { message = $"Flight #{i + 1}: {error}" });
        }

        using var conn = db.OpenConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        var created = new List<FlightDto>();
        foreach (var flight in flights)
        {
            var id = await conn.ExecuteScalarAsync<int>(InsertSql, flight, tx);
            created.Add(await conn.QuerySingleAsync<FlightDto>(SelectOneSql, new { Id = id }, tx));
        }
        tx.Commit();
        return Created("/api/flights", created);
    }

    // PUT /api/flights/{id}  - change a flight
    [Authorize(Policy = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<FlightDto>> Update(int id, [FromBody] SaveFlightRequest request)
    {
        var flight = Normalize(request);
        var error = Validate(flight);
        if (error != null) return BadRequest(new { message = error });

        using var conn = db.OpenConnection();
        var rows = await conn.ExecuteAsync(
            "UPDATE Flights SET FlightNo=@FlightNo, [From]=@From, [To]=@To, Pax=@Pax, Std=@Std, Etd=@Etd WHERE Id=@Id AND IsActive=1",
            new { Id = id, flight.FlightNo, flight.From, flight.To, flight.Pax, flight.Std, flight.Etd });
        if (rows == 0) return NotFound(new { message = "Flight not found." });
        return Ok(await conn.QuerySingleAsync<FlightDto>(SelectOneSql, new { Id = id }));
    }

    // DELETE /api/flights/{id}  - hides the flight (keeps existing meal requests intact)
    [Authorize(Policy = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var conn = db.OpenConnection();
        var rows = await conn.ExecuteAsync("UPDATE Flights SET IsActive = 0 WHERE Id = @Id AND IsActive = 1", new { Id = id });
        return rows == 0 ? NotFound(new { message = "Flight not found." }) : NoContent();
    }

    private static SaveFlightRequest Normalize(SaveFlightRequest f) => f with
    {
        FlightNo = (f.FlightNo ?? "").Trim().ToUpperInvariant(),
        From = (f.From ?? "").Trim().ToUpperInvariant(),
        To = (f.To ?? "").Trim().ToUpperInvariant()
    };

    private static string? Validate(SaveFlightRequest f)
    {
        if (f.FlightNo.Length == 0 || f.FlightNo.Length > 30) return "FlightNo is required (max 30 characters).";
        if (f.From.Length == 0 || f.From.Length > 10 || f.To.Length == 0 || f.To.Length > 10) return "From and To are required (max 10 characters).";
        if (f.Pax < 1) return "Pax must be at least 1.";
        if (f.Etd < f.Std) return "Etd cannot be earlier than Std.";
        return null;
    }
}
