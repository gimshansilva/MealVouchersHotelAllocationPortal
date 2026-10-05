using Dapper;
using MealVouchersHotelAllocation.Api.Security;
using Microsoft.Data.SqlClient;

namespace MealVouchersHotelAllocation.Api.Data;

public static class DbInitializer
{
    public const string DevUsername = "pramodi";
    public const string DevPassword = "Password@123";

    public static async Task InitializeAsync(IConfiguration config, ILogger logger)
    {
        var cs = config.GetConnectionString("DefaultConnection")
                 ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is missing.");
        var target = new SqlConnectionStringBuilder(cs);
        var dbName = target.InitialCatalog;
        if (string.IsNullOrWhiteSpace(dbName))
            throw new InvalidOperationException("The connection string must include Database=<name>.");

        // 1. Create the database if it does not exist (connect to master first)
        var master = new SqlConnectionStringBuilder(cs) { InitialCatalog = "master" };
        await using (var conn = new SqlConnection(master.ConnectionString))
        {
            await conn.OpenAsync();
            var quoted = "[" + dbName.Replace("]", "]]") + "]";
            await conn.ExecuteAsync($"IF DB_ID(@name) IS NULL CREATE DATABASE {quoted}", new { name = dbName });
        }

        // 2. Create tables
        await using var db = new SqlConnection(cs);
        await db.OpenAsync();

        // If tables from an older/different script exist with the wrong columns, keep them under a new name
        // (nothing is deleted) and let the correct tables be created below.
        await ArchiveMismatchedTablesAsync(db, logger);
        foreach (var sql in Schema) await db.ExecuteAsync(sql);

        // 3. Seed the development login and sample flights
        var hasUser = await db.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM dbo.Users WHERE Username=@u", new { u = DevUsername });
        if (hasUser == 0)
        {
            await db.ExecuteAsync(
                "INSERT dbo.Users(Username,DisplayName,EmployeeNo,Role,PasswordHash) VALUES(@u,@d,@e,@r,@h)",
                new { u = DevUsername, d = "Pramodi", e = "HR2394", r = "Admin", h = PasswordHasher.Hash(DevPassword) });
            logger.LogInformation("Seeded development login: {User} / {Password}", DevUsername, DevPassword);
        }

        // The development login manages data through the API, so it must be an Admin.
        await db.ExecuteAsync("UPDATE dbo.Users SET Role='Admin' WHERE Username=@u AND Role<>'Admin'", new { u = DevUsername });

        var flights = await db.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM dbo.Flights");
        if (flights == 0)
        {
            await db.ExecuteAsync(@"INSERT dbo.Flights(FlightNo,[From],[To],Pax,Std,Etd) VALUES
                ('UL225','CMB','DXB',245,'2026-02-02T18:40:00','2026-02-02T21:50:00'),
                ('UL225','CMB','SIN',180,'2025-12-30T07:00:00','2025-12-30T08:00:00'),
                ('UL101','CMB','BKK',180,'2026-02-05T07:00:00','2026-02-05T08:00:00')");
        }

        logger.LogInformation("Database '{Db}' is ready.", dbName);
    }

    private static readonly Dictionary<string, string[]> ExpectedColumns = new()
    {
        ["Users"] = new[] { "Id", "Username", "DisplayName", "EmployeeNo", "Role", "PasswordHash", "IsActive", "CreatedAt" },
        ["Flights"] = new[] { "Id", "FlightNo", "From", "To", "Pax", "Std", "Etd", "IsActive" },
        ["MealRequests"] = new[] { "Id", "UserId", "FlightId", "MealsRequired", "MealsIssued", "MealType", "Reason", "Status", "CreatedAt", "UpdatedAt" }
    };

    private static async Task ArchiveMismatchedTablesAsync(SqlConnection db, ILogger logger)
    {
        var mismatched = new List<string>();
        foreach (var (table, expected) in ExpectedColumns)
        {
            var existing = (await db.QueryAsync<string>(
                "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @t",
                new { t = table })).ToList();
            if (existing.Count > 0 && expected.Any(c => !existing.Contains(c, StringComparer.OrdinalIgnoreCase)))
                mismatched.Add(table);
        }
        if (mismatched.Count == 0) return;

        var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        foreach (var table in ExpectedColumns.Keys)   // archive all existing ones so the links between tables stay consistent
        {
            var exists = await db.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM sys.tables WHERE name = @t AND schema_id = SCHEMA_ID('dbo')", new { t = table });
            if (exists == 0) continue;
            var newName = $"{table}_old_{stamp}";
            await db.ExecuteAsync("EXEC sp_rename @o, @n", new { o = $"dbo.{table}", n = newName });
            logger.LogWarning("Table dbo.{Table} had different columns than this app expects. It was kept as dbo.{NewName} and a new dbo.{Table} will be created.", table, newName, table);
        }
    }

    private static readonly string[] Schema =
    {
        @"IF OBJECT_ID('dbo.Users','U') IS NULL
          CREATE TABLE dbo.Users(
            Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            Username NVARCHAR(100) NOT NULL UNIQUE,
            DisplayName NVARCHAR(150) NOT NULL,
            EmployeeNo NVARCHAR(50) NOT NULL,
            Role NVARCHAR(50) NOT NULL,
            PasswordHash NVARCHAR(500) NOT NULL,
            IsActive BIT NOT NULL DEFAULT 1,
            CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME())",
        @"IF OBJECT_ID('dbo.Flights','U') IS NULL
          CREATE TABLE dbo.Flights(
            Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            FlightNo NVARCHAR(30) NOT NULL,
            [From] NVARCHAR(10) NOT NULL,
            [To] NVARCHAR(10) NOT NULL,
            Pax INT NOT NULL,
            Std DATETIME2 NOT NULL,
            Etd DATETIME2 NOT NULL,
            IsActive BIT NOT NULL DEFAULT 1)",
        @"IF OBJECT_ID('dbo.MealRequests','U') IS NULL
          CREATE TABLE dbo.MealRequests(
            Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            UserId INT NOT NULL,
            FlightId INT NOT NULL,
            MealsRequired INT NOT NULL,
            MealsIssued INT NOT NULL DEFAULT 0,
            MealType NVARCHAR(80) NOT NULL,
            Reason NVARCHAR(200) NOT NULL,
            Status NVARCHAR(30) NOT NULL DEFAULT 'Pending',
            CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
            UpdatedAt DATETIME2 NULL,
            FOREIGN KEY(UserId) REFERENCES dbo.Users(Id),
            FOREIGN KEY(FlightId) REFERENCES dbo.Flights(Id))"
    };
}
