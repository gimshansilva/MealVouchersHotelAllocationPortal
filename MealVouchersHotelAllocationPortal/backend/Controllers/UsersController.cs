using Dapper;
using MealVouchersHotelAllocation.Api.Data;
using MealVouchersHotelAllocation.Api.Models;
using MealVouchersHotelAllocation.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace MealVouchersHotelAllocation.Api.Controllers;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/users")]
public sealed class UsersController(Db db) : ControllerBase
{
    private static readonly string[] Roles = { "Employee", "Admin" };
    private const string SelectSql = "SELECT Id, Username, DisplayName, EmployeeNo, Role, IsActive FROM Users";

    private int CurrentUserId =>
        int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value, out var id) ? id : 0;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminUserDto>>> GetAll()
    {
        using var conn = db.OpenConnection();
        return Ok(await conn.QueryAsync<AdminUserDto>(SelectSql + " ORDER BY Id"));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdminUserDto>> GetOne(int id)
    {
        using var conn = db.OpenConnection();
        var user = await conn.QuerySingleOrDefaultAsync<AdminUserDto>(SelectSql + " WHERE Id = @Id", new { Id = id });
        return user is null ? NotFound(new { message = "User not found." }) : Ok(user);
    }

    // POST /api/users - add a user (the password is hashed before it is stored)
    [HttpPost]
    public async Task<ActionResult<AdminUserDto>> Create([FromBody] CreateUserRequest request)
    {
        var username = (request.Username ?? "").Trim();
        var displayName = (request.DisplayName ?? "").Trim();
        var employeeNo = (request.EmployeeNo ?? "").Trim();
        var role = ResolveRole(request.Role);

        if (username.Length == 0 || username.Length > 100) return BadRequest(new { message = "Username is required (max 100 characters)." });
        if (displayName.Length == 0 || displayName.Length > 150) return BadRequest(new { message = "DisplayName is required (max 150 characters)." });
        if (employeeNo.Length == 0 || employeeNo.Length > 50) return BadRequest(new { message = "EmployeeNo is required (max 50 characters)." });
        if (role is null) return BadRequest(new { message = "Role must be Employee or Admin." });
        var passwordError = ValidatePassword(request.Password);
        if (passwordError != null) return BadRequest(new { message = passwordError });

        using var conn = db.OpenConnection();
        var exists = await conn.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Users WHERE Username = @Username", new { Username = username });
        if (exists > 0) return Conflict(new { message = "That username already exists." });

        try
        {
            var id = await conn.ExecuteScalarAsync<int>(
                "INSERT INTO Users (Username, DisplayName, EmployeeNo, Role, PasswordHash) OUTPUT INSERTED.Id VALUES (@Username, @DisplayName, @EmployeeNo, @Role, @PasswordHash)",
                new { Username = username, DisplayName = displayName, EmployeeNo = employeeNo, Role = role, PasswordHash = PasswordHasher.Hash(request.Password) });
            var created = await conn.QuerySingleAsync<AdminUserDto>(SelectSql + " WHERE Id = @Id", new { Id = id });
            return Created($"/api/users/{id}", created);
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            return Conflict(new { message = "That username already exists." });
        }
    }

    // PUT /api/users/{id} - change name, employee number, role, or active state
    [HttpPut("{id:int}")]
    public async Task<ActionResult<AdminUserDto>> Update(int id, [FromBody] UpdateUserRequest request)
    {
        var displayName = (request.DisplayName ?? "").Trim();
        var employeeNo = (request.EmployeeNo ?? "").Trim();
        var role = ResolveRole(request.Role);

        if (displayName.Length == 0 || displayName.Length > 150) return BadRequest(new { message = "DisplayName is required (max 150 characters)." });
        if (employeeNo.Length == 0 || employeeNo.Length > 50) return BadRequest(new { message = "EmployeeNo is required (max 50 characters)." });
        if (role is null) return BadRequest(new { message = "Role must be Employee or Admin." });
        if (id == CurrentUserId && (role != "Admin" || !request.IsActive))
            return BadRequest(new { message = "You cannot remove your own admin access or deactivate your own account." });

        using var conn = db.OpenConnection();
        var rows = await conn.ExecuteAsync(
            "UPDATE Users SET DisplayName=@DisplayName, EmployeeNo=@EmployeeNo, Role=@Role, IsActive=@IsActive WHERE Id=@Id",
            new { Id = id, DisplayName = displayName, EmployeeNo = employeeNo, Role = role, request.IsActive });
        if (rows == 0) return NotFound(new { message = "User not found." });
        return Ok(await conn.QuerySingleAsync<AdminUserDto>(SelectSql + " WHERE Id = @Id", new { Id = id }));
    }

    // POST /api/users/{id}/reset-password
    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest request)
    {
        var passwordError = ValidatePassword(request.Password);
        if (passwordError != null) return BadRequest(new { message = passwordError });

        using var conn = db.OpenConnection();
        var rows = await conn.ExecuteAsync("UPDATE Users SET PasswordHash=@PasswordHash WHERE Id=@Id",
            new { Id = id, PasswordHash = PasswordHasher.Hash(request.Password) });
        return rows == 0 ? NotFound(new { message = "User not found." }) : NoContent();
    }

    // DELETE /api/users/{id} - deactivates the user (they can no longer log in; their history is kept)
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id)
    {
        if (id == CurrentUserId) return BadRequest(new { message = "You cannot deactivate your own account." });
        using var conn = db.OpenConnection();
        var rows = await conn.ExecuteAsync("UPDATE Users SET IsActive = 0 WHERE Id = @Id", new { Id = id });
        return rows == 0 ? NotFound(new { message = "User not found." }) : NoContent();
    }

    private static string? ResolveRole(string? role) =>
        Roles.FirstOrDefault(r => r.Equals((role ?? "").Trim(), StringComparison.OrdinalIgnoreCase));

    private static string? ValidatePassword(string? password) =>
        string.IsNullOrEmpty(password) || password.Length < 8 || password.Length > 100
            ? "Password must be 8 to 100 characters."
            : null;
}
