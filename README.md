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

## Site Preview

<img width="1512" height="762" alt="1 Login" src="https://github.com/user-attachments/assets/66a36042-c8d7-4b8b-9ee3-88408da29103" />
<img width="1512" height="762" alt="Login" src="https://github.com/user-attachments/assets/9f0559f8-4e91-4723-baeb-4c687f0b5d61" />
<img width="1512" height="762" alt="3 danding view" src="https://github.com/user-attachments/assets/0b4626e7-2eb2-4b3b-a10b-bdddea97415b" />
<img width="1512" height="762" alt="4 select flight" src="https://github.com/user-attachments/assets/ac6d7866-592f-49f1-ada7-1c399488715a" />
<img width="1512" height="762" alt="5 add destails to selected flight" src="https://github.com/user-attachments/assets/b9b791d2-0f21-495a-bfdf-7ed3c071fed6" />
<img width="1512" height="762" alt="6 multiple flights" src="https://github.com/user-attachments/assets/f2ed0327-7712-42f5-97d3-a985e7cf31a2" />
<img width="1512" height="762" alt="7 details added" src="https://github.com/user-attachments/assets/c8474477-9f70-4755-a3bc-ee5f2ba10613" />















































