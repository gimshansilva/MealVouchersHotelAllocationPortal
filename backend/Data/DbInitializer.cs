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
        foreach (var sql in Schema) await db.ExecuteAsync(sql);

        // 3. Seed the development login and sample flights
        var hasUser = await db.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM dbo.Users WHERE Username=@u", new { u = DevUsername });
        if (hasUser == 0)
        {
            await db.ExecuteAsync(
                "INSERT dbo.Users(Username,DisplayName,EmployeeNo,Role,PasswordHash) VALUES(@u,@d,@e,@r,@h)",
                new { u = DevUsername, d = "Pramodi", e = "HR2394", r = "Employee", h = PasswordHasher.Hash(DevPassword) });
            logger.LogInformation("Seeded development login: {User} / {Password}", DevUsername, DevPassword);
        }

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
            IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
            CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME())",
        @"IF OBJECT_ID('dbo.Flights','U') IS NULL
          CREATE TABLE dbo.Flights(
            Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            FlightNo NVARCHAR(30) NOT NULL,
            [From] NVARCHAR(10) NOT NULL,
            [To] NVARCHAR(10) NOT NULL,
            Pax INT NOT NULL,
            Std DATETIME2 NOT NULL,
            Etd DATETIME2 NOT NULL,
            IsActive BIT NOT NULL CONSTRAINT DF_Flights_IsActive DEFAULT 1)",
        @"IF OBJECT_ID('dbo.MealRequests','U') IS NULL
          CREATE TABLE dbo.MealRequests(
            Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            UserId INT NOT NULL,
            FlightId INT NOT NULL,
            MealsRequired INT NOT NULL,
            MealsIssued INT NOT NULL CONSTRAINT DF_MealRequests_MealsIssued DEFAULT 0,
            MealType NVARCHAR(80) NOT NULL,
            Reason NVARCHAR(200) NOT NULL,
            Status NVARCHAR(30) NOT NULL CONSTRAINT DF_MealRequests_Status DEFAULT 'Pending',
            CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_MealRequests_CreatedAt DEFAULT SYSUTCDATETIME(),
            UpdatedAt DATETIME2 NULL,
            CONSTRAINT FK_MealRequests_Users FOREIGN KEY(UserId) REFERENCES dbo.Users(Id),
            CONSTRAINT FK_MealRequests_Flights FOREIGN KEY(FlightId) REFERENCES dbo.Flights(Id))"
    };
}
