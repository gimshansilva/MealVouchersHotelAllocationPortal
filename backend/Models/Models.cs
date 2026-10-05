namespace MealVouchersHotelAllocation.Api.Models;

public sealed record LoginRequest(string Username, string Password);
public sealed record UserDto(int Id, string Username, string DisplayName, string EmployeeNo, string Role);
public sealed record LoginResponse(string Token, UserDto User);

public sealed record FlightDto(int Id, string FlightNo, string From, string To, int Pax, DateTime Std, DateTime Etd);
public sealed record MealRequestDto(int Id, int FlightId, string FlightNo, string From, string To, int Pax, DateTime Std, DateTime Etd, string MealType, int MealsRequired, int MealsIssued, string Reason, string Status);
public sealed record CreateMealRequest(int FlightId, int MealsRequired, string MealType, string Reason);
public sealed record UpdateMealRequest(int MealsRequired);
public sealed record SaveFlightRequest(string FlightNo, string From, string To, int Pax, DateTime Std, DateTime Etd);

public sealed record AdminUserDto(int Id, string Username, string DisplayName, string EmployeeNo, string Role, bool IsActive);
public sealed record CreateUserRequest(string Username, string DisplayName, string EmployeeNo, string Role, string Password);
public sealed record UpdateUserRequest(string DisplayName, string EmployeeNo, string Role, bool IsActive);
public sealed record ResetPasswordRequest(string Password);

public sealed record AdminMealRequestDto(int Id, int UserId, string Username, int FlightId, string FlightNo, string From, string To, string MealType, int MealsRequired, int MealsIssued, string Reason, string Status);
public sealed record AdminCreateMealRequest(int UserId, int FlightId, int MealsRequired, int MealsIssued, string MealType, string Reason, string? Status);
public sealed record AdminUpdateMealRequest(int MealsRequired, int MealsIssued, string MealType, string Reason, string Status);
