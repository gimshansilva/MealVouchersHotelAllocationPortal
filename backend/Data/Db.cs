using System.Data;
using Microsoft.Data.SqlClient;

namespace MealVouchersHotelAllocation.Api.Data;

public sealed class Db(IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("DefaultConnection is missing.");

    public IDbConnection OpenConnection() => new SqlConnection(_connectionString);
}
