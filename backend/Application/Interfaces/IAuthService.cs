using TPFinal.Api.Domain;

namespace TPFinal.Api.Application;

/// <summary>
/// Interfaz para el servicio de autenticación.
/// </summary>
/// <remarks>
/// Define los métodos para registrar, iniciar sesión y obtener usuarios por nombre de usuario.
/// </remarks>
public interface IAuthService
{
    Task<User> RegisterAsync(RegisterRequest req); // Registra y devuelve el usuario creado
    Task<string?> LoginAsync(LoginRequest req); // Intenta loguear y devuelve el token JWT o null si falla
    Task<User?> GetByUsernameAsync(string username); // Devuelve el usuario por su nombre de usuario o null si no existe
}
