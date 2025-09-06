using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TPFinal.Api.Domain;
using TPFinal.Api.Infrastructure;

namespace TPFinal.Api.Application;

/// <summary>
/// Implementación de servicio de autenticación de usuarios.
/// </summary>
/// <remarks>
/// Proporciona métodos para registrar nuevos usuarios, iniciar sesión con credenciales y obtener usuarios
/// por nombre de usuario. Utiliza JWT para generar tokens de autenticación y maneja el hashing seguro de contraseñas.
/// </remarks>
public class AuthService : IAuthService
{
    private readonly AppDbContext _db; // Contexto de la base de datos
    private readonly IConfiguration _cfg; // Configuración de la aplicación (JWT)
    private readonly ILogger<AuthService> _logger; // Logger para registrar eventos e información

    public AuthService(AppDbContext db, IConfiguration cfg, ILogger<AuthService> logger)
    {
        _db = db;
        _cfg = cfg;
        _logger = logger;
    }

    /// <summary>
    /// Registra un nuevo usuario en el sistema.
    /// </summary>
    /// <remarks>
    /// Valida la unicidad del nombre de usuario, hashea la contraseña y guarda el nuevo registro en la base de datos.
    /// Lanza excepciones si los parámetros son inválidos o si el usuario ya existe.
    /// </remarks>
    public async Task<User> RegisterAsync(RegisterRequest req)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(req.Username))
                throw new ArgumentException("Username requerido"); // Validación básica

            if (string.IsNullOrWhiteSpace(req.Password))
                throw new ArgumentException("Password requerido"); // Validación básica

            var exists = await _db.Users.AnyAsync(u => u.Username == req.Username);
            if (exists)
                throw new InvalidOperationException("El usuario ya existe"); // Validación de unicidad

            var (hash, salt) = PasswordHasher.Hash(req.Password); // Hasheo de contraseña


            var isFirstUser = !await _db.Users.AnyAsync(); // Detecta si es el primer User
            var role = isFirstUser ? UserType.Administrator : req.IsAdmin ? UserType.Administrator : UserType.BaseUser;
            var user = new User(
                username: req.Username,
                fullname: req.FullName ?? "",
                passwordHash: hash,
                passwordSalt: salt,
                type: role
            ); // Creación del usuario

            _db.Users.Add(user); // Agregado a la base de datos
            await _db.SaveChangesAsync(); // Guardado de cambios

            return user; // Retorno del usuario creado
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar usuario {Username}", req.Username); // Log de error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }
    }

    /// <summary>
    /// Inicia sesión en el sistema y genera un token JWT.
    /// </summary>
    /// <remarks>
    /// Verifica las credenciales proporcionadas. Si el usuario y la contraseña son válidos,
    /// genera un token JWT con los claims correspondientes. Retorna null si las credenciales son incorrectas.
    /// </remarks>
    public async Task<string?> LoginAsync(LoginRequest req)
    {
        try
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username); // Búsqueda del usuario
            if (user is null)
                return null; // Usuario no encontrado

            if (!PasswordHasher.Verify(req.Password, user.PasswordHash, user.PasswordSalt))
                return null; // Contraseña incorrecta

            return GenerateJwt(user); // Generación y retorno del token JWT
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al intentar login para {Username}", req.Username); // Log de error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }
    }

    /// <summary>
    /// Obtiene un usuario por su nombre de usuario.
    /// </summary>
    /// <remarks>
    /// Retorna null si no se encuentra ningún usuario con el nombre especificado.
    /// </remarks>
    public async Task<User?> GetByUsernameAsync(string username)
    {
        try
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.Username == username); // Búsqueda del usuario
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar usuario por username {Username}", username); // Log de error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }
    }

    /// <summary>
    /// Genera un token JWT para el usuario autenticado.
    /// </summary>
    /// <remarks>
    /// El token incluye claims con el identificador único del usuario (uid),
    /// el nombre de usuario y el rol asignado (administrator o user).
    /// Expira después de 8 horas.
    /// </remarks>
    private string GenerateJwt(User user)
    {
        try
        {
            var issuer = _cfg["Jwt:Issuer"];
            var audience = _cfg["Jwt:Audience"];
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256); // Algoritmo de firma HMAC SHA256

            var role = user.Type == UserType.Administrator ? "administrator" : "user"; // Rol basado en el tipo de usuario

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("uid", user.Id.ToString()),
                new Claim(ClaimTypes.Role, role),
            }; // Claims del token

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            ); // Creación del token

            return new JwtSecurityTokenHandler().WriteToken(token); // Retorno del token serializado
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando JWT para usuario {UserId}", user.Id); // Log de error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }
    }
}
