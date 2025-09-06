namespace TPFinal.Api.Application;

/// <summary>
/// DTOs relacionados con listas de seguimiento (watchlists).
/// </summary>
/// <remarks>
/// Estos DTOs se utilizan para transferir datos de listas de seguimiento entre diferentes capas de la aplicación.
/// </remarks>
public record WatchlistItemDto(
    string ImdbId,
    string Title,
    string? Poster,
    int Position
); // DTO para la información de un ítem en la lista de seguimiento

public record AddToWatchlistRequest(string ImdbId, int? Position = null); // DTO para agregar una película a la lista de seguimiento
public record ReorderRequest(string ImdbId, int NewPosition); // DTO para cambiar la posición de una película en la lista de seguimiento
