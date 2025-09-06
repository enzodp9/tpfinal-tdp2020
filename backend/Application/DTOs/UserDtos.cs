namespace TPFinal.Api.Application;

/// <summary>
/// DTOs relacionados con usuarios.
/// </summary>
/// <remarks>
/// Estos DTOs se utilizan para transferir datos de usuarios entre diferentes capas de la aplicación.
/// Se diferencia de los DTOs de autenticación que están en AuthDtos.cs en que estos DTOs son para la
/// gestión general de usuarios (listado, detalle, creación, actualización, cambio de contraseña)
/// y los otros para autenticación de usuarios.
/// </remarks>

public record UserDetailDto(Guid Id, string Username, string Fullname, string Role, string? AvatarUrl); // DTO para la información detallada de un usuario
public record UpsertUserDto(Guid? Id, string? Username, string Fullname, string? Password, string? Role); // DTO para crear o actualizar un usuario
public record ChangePasswordDto(string Password); // DTO para cambiar la contraseña de un usuario
public record UpdateAvatarDto(string? AvatarUrl); // DTO para actualizar el avatar de un usuario