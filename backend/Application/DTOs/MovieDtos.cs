namespace TPFinal.Api.Application;

/// <summary>
/// DTOs relacionados con películas.
/// </summary>
/// <remarks>
/// Estos DTOs se utilizan para transferir datos de películas entre diferentes capas de la aplicación.
/// </remarks>
public record MovieDetailDto(
    string ImdbId,
    string Title,
    string Type,            
    string? Genre,
    string? Released,       
    int? RuntimeMinutes,
    string? Poster,
    string? Country,
    decimal? ImdbRating,
    string? Director,
    string? Writer,
    string? Actors,
    int? Year               
); // DTO para la información detallada de una película 

public record MovieListItemDto(
    string ImdbId,
    string Title,
    string Type,            
    string? Genre,
    string? Poster,
    decimal? ImdbRating,
    int? Year
); // DTO para la información resumida de una película en listas 

public record MovieSearchItemDto(
    string ImdbId,
    string Title,
    string Year,
    string Type,     
    string? Poster
); // DTO para la utilización de filtros de búsqueda en la lista de películas
