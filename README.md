# Meal Vouchers & Hotel Allocation Portal

Stack: HTML/CSS/JavaScript frontend, ASP.NET Core Web API (.NET 8), Dapper, SQL Server, JWT authentication.
The API also serves the frontend, so one run starts everything at **http://localhost:5080**.

## Run and debug in VS Code (one step)

1. Install the **.NET 8 SDK** and make sure **SQL Server** is running (Windows Authentication, `localhost`).
2. In VS Code: **File > Open Folder** and open this folder (the one containing `backend`, `frontend` and `.vscode`).
3. Install the recommended **C#** extension when prompted.
4. Press **F5** (configuration: "Run Portal (API + Frontend)").

F5 frees port 5080 if an old run is stuck, builds, creates the database/tables/login automatically, starts the API and opens the portal in your browser. Breakpoints in `backend/**/*.cs` work. For frontend debugging use the browser DevTools (F12).

Login: `pramodi` / `Password@123`

Without VS Code: `.\run.ps1`

## Configuration

`backend/appsettings.json` > `ConnectionStrings:DefaultConnection`. For a named instance use e.g. `Server=localhost\SQLEXPRESS;Database=MealVouchersHotelAllocation;Trusted_Connection=True;TrustServerCertificate=True;`

## Troubleshooting

- **"DATABASE SETUP FAILED" in the Debug Console**: SQL Server is not running or the connection string is wrong.
- **Port 5080 used by another program**: close it, or change the port in `.vscode/launch.json`, `backend/Properties/launchSettings.json`, `scripts/free-port.ps1` and `run.ps1`.
- **Login 401**: delete the `pramodi` row in `dbo.Users` and restart; it is recreated on startup.

## API (all except login require the JWT)

- `POST /api/auth/login`, `GET /api/flights`, `GET|POST /api/meal-requests`, `PUT|DELETE /api/meal-requests/{id}`, `GET /api/health`

## Scope

Implements the seven supplied Figma screens (Meal Vouchers flow). Hotel Allocation and History are not in the supplied design and are not built. Before production: replace the development login and JWT key and connect real flight data.
