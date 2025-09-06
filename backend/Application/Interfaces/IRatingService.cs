using TPFinal.Api.Domain;

namespace TPFinal.Api.Application;

/// <summary>
/// Interfaz para el servicio de gestión de calificaciones.
/// </summary>
/// <remarks>
/// Define los métodos para crear, actualizar, borrar y obtener calificaiones de películas.
/// </remarks>
public interface IRatingService
{
    Task<RatingDto> UpsertAsync(Guid userId, RateUpsertDto dto);           // Crear o actualizar el rating de una película
    Task DeleteAsync(Guid userId, string imdbId);                              // Eliminar el rating de una película
    Task<IReadOnlyList<RatingDto>> GetByMovieAsync(string imdbId);             // Obtener todos los ratings de una película
    Task<MovieRatingSummaryDto?> GetMovieSummaryAsync(string imdbId);          // Obtener el resumen de ratings de una película (cantidad y promedio)
    Task<IReadOnlyList<RatingDto>> GetMineAsync(Guid userId);                  // Obtener los ratings de un usuario
}
