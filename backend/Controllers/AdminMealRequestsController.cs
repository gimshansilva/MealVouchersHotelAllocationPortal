using Dapper;
using MealVouchersHotelAllocation.Api.Data;
using MealVouchersHotelAllocation.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MealVouchersHotelAllocation.Api.Controllers;

// Admin view of ALL meal requests (the portal itself only sees the logged-in user's own requests).
[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/meal-requests")]
public sealed class AdminMealRequestsController(Db db) : ControllerBase
{
    private static readonly string[] Statuses = { "Pending", "Approved", "Issued", "Rejected" };

    private const string SelectSql = @"
        SELECT mr.Id, mr.UserId, u.Username, mr.FlightId, f.FlightNo, f.[From], f.[To],
               mr.MealType, mr.MealsRequired, mr.MealsIssued, mr.Reason, mr.Status
        FROM MealRequests mr
        INNER JOIN Users u ON u.Id = mr.UserId
        INNER JOIN Flights f ON f.Id = mr.FlightId
        WHERE mr.Status <> 'Deleted'";

    // GET /api/admin/meal-requests?userId=1&flightId=2  (both filters optional)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminMealRequestDto>>> GetAll([FromQuery] int? userId, [FromQuery] int? flightId)
    {
        using var conn = db.OpenConnection();
        var rows = await conn.QueryAsync<AdminMealRequestDto>(
            SelectSql + " AND (@UserId IS NULL OR mr.UserId = @UserId) AND (@FlightId IS NULL OR mr.FlightId = @FlightId) ORDER BY mr.Id",
            new { UserId = userId, FlightId = flightId });
        return Ok(rows);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdminMealRequestDto>> GetOne(int id)
    {
        using var conn = db.OpenConnection();
        var row = await conn.QuerySingleOrDefaultAsync<AdminMealRequestDto>(SelectSql + " AND mr.Id = @Id", new { Id = id });
        return row is null ? NotFound(new { message = "Meal request not found." }) : Ok(row);
    }

    // POST /api/admin/meal-requests - add a meal request for any user
    [HttpPost]
    public async Task<ActionResult<AdminMealRequestDto>> Create([FromBody] AdminCreateMealRequest request)
    {
        var status = string.IsNullOrWhiteSpace(request.Status) ? "Pending" : ResolveStatus(request.Status);
        var error = Validate(request.MealsRequired, request.MealsIssued, request.MealType, request.Reason, status);
        if (error != null) return BadRequest(new { message = error });

        using var conn = db.OpenConnection();
        var userOk = await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Users WHERE Id = @UserId AND IsActive = 1", new { request.UserId });
        if (userOk == 0) return BadRequest(new { message = "UserId does not match an active user." });
        var flightOk = await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Flights WHERE Id = @FlightId AND IsActive = 1", new { request.FlightId });
        if (flightOk == 0) return BadRequest(new { message = "FlightId does not match an active flight." });
        var duplicate = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM MealRequests WHERE UserId = @UserId AND FlightId = @FlightId AND Status <> 'Deleted'",
            new { request.UserId, request.FlightId });
        if (duplicate > 0) return Conflict(new { message = "A meal request already exists for this user and flight." });

        var id = await conn.ExecuteScalarAsync<int>(
            "INSERT INTO MealRequests (UserId, FlightId, MealsRequired, MealsIssued, MealType, Reason, Status) OUTPUT INSERTED.Id VALUES (@UserId, @FlightId, @MealsRequired, @MealsIssued, @MealType, @Reason, @Status)",
            new { request.UserId, request.FlightId, request.MealsRequired, request.MealsIssued, MealType = request.MealType.Trim(), Reason = request.Reason.Trim(), Status = status });
        var created = await conn.QuerySingleAsync<AdminMealRequestDto>(SelectSql + " AND mr.Id = @Id", new { Id = id });
        return Created($"/api/admin/meal-requests/{id}", created);
    }

    // PUT /api/admin/meal-requests/{id} - change quantities, meal type, reason or status
    [HttpPut("{id:int}")]
    public async Task<ActionResult<AdminMealRequestDto>> Update(int id, [FromBody] AdminUpdateMealRequest request)
    {
        var status = ResolveStatus(request.Status);
        var error = Validate(request.MealsRequired, request.MealsIssued, request.MealType, request.Reason, status);
        if (error != null) return BadRequest(new { message = error });

        using var conn = db.OpenConnection();
        var rows = await conn.ExecuteAsync(
            "UPDATE MealRequests SET MealsRequired=@MealsRequired, MealsIssued=@MealsIssued, MealType=@MealType, Reason=@Reason, Status=@Status, UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id AND Status <> 'Deleted'",
            new { Id = id, request.MealsRequired, request.MealsIssued, MealType = request.MealType.Trim(), Reason = request.Reason.Trim(), Status = status });
        if (rows == 0) return NotFound(new { message = "Meal request not found." });
        return Ok(await conn.QuerySingleAsync<AdminMealRequestDto>(SelectSql + " AND mr.Id = @Id", new { Id = id }));
    }

    // DELETE /api/admin/meal-requests/{id} - marks the request as deleted
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var conn = db.OpenConnection();
        var rows = await conn.ExecuteAsync(
            "UPDATE MealRequests SET Status='Deleted', UpdatedAt=SYSUTCDATETIME() WHERE Id=@Id AND Status <> 'Deleted'", new { Id = id });
        return rows == 0 ? NotFound(new { message = "Meal request not found." }) : NoContent();
    }

    private static string? ResolveStatus(string? status) =>
        Statuses.FirstOrDefault(s => s.Equals((status ?? "").Trim(), StringComparison.OrdinalIgnoreCase));

    private static string? Validate(int required, int issued, string? mealType, string? reason, string? status)
    {
        if (required < 1) return "MealsRequired must be at least 1.";
        if (issued < 0) return "MealsIssued cannot be negative.";
        if (string.IsNullOrWhiteSpace(mealType) || mealType.Trim().Length > 80) return "MealType is required (max 80 characters).";
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 200) return "Reason is required (max 200 characters).";
        if (status is null) return "Status must be one of: Pending, Approved, Issued, Rejected.";
        return null;
    }
}
