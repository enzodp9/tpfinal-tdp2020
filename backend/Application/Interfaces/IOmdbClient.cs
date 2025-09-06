using TPFinal.Api.Application;
using TPFinal.Api.Domain;

/// <summary>
/// Interfaz para el cliente de OMDb.
/// </summary>
/// <remarks>
/// Define los métodos para buscar y obtener detalles de películas desde la API de OMDb.
/// </remarks>
public interface IOmdbClient
{
    Task<Movie?> FetchAsync(string imdbIdOrTitle); // Devuelve los detalles de una película por su ID de IMDb o título, o null si no se encuentra
    Task<IReadOnlyList<MovieSearchItemDto>> SearchAsync(string query); // Devuelve una lista de películas que coinciden con la búsqueda por título (contiene)
}
