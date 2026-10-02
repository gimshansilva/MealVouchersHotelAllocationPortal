namespace MealVouchersHotelAllocation.Api.Models;

public sealed record LoginRequest(string Username, string Password);
public sealed record UserDto(int Id, string Username, string DisplayName, string EmployeeNo, string Role);
public sealed record LoginResponse(string Token, UserDto User);

public sealed record FlightDto(int Id, string FlightNo, string From, string To, int Pax, DateTime Std, DateTime Etd);
public sealed record MealRequestDto(int Id, int FlightId, string FlightNo, string From, string To, int Pax, DateTime Std, DateTime Etd, string MealType, int MealsRequired, int MealsIssued, string Reason, string Status);
public sealed record CreateMealRequest(int FlightId, int MealsRequired, string MealType, string Reason);
public sealed record UpdateMealRequest(int MealsRequired);
