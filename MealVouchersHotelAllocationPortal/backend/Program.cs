using System.Text;
using MealVouchersHotelAllocation.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSingleton<Db>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Log in with POST /api/auth/login, then paste only the token here."
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => policy
    .WithOrigins("http://localhost:5500", "http://127.0.0.1:5500", "http://localhost:5173", "http://127.0.0.1:5173")
    .AllowAnyHeader().AllowAnyMethod()));

var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
        ValidIssuer = jwt["Issuer"], ValidAudience = jwt["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!))
    };
});
builder.Services.AddAuthorization(options =>
    options.AddPolicy("Admin", policy => policy.RequireAssertion(ctx =>
        ctx.User.Claims.Any(c => (c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "role") && c.Value == "Admin"))));

var app = builder.Build();

// Create the database, tables and the development login automatically on every start (safe to repeat).
try
{
    await DbInitializer.InitializeAsync(app.Configuration, app.Logger);
}
catch (Exception ex)
{
    app.Logger.LogError(ex,
        "DATABASE SETUP FAILED. Check that SQL Server is running and that ConnectionStrings:DefaultConnection in backend/appsettings.json is correct (named instance example: Server=localhost\\SQLEXPRESS;...). The portal will open, but login will not work until this is fixed.");
}

var frontendPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "frontend"));
if (!Directory.Exists(frontendPath))
{
    app.Logger.LogError("Frontend folder not found at {Path}", frontendPath);
}
else
{
    var files = new PhysicalFileProvider(frontendPath);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = files,
        OnPrepareResponse = ctx => ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store" // always load the latest app.js/styles.css
    });
}

app.UseSwagger();
app.UseSwaggerUI(o => o.DocumentTitle = "Meal Vouchers & Hotel Allocation API");
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new { status = "running" }));
app.Run();
