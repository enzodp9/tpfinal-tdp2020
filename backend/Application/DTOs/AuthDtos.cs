namespace TPFinal.Api.Application;

/// <summary>
/// DTOs relacionados con la autenticación.
/// </summary>
/// <remarks>
/// Estos DTOs se utilizan para transferir datos de autenticación entre diferentes capas de la aplicación.
/// Se diferencia de los DTOs de usuarios que están en UserDtos.cs en que estos DTOs son para la
/// autenticación de usuarios (registro, login, info del usuario logueado)
/// y los otros para la gestión general de usuarios.
/// </remarks>
public record RegisterRequest(string Username, string FullName, string Password, bool IsAdmin = false); // DTO para el registro de usuarios
public record LoginRequest(string Username, string Password); // DTO para el inicio de sesión
public record LoginResponse(string Token); // DTO para la respuesta del inicio de sesión (token JWT)

public record MeResponse(
    Guid Id,
    string Username,
    string FullName,
    string Role,          
    string? AvatarUrl     
); // DTO para la respuesta del endpoint /me que devuelve info del usuario logueado