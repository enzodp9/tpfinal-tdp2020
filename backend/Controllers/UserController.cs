using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TPFinal.Api.Application;
using TPFinal.Api.Infrastructure;
using System.Security.Claims;

namespace TPFinal.Api.Controllers;

/// <summary>
/// Endpoints de administración de usuarios y perfil del usuario autenticado.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _svc;
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public UsersController(IUserService svc, AppDbContext db, IWebHostEnvironment env)
    {
        _svc = svc;
        _db = db;
        _env = env;
    }

    /// <summary>
    /// Extrae el UserId del token JWT.
    /// </summary>
    /// <remarks>
    ///  Devuelve el Guid del usuario o Guid.Empty si no se encuentra o es inválido
    /// (en cuyo caso el endpoint debería rechazar la petición por falta de autorización).
    /// </remarks>
    private Guid GetUserId()
    {
        var uid = User.FindFirstValue("uid");
        return Guid.TryParse(uid, out var g) ? g : Guid.Empty;
    }

    /// <summary>
    /// Lista usuarios con filtro opcional.
    /// </summary>
    /// <remarks>
    /// Si se provee <c>q</c>, filtra por username o fullname que contenga el texto (case insensitive).
    /// Requiere rol administrador.
    /// </remarks>
    /// <param name="q">Texto a buscar en username o fullname (contiene).</param>
    /// <returns>Listado de usuarios.</returns>
    /// GET /api/users
    [HttpGet]
    [Authorize(Roles = "administrator")]
    [ProducesResponseType(typeof(IReadOnlyList<UserDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<UserDetailDto>>> Get([FromQuery] string? q)
    {
        try
        {
            var list = await _svc.GetAllAsync(q); // Llamada al servicio para obtener la lista de usuarios con filtro opcional
            return Ok(list); // Retorno 200 con la lista de usuarios
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message }); // Retorno 400 si hay error en los parámetros
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message }); // Retorno 400 si hay error en los parámetros
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message }); // Retorno 500 para otros errores
        }
    }

    /// <summary>
    /// Obtiene un usuario por su Id.
    /// </summary>
    /// <remarks>
    /// Requiere rol administrador.
    /// </remarks>
    /// <param name="id">Identificador del usuario.</param>
    /// <returns>Datos del usuario o 404 si no existe.</returns>
    /// GET /api/users/{id}
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "administrator")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserDetailDto>> GetById(Guid id)
    {
        try
        {
            var dto = await _svc.GetByIdAsync(id); // Llamada al servicio para obtener el usuario por Id
            return dto is null ? NotFound() : Ok(dto); // Retorno 404 si no se encuentra, 200 con el usuario si se encuentra
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message }); // Retorno 500 para otros errores
        }
    }

    /// <summary>
    /// Crea o actualiza un usuario.
    /// </summary>
    /// <remarks>
    /// Si no se provee <c>id</c>, crea un nuevo usuario. Si se provee, actualiza el usuario existente.
    /// Requiere rol administrador.
    /// </remarks>
    /// <param name="dto">Datos del usuario.</param>
    /// <returns>Usuario creado o actualizado.</returns>
    /// POST /api/users
    [HttpPost]
    [Authorize(Roles = "administrator")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserDetailDto>> Upsert([FromBody] UpsertUserDto dto)
    {
        try
        {
            var userId = dto.Id ?? Guid.Empty; // Si no se provee Id, es creación (Guid.Empty)
            var result = await _svc.UpsertAsync(userId, dto); // Llamada al servicio para crear o actualizar el usuario

            if (dto.Id is null || dto.Id == Guid.Empty)
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result); // Retorno 201 para creación

            return Ok(result); // Retorno 200 para actualización
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message }); // Retorno 400 si hay error en los parámetros
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message }); // Retorno 409 si hay conflicto (e.g. username ya existe)
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message }); // Retorno 500 para otros errores
        }
    }


    /// <summary>
    /// Elimina un usuario.
    /// </summary>
    /// <remarks>
    /// Requiere rol administrador.
    /// </remarks>
    /// <param name="id">Identificador del usuario.</param>
    /// <returns>NoContent si se eliminó, 404 si no existe.</returns>
    /// DELETE /api/users/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "administrator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var ok = await _svc.DeleteAsync(id); // Llamada al servicio para eliminar el usuario
            return ok ? NoContent() : NotFound(); // Retorno 204 si se eliminó, 404 si no existe
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message }); // Retorno 400 si hay error en los parámetros
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message }); // Retorno 500 para otros errores
        }
    }

    /// <summary>
    /// Cambia la contraseña de un usuario.
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <param name="dto">Nueva contraseña.</param>
    /// <returns>NoContent si se cambió, 404 si no existe.</returns>
    /// POST /api/users/{id}/password
    [HttpPost("{id:guid}/password")]
    [Authorize(Roles = "administrator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordDto dto)
    {
        try
        {
            var ok = await _svc.ChangePasswordAsync(id, dto.Password); // Llamada al servicio para cambiar la contraseña
            return ok ? NoContent() : NotFound(); // Retorno 204 si se cambió, 404 si no existe
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message }); // Retorno 400 si hay error en los parámetros
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message }); // Retorno 400 si hay error en los parámetros
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message }); // Retorno 500 para otros errores    
        }
    }

    /// <summary>
    /// Devuelve los datos del usuario autenticado.
    /// </summary>
    /// <remarks>
    /// Requiere token JWT en el header Authorization.
    /// </remarks>
    /// <returns>Datos del usuario (formato MeResponse) o 404 si no existe.</returns>
    // GET /api/users/me
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Me()
    {
        try
        {
            var uid = GetUserId();
            var me = await _db.Users.AsNoTracking()
                .Where(x => x.Id == uid)
                .Select(x => new
                {
                    id = x.Id,
                    username = x.Username,
                    fullname = x.Fullname,
                    role = x.Type.ToString().ToLower(),
                    avatarUrl = x.AvatarUrl
                })
                .FirstOrDefaultAsync();

            return me is null ? NotFound() : Ok(me);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza datos de mi perfil.
    /// </summary>
    /// <remarks>
    /// Permite actualizar fullname y avatarUrl.
    /// Requiere token JWT en el header Authorization.
    /// </remarks>
    /// <param name="dto">Datos a actualizar (fullname obligatorio).</param>
    /// <returns>Datos actualizados del usuario o 404 si no existe.</returns>
    /// PATCH /api/users/me
    [HttpPatch("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateMe([FromBody] UpsertUserDto dto)
    {
        try
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.Fullname))
                return BadRequest(new { error = "fullname requerido" }); // Validación básica

            var uid = GetUserId(); // Obtenemos el Id del usuario autenticado

            var updatedUser = await _svc.UpsertAsync(uid, dto); // Llamada al servicio para actualizar el usuario

            return Ok(updatedUser); // Retorno 200 con los datos actualizados
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message }); // Retorno 400 si hay error en los parámetros
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message }); // Retorno 404 si el usuario no existe
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message }); // Retorno 500 para otros errores
        }
    }


    /// <summary>
    /// Request para subir avatar.
    /// </summary>
    /// <remarks>
    /// Clase interna para manejar la subida de archivos.
    /// </remarks>
    public class UploadAvatarRequest
    {
        public IFormFile File { get; set; } = default!;
    }

    /// <summary>
    /// Sube o reemplaza mi avatar (multipart/form-data, máx 5MB).
    /// </summary>
    /// <remarks>
    /// Guarda el archivo en /wwwroot/avatars/{userId}.{ext} y actualiza el campo avatarUrl del usuario.
    /// Requiere token JWT en el header Authorization.
    /// </remarks>
    /// <param name="req">Archivo de imagen a subir.</param>
    /// <returns>URL del avatar actualizado o error.</returns>
    /// POST /api/users/me/avatar
    [HttpPost("me/avatar")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [ProducesResponseType(typeof(UpdateAvatarDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadAvatar([FromForm] UploadAvatarRequest req)
    {
        try
        {
            if (req.File is null || req.File.Length == 0)
                return BadRequest(new { error = "Archivo requerido" }); // Validación básica

            if (!req.File.ContentType.StartsWith("image/"))
                return BadRequest(new { error = "Debe ser una imagen" }); // Validación básica

            var uid = GetUserId(); // Obtenemos el Id del usuario autenticado

            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"); // Aseguramos que exista wwwroot
            var avatarsDir = Path.Combine(webRoot, "avatars"); // Carpeta de avatares
            Directory.CreateDirectory(avatarsDir); // Crear la carpeta si no existe

            var ext = Path.GetExtension(req.File.FileName); // Obtener la extensión del archivo
            if (string.IsNullOrWhiteSpace(ext)) ext = ".png"; // Si no tiene extensión, asumimos .png

            var fileName = uid.ToString("N") + ext.ToLowerInvariant(); // Nombre del archivo: userId + extensión
            var fullPath = Path.Combine(avatarsDir, fileName); // Ruta completa del archivo

            using (var fs = System.IO.File.Create(fullPath))
                await req.File.CopyToAsync(fs); // Guardar el archivo en disco

            var relPath = Path.Combine("avatars", fileName).Replace("\\", "/"); // Ruta relativa para usar en la URL

            var updatedUser = await _svc.UpdateAvatarAsync(uid, new UpdateAvatarDto(relPath));             // Llamada al service, solo le pasamos la URL

            string? avatarUrl = updatedUser!.AvatarUrl;
            return Ok(new UpdateAvatarDto(avatarUrl)); // Retorno 200 con la URL del avatar actualizado
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message }); // Retorno 400 si hay error en los parámetros
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message }); // Retorno 404 si el usuario no existe
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message }); // Retorno 500 para otros errores
        }
    }


    /// <summary>
    /// Devuelve métricas de mi actividad (watchlist y ratings).
    /// </summary>
    /// <remarks>
    /// Requiere token JWT en el header Authorization.
    /// </remarks>
    /// <returns>Objeto con conteos de watchlist y ratings.</returns>
    /// GET /api/users/me/summary
    [HttpGet("me/summary")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Summary()
    {
        try
        {
            var uid = GetUserId(); // Obtenemos el Id del usuario autenticado

            var watchlistCount = await _db.WatchListItems
                .Where(i => i.WatchList!.UserId == uid)
                .CountAsync(); // Contar items en la watchlist del usuario

            var ratingsCount = await _db.Ratings
                .Where(r => r.UserId == uid)
                .CountAsync(); // Contar ratings del usuario

            return Ok(new { watchlistCount, ratingsCount }); // Retorno 200 con el resumen
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message }); // Retorno 500 para otros errores
        }
    }

    /// <summary>
    /// Comprueba si existe al menos un usuario registrado.
    /// </summary>
    /// <remarks>
    /// Devuelve { exists: true } si hay al menos un usuario en la base de datos,
    /// o { exists: false } si la tabla está vacía. Este endpoint es público (AllowAnonymous)
    /// para que el frontend lo use en el primer arranque y pueda redirigir a /register.
    /// </remarks>
    /// <returns>Objeto con la propiedad exists.</returns>
    /// GET /api/users/exists
    [HttpGet("exists")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Exists()
    {
        try
        {
            var exists = await _svc.AnyUsersAsync();
            return Ok(new { exists });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

}
