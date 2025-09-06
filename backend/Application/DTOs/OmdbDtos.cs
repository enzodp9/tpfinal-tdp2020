namespace TPFinal.Api.Application;

/// <summary>
/// DTOs relacionados con la API de Omdb.
/// </summary>
/// <remarks>
/// Estos DTOs se utilizan para deserializar las respuestas de la API de OMDb.
/// Se diferencian de las entidades de Movie en que
/// estos DTOs reflejan la estructura exacta de los datos recibidos de OMDb.
/// </remarks>
public record OmdbSearchDto(List<OmdbShortDto>? Search, string? Response, string? Error); // DTO para la respuesta de búsqueda (lista corta)
public record class OmdbShortDto (string? Title, string? Year, string? imdbID, string? Type, string? Poster); // DTO para cada ítem en la búsqueda (info corta)
public record OmdbFullDto (
    string? Title,
    string? Year,
    string? Rated,
    string? Released,
    string? Runtime,
    string? Genre,
    string? Director,
    string? Writer,
    string? Actors,
    string? Country,
    string? Poster,
    string? imdbRating,
    string? imdbID,
    string? Type,
    string? Response,
    string? Error
); // DTO para la respuesta completa de detalle de película
