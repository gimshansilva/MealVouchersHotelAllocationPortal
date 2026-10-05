IF DB_ID(N'MealVouchersHotelAllocation') IS NULL
BEGIN
    CREATE DATABASE MealVouchersHotelAllocation;
END
GO
USE MealVouchersHotelAllocation;
GO

IF OBJECT_ID('dbo.Users','U') IS NULL
BEGIN
    CREATE TABLE dbo.Users(
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Username NVARCHAR(100) NOT NULL UNIQUE,
        DisplayName NVARCHAR(150) NOT NULL,
        EmployeeNo NVARCHAR(50) NOT NULL,
        Role NVARCHAR(50) NOT NULL,
        PasswordHash NVARCHAR(500) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME()
    );
END
GO

IF OBJECT_ID('dbo.Flights','U') IS NULL
BEGIN
    CREATE TABLE dbo.Flights(
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FlightNo NVARCHAR(30) NOT NULL,
        [From] NVARCHAR(10) NOT NULL,
        [To] NVARCHAR(10) NOT NULL,
        Pax INT NOT NULL,
        Std DATETIME2 NOT NULL,
        Etd DATETIME2 NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Flights_IsActive DEFAULT 1
    );
END
GO

IF OBJECT_ID('dbo.MealRequests','U') IS NULL
BEGIN
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
        CONSTRAINT FK_MealRequests_Flights FOREIGN KEY(FlightId) REFERENCES dbo.Flights(Id)
    );
END
GO

-- Bootstrap account for local development only. Replace with the organization's real identity source in production.
-- Password: Password@123
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username='pramodi')
BEGIN
    INSERT dbo.Users(Username,DisplayName,EmployeeNo,Role,PasswordHash)
    VALUES('pramodi','Pramodi','HR2394','Employee','PBKDF2-SHA256$120000$TG9jYWxEZXZTYWx0MTIzNA==$SXT4ZhveH+BEtZnX3faukknP2zAD6MpxuqkQ26KotNQ=');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Flights)
BEGIN
    INSERT dbo.Flights(FlightNo,[From],[To],Pax,Std,Etd) VALUES
    ('UL225','CMB','DXB',245,'2026-02-02T18:40:00','2026-02-02T21:50:00'),
    ('UL225','CMB','SIN',180,'2025-12-30T07:00:00','2025-12-30T08:00:00'),
    ('UL101','CMB','BKK',180,'2026-02-05T07:00:00','2026-02-05T08:00:00');
END
GO
