using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TPFinal.Api.Application;
using TPFinal.Api.Domain;

namespace TPFinal.Api.Controllers;

/// <summary>
/// Endpoints de autenticación: registro, login y datos del usuario autenticado.
/// </summary>

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth; // Servicio de autenticación

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    /// <summary>
    /// Registra un nuevo usuario.
    /// </summary>
    /// <remarks>
    /// Requiere <c>Username</c> y <c>Password</c>. Si <c>IsAdmin</c> es true, se crea con rol administrador.
    /// </remarks>
    /// <param name="req">Datos de registro.</param>
    /// <returns>Usuario creado (formato MeResponse).</returns>
    /// Post /api/auth/register
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MeResponse>> Register([FromBody] RegisterRequest req)
    {
        try
        {
            var u = await _auth.RegisterAsync(req); // Registro del usuario, validaciones en el service
            var role = u.Type == UserType.Administrator ? "administrator" : "user"; // Determinación del rol

            return Ok(new MeResponse(
                u.Id,
                u.Username,
                u.Fullname,
                role,
                u.AvatarUrl
            )); // Retorno del usuario creado
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message }); // Retorno 400 si hay error en los parámetros
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message }); // Retorno 409 si el usuario ya existe
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message }); // Retorno 500 para otros errores
        }
    }

    /// <summary>
    /// Inicia sesión y devuelve un JWT.
    /// </summary>
    /// <param name="req">Credenciales.</param>
    /// <returns>Token JWT en caso de éxito.</returns>
    /// Post /api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req)
    {
        try
        {
            var token = await _auth.LoginAsync(req); // Intento de login, devuelve el token o null si falla
            if (token is null)
                return Unauthorized(new { error = "Usuario o contraseña inválidos" }); // Retorno 401 si falla

            return Ok(new LoginResponse(token)); // Retorno del token en caso de éxito
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message }); // Retorno 400 si hay error en los parámetros
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message }); // Retorno 500 para otros errores
        }
    }

    /// <summary>
    /// Devuelve la información del usuario autenticado.
    /// </summary>
    /// <remarks>
    /// Requiere token JWT en el header Authorization.
    /// </remarks>
    /// <returns>Datos del usuario (formato MeResponse).</returns>
    /// Get /api/auth/me
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MeResponse>> Me()
    {
        try
        {
            // Tomamos el username desde los claims emitidos en el JWT
            var username = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue(ClaimTypes.Name)
                        ?? User.FindFirstValue("sub"); 

            if (string.IsNullOrWhiteSpace(username)) 
                return Unauthorized(); // No se encontró el username en los claims

            var u = await _auth.GetByUsernameAsync(username); // Búsqueda del usuario por username
            if (u is null) return NotFound(); // Usuario no encontrado

            var role = u.Type == UserType.Administrator ? "administrator" : "user"; // Determinación del rol

            return Ok(new MeResponse(
                u.Id,
                u.Username,
                u.Fullname,
                role,
                u.AvatarUrl
            )); // Retorno de los datos del usuario
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message }); // Retorno 500 para otros errores
        }
    }
}
