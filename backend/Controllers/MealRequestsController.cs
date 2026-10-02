using Dapper;
using MealVouchersHotelAllocation.Api.Data;
using MealVouchersHotelAllocation.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealVouchersHotelAllocation.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/meal-requests")]
public sealed class MealRequestsController(Db db) : ControllerBase
{
    private int CurrentUserId =>
        int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value, out var id) ? id : 0;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MealRequestDto>>> Get()
    {
        if (CurrentUserId == 0) return Unauthorized();
        using var conn = db.OpenConnection();
        var rows = await conn.QueryAsync<MealRequestDto>(SelectSql + " ORDER BY mr.Id", new { UserId = CurrentUserId });
        return Ok(rows);
    }

    [HttpPost]
    public async Task<ActionResult<MealRequestDto>> Create(CreateMealRequest request)
    {
        if (CurrentUserId == 0) return Unauthorized();
        if (request.MealsRequired < 1) return BadRequest(new { message = "Meals required must be greater than zero." });
        if (string.IsNullOrWhiteSpace(request.MealType) || string.IsNullOrWhiteSpace(request.Reason)) return BadRequest(new { message = "Meal type and reason are required." });
        using var conn = db.OpenConnection();
        var exists = await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM MealRequests WHERE UserId=@UserId AND FlightId=@FlightId AND Status <> 'Deleted'", new { UserId=CurrentUserId, request.FlightId });
        if (exists > 0) return Conflict(new { message = "A meal request already exists for this flight." });
        var id = await conn.ExecuteScalarAsync<int>("""
            INSERT INTO MealRequests(UserId, FlightId, MealsRequired, MealsIssued, MealType, Reason, Status)
            OUTPUT INSERTED.Id VALUES(@UserId,@FlightId,@MealsRequired,0,@MealType,@Reason,'Pending')
        """, new { UserId=CurrentUserId, request.FlightId, request.MealsRequired, request.MealType, request.Reason });
        var row = await conn.QuerySingleAsync<MealRequestDto>(SelectSql + " AND mr.Id=@Id", new { UserId=CurrentUserId, Id=id });
        return Created($"/api/meal-requests/{id}", row);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, UpdateMealRequest request)
    {
        if (request.MealsRequired < 0) return BadRequest(new { message = "Meals required cannot be negative." });
        using var conn = db.OpenConnection();
        var affected = await conn.ExecuteAsync("UPDATE MealRequests SET MealsRequired=@MealsRequired, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id AND UserId=@UserId AND Status <> 'Deleted'", new { request.MealsRequired, Id=id, UserId=CurrentUserId });
        return affected == 0 ? NotFound(new { message = "Meal request not found." }) : NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        using var conn = db.OpenConnection();
        var affected = await conn.ExecuteAsync("UPDATE MealRequests SET Status='Deleted', UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id AND UserId=@UserId", new { Id=id, UserId=CurrentUserId });
        return affected == 0 ? NotFound(new { message = "Meal request not found." }) : NoContent();
    }

    private const string SelectSql = """
        SELECT mr.Id, f.Id AS FlightId, f.FlightNo, f.[From], f.[To], f.Pax, f.Std, f.Etd,
               mr.MealType, mr.MealsRequired, mr.MealsIssued, mr.Reason, mr.Status
        FROM MealRequests mr INNER JOIN Flights f ON f.Id=mr.FlightId
        WHERE mr.UserId=@UserId AND mr.Status <> 'Deleted'
    """;
}
