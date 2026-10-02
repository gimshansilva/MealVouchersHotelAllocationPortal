using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dapper;
using MealVouchersHotelAllocation.Api.Data;
using MealVouchersHotelAllocation.Api.Models;
using MealVouchersHotelAllocation.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace MealVouchersHotelAllocation.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(Db db, IConfiguration config) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Username and password are required." });

        using var conn = db.OpenConnection();
        var user = await conn.QuerySingleOrDefaultAsync<UserRow>("""
            SELECT Id, Username, DisplayName, EmployeeNo, Role, PasswordHash
            FROM Users WHERE Username = @Username AND IsActive = 1
        """, new { request.Username });

        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid username or password." });

        var jwt = config.GetSection("Jwt");
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("employeeNo", user.EmployeeNo)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(jwt["Issuer"], jwt["Audience"], claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(jwt["ExpiresMinutes"] ?? "480")), signingCredentials: creds);

        return Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), new UserDto(user.Id, user.Username, user.DisplayName, user.EmployeeNo, user.Role)));
    }

    private sealed record UserRow(int Id, string Username, string DisplayName, string EmployeeNo, string Role, string PasswordHash);
}
