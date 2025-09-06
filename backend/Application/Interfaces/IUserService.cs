using TPFinal.Api.Domain;

namespace TPFinal.Api.Application;

/// <summary>
/// Interfaz para el servicio de gestión de usuarios.
/// </summary>
/// <remarks>
/// Define los métodos para crear, obtener, actualizar y borrar usuarios.
/// </remarks>
public interface IUserService
{
    Task<IReadOnlyList<UserDetailDto>> GetAllAsync(string? q); // Devuelve todos los usuarios, con búsqueda opcional por nombre de usuario (contiene)
    Task<UserDetailDto?> GetByIdAsync(Guid id); // Devuelve el usuario por su ID o null si no existe
    Task<UserDetailDto> UpsertAsync(Guid id, UpsertUserDto dto); // Crea o atualiza y devuelve el usuario creado, o null si no existe
    Task<bool> DeleteAsync(Guid id); // Borra el usuario, devuelve true si se borró, false si no existe
    Task<bool> ChangePasswordAsync(Guid id, string newPassword); // Cambia la contraseña del usuario, devuelve true si se cambió, false si no existe
    Task<UserDetailDto> UpdateAvatarAsync(Guid id, UpdateAvatarDto avatarDto); // Actualiza el avatar del usuario y devuelve el usuario actualizado
    Task<bool> AnyUsersAsync(); // Indica si existe al menos un usuario registrado en la base de datos.
}
