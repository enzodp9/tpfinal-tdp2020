using TPFinal.Api.Domain;

namespace TPFinal.Api.Application;

/// <summary>
/// Interfaz para el servicio de gestión de listas de seguimiento.
/// </summary>
/// <remarks>
/// Define los métodos para obtener, agregar, eliminar y reordenar películas en la lista de seguimiento de un usuario.
/// </remarks>
public interface IWatchlistService
{
    Task<IReadOnlyList<WatchlistItemDto>> GetMyAsync(Guid userId); // Obtener mi watchlist
    Task AddAsync(Guid userId, string imdbId, int? position = null); // Agregar una película a mi watchlist (al final o en posición específica)
    Task RemoveAsync(Guid userId, string imdbId); // Eliminar una película de mi watchlist
    Task ReorderAsync(Guid userId, string imdbId, int newPosition); // Cambiar posición de una película en mi watchlist
}
