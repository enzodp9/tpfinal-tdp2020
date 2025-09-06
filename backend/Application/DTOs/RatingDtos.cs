namespace TPFinal.Api.Application;

/// <summary>
/// DTOs relacionados con calificaciones de películas.
/// </summary>
/// <remarks>
/// Estos DTOs se utilizan para transferir datos de calificaciones entre diferentes capas de la aplicación.
/// </remarks>
public record RateUpsertDto(string ImdbId, int Qualification, string? Comment); // DTO para crear o actualizar una calificación
public record RatingDto(
    Guid Id,
    string ImdbId,
    string Title,
    int Qualification,
    string? Comment,
    DateTime Date,
    string Username,
    string Fullname,
    string? AvatarUrl
); // DTO para la información detallada de una calificación

public record MovieRatingSummaryDto(string ImdbId, string Title, int Count, decimal Average); // DTO para el resumen de calificaciones de una película

