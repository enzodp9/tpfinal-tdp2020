using Microsoft.EntityFrameworkCore;
using TPFinal.Api.Domain;
using TPFinal.Api.Infrastructure;

namespace TPFinal.Api.Application;

/// <summary>
/// Implementación de servicio para la gestión de usuarios.
/// </summary>
/// <remarks>
/// Expone operaciones CRUD y cambio de contraseña, mapeando entidades a DTOs para su exposición en la API.
/// </remarks>
public sealed class UserService : IUserService
{
    private readonly AppDbContext _db; // Contexto de la base de datos
    private readonly ILogger<UserService> _logger; // Logger para registrar eventos e información
    public UserService(AppDbContext db, ILogger<UserService> logger)
    {
        _db = db;
        _logger = logger;
    }


    /// <summary>
    /// Obtener todos los usuarios, con búsqueda opcional por nombre de usuario (contiene)
    /// </summary>
    /// <remarks>
    /// Devuelve una lista de DTOs con los usuarios que coinciden con el filtro.
    /// Si no se proporciona filtro, devuelve todos los usuarios.
    /// </remarks>
    public async Task<IReadOnlyList<UserDetailDto>> GetAllAsync(string? q)
    {
        try
        {
            var query = _db.Users.AsNoTracking(); // Consulta inicial de todos los usuarios

            if (!string.IsNullOrWhiteSpace(q))
            {
                var like = q.Trim().ToLower();
                query = query.Where(u =>
                    u.Username.ToLower().Contains(like) ||
                    u.Fullname.ToLower().Contains(like));
            } // Filtrado por nombre de usuario o nombre completo (Contains)

            var items = await query
                .OrderBy(u => u.Username)
                .Select(u => new UserDetailDto(
                    u.Id, u.Username, u.Fullname, u.Type.ToString().ToLower(), u.AvatarUrl
                ))
                .ToListAsync(); // Ejecución de la consulta y mapeo a DTOs

            return items; // Retorno de la lista de usuarios
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en GetAllAsync(q:{Query})", q); // Log de error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }
    }


    /// <summary>
    /// Obtener el usuario por su ID o null si no existe
    /// </summary>
    /// <remarks>
    /// Devuelve un DTO con los detalles del usuario o null si no se encuentra.
    /// </remarks>
    public async Task<UserDetailDto?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _db.Users.AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => new UserDetailDto(u.Id, u.Username, u.Fullname, u.Type.ToString().ToLower(), u.AvatarUrl))
                .FirstOrDefaultAsync(); // Retorna el usuario o null si no existe
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en GetByIdAsync(id:{Id})", id); // Log de error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }
    }


    /// <summary>
    /// Crea o actualiza un usuario (Upsert).
    /// </summary>
    /// <remarks>
    /// Si el ID es Guid.Empty, crea un nuevo usuario; si no, actualiza el existente.
    /// Valida que el nombre de usuario sea único al crear uno nuevo.
    /// </remarks>
    public async Task<UserDetailDto> UpsertAsync(Guid id, UpsertUserDto dto)
    {
        try
        {
            // Validaciones mínimas
            if (dto is null) throw new ArgumentException("Datos requeridos.");
            if (string.IsNullOrWhiteSpace(dto.Fullname)) throw new ArgumentException("Fullname requerido.");

            var normalizedUsername = dto.Username?.Trim();

            User? user = null;

            if (id == Guid.Empty)
            {
                // Crear nuevo usuario
                if (string.IsNullOrWhiteSpace(dto.Username)) throw new ArgumentException("Username requerido.");
                if (string.IsNullOrWhiteSpace(dto.Password)) throw new ArgumentException("Password requerido.");

                var exists = await _db.Users.AnyAsync(u => u.Username.ToLower() == normalizedUsername!.ToLower());
                if (exists) throw new InvalidOperationException("El nombre de usuario ya existe.");

                var role = NormalizeRole(dto.Role); 
                var (hash, salt) = PasswordHasher.Hash(dto.Password);

                user = new User
                {
                    Id = Guid.NewGuid(),
                    Username = normalizedUsername!,
                    Fullname = dto.Fullname.Trim(),
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    Type = role
                };

                _db.Users.Add(user);
            }
            else
            {
                // Actualizar usuario existente
                user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
                if (user is null) throw new InvalidOperationException("El usuario no existe.");

                var role = NormalizeRole(dto.Role);
                user.Fullname = dto.Fullname.Trim();
                user.Type = role;

                // Opcional: permitir cambio de contraseña
                if (!string.IsNullOrWhiteSpace(dto.Password))
                {
                    var (hash, salt) = PasswordHasher.Hash(dto.Password);
                    user.PasswordHash = hash;
                    user.PasswordSalt = salt;
                }
            }

            await _db.SaveChangesAsync();

            return new UserDetailDto(
                user!.Id,
                user.Username,
                user.Fullname,
                user.Type.ToString().ToLower(),
                user.AvatarUrl
            );
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validación fallida en UpsertAsync(id:{Id}, user:{Username})", id, dto?.Username);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operación inválida en UpsertAsync(id:{Id}, user:{Username})", id, dto?.Username);
            throw;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Fallo al persistir UpsertAsync(id:{Id}, user:{Username})", id, dto?.Username);
            throw new InvalidOperationException("No se pudo guardar el usuario.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado en UpsertAsync(id:{Id}, user:{Username})", id, dto?.Username);
            throw;
        }
    }



    /// <summary>
    /// Borra un usuario por su ID.
    /// </summary>
    /// <remarks>
    /// Si el usuario no existe, no realiza ninguna acción y retorna false.
    /// Si se elimina correctamente, retorna true.
    /// </remarks>
    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id); // Búsqueda del usuario
            if (user is null) return false; // No existe

            _db.Users.Remove(user); // Eliminación del usuario
            await _db.SaveChangesAsync(); // Guardado de cambios
            return true; // Retorno true si se borró
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Fallo al persistir DeleteAsync(id:{Id})", id); // Log de error
            throw new InvalidOperationException("No se pudo eliminar el usuario.", ex); // Re-lanzar la excepción para que el controlador pueda manejarla
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado en DeleteAsync(id:{Id})", id); // Log de error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }
    }

    /// <summary>
    /// Cambia la contraseña de un usuario.
    /// </summary>
    /// <remarks>
    /// Valida que la nueva contraseña no esté vacía.
    /// Retorna true si la contraseña se cambió correctamente, false si el usuario no existe.
    /// </remarks>
    public async Task<bool> ChangePasswordAsync(Guid id, string newPassword)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(newPassword))
                throw new ArgumentException("Password requerido."); // Validación del parámetro

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id); // Búsqueda del usuario
            if (user is null) return false; // No existe

            var (hash, salt) = PasswordHasher.Hash(newPassword); // Hasheo de la nueva contraseña
            user.PasswordHash = hash;
            user.PasswordSalt = salt;

            await _db.SaveChangesAsync(); // Guardado de cambios
            return true; // Retorno true si se cambió la contraseña
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validación fallida en ChangePasswordAsync(id:{Id})", id); // Log de error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Fallo al persistir ChangePasswordAsync(id:{Id})", id); // Log de error
            throw new InvalidOperationException("No se pudo cambiar la contraseña.", ex); // Re-lanzar la excepción para que el controlador pueda manejarla
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado en ChangePasswordAsync(id:{Id})", id); // Log de error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }
    }


    /// <summary>
    /// Actualiza el avatar del usuario y devuelve el usuario actualizado.
    /// </summary>
    /// <remarks>
    /// Valida que la URL del avatar no esté vacía.
    /// Lanza InvalidOperationException si el usuario no existe.   
    /// </remarks>
    public async Task<UserDetailDto> UpdateAvatarAsync(Guid id, UpdateAvatarDto dto)
    {
        try
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.AvatarUrl))
                throw new ArgumentException("AvatarUrl requerido."); // Validación del parámetro

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id); // Búsqueda del usuario
            if (user is null)
                throw new InvalidOperationException("Usuario no encontrado."); // No existe

            user.AvatarUrl = dto.AvatarUrl; // Actualización del avatar
            await _db.SaveChangesAsync(); // Guardado de cambios

            return new UserDetailDto(
                user.Id,
                user.Username,
                user.Fullname,
                user.Type.ToString().ToLower(),
                user.AvatarUrl
            ); // Retorno del usuario actualizado
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validación fallida en UpdateAvatarAsync(id:{Id})", id); // Log de error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operación inválida en UpdateAvatarAsync(id:{Id})", id); // Log de error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Fallo al persistir UpdateAvatarAsync(id:{Id})", id); // Log de error
            throw new InvalidOperationException("No se pudo actualizar el avatar.", ex); // Re-lanzar la excepción para que el controlador pueda manejarla
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado en UpdateAvatarAsync(id:{Id})", id); // Log de error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }

    }

    /// <summary>
    /// Comprueba si existe al menos un usuario en la base de datos.
    /// </summary>
    /// <remarks>
    /// Útil para detectar el primer arranque del sistema y permitir al frontend redirigir
    /// a /register cuando no hay usuarios aún.
    /// </remarks>
    public async Task<bool> AnyUsersAsync()
    {
        try
        {
            return await _db.Users.AnyAsync(); 
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comprobando existencia de usuarios en AnyUsersAsync()"); // Log de error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }
    }


    /// <summary>
    /// Normaliza el rol desde string a UserType.
    /// </summary>
    /// <remarks>
    /// Si el rol es "admin" o "administrator" (case insensitive), retorna UserType.Administrator.
    /// Cualquier otro valor retorna UserType.BaseUser.
    /// </remarks>
    private static UserType NormalizeRole(string? role)
    {
        role = (role ?? "").Trim().ToLower();
        return role is "admin" or "administrator" ? UserType.Administrator : UserType.BaseUser;
    }
}
