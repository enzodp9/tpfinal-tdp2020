using TPFinal.Api.Domain;

namespace TPFinal.Api.Application;

/// <summary>
/// Interfaz para el servicio de gestión de películas.
/// </summary>
/// <remarks>
/// Define los métodos para buscar, obtener y asegurar películas desde OMDb y la base de datos local.
/// </remarks>

public interface IMovieService
{
    Task<Movie?> GetByIdAsync(string imdbId); // Devuelve la película por su ID de IMDb o null si no existe

    Task<IReadOnlyList<Movie>> EnsureAndSearchAsync(
        string? imdbId = null,
        string? title = null,
        string? genre = null,
        string? type = null); // Asegura que las películas existan en la BD y las devuelve según los criterios de filtrado
}
