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

**Swagger:** choose **Run API (open Swagger)** in the Run and Debug dropdown before pressing F5, or open `http://localhost:5080/swagger` while the app is running.

Without VS Code: `.\run.ps1`

## Check that everything works

With the app running (F5), open a PowerShell terminal in the project folder and run `.\scripts\check-api.ps1` (add `-Write` to also test adding a flight). Each call prints PASS or FAIL with the reason.

## Configuration

`backend/appsettings.json` > `ConnectionStrings:DefaultConnection`. For a named instance use e.g. `Server=localhost\SQLEXPRESS;Database=MealVouchersHotelAllocation;Trusted_Connection=True;TrustServerCertificate=True;`

## Troubleshooting

- **"DATABASE SETUP FAILED" in the Debug Console**: SQL Server is not running or the connection string is wrong.
- **Port 5080 used by another program**: close it, or change the port in `.vscode/launch.json`, `backend/Properties/launchSettings.json`, `scripts/free-port.ps1` and `run.ps1`.
- **Login 401**: delete the `pramodi` row in `dbo.Users` and restart; it is recreated on startup.

## API (all except login and health require the JWT)

| Table | Endpoints | Who |
|---|---|---|
| Auth | `POST /api/auth/login`, `GET /api/health` | everyone |
| Flights | `GET /api/flights` | any logged-in user |
| Flights | `POST /api/flights`, `POST /api/flights/bulk`, `PUT|DELETE /api/flights/{id}` | Admin |
| Users | `GET|POST /api/users`, `GET|PUT|DELETE /api/users/{id}`, `POST /api/users/{id}/reset-password` | Admin |
| MealRequests (own) | `GET|POST /api/meal-requests`, `PUT|DELETE /api/meal-requests/{id}` | any logged-in user (portal) |
| MealRequests (all users) | `GET|POST /api/admin/meal-requests`, `GET|PUT|DELETE /api/admin/meal-requests/{id}` | Admin |

The development login `pramodi` is an Admin. Try every endpoint from VS Code with `api-test.http` (REST Client extension), or import flights from JSON with `.\scripts\import-flights.ps1`.

## Scope

Implements the seven supplied Figma screens (Meal Vouchers flow). Hotel Allocation and History are not in the supplied design and are not built. Before production: replace the development login and JWT key and connect real flight data.

## Site Preview
<img width="1512" height="762" alt="1 Login" src="https://github.com/user-attachments/assets/68f24c78-436c-471c-8fd4-1f9512e49ac9" />
<img width="1512" height="762" alt="Login" src="https://github.com/user-attachments/assets/a1396639-e7b7-4a5e-83fc-77b25b789dca" />
<img width="1512" height="762" alt="3 danding view" src="https://github.com/user-attachments/assets/050cd90a-1e40-4bc0-8a67-1a206428633d" />
<img width="1512" height="762" alt="4 select flight" src="https://github.com/user-attachments/assets/57986bf2-521b-4b2c-b08d-97095544408a" />
<img width="1512" height="762" alt="5 add destails to selected flight" src="https://github.com/user-attachments/assets/75169ca9-99d4-42f2-9f06-edfd63ffbfa7" />
<img width="1512" height="762" alt="6 multiple flights" src="https://github.com/user-attachments/assets/2863d3e8-f8ec-40a5-b70f-420ac48cc5cf" />
<img width="1512" height="762" alt="7 details added" src="https://github.com/user-attachments/assets/1eeea655-f2ce-44de-b01d-b3c63e581d87" />

























