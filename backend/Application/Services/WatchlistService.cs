using Microsoft.EntityFrameworkCore;
using TPFinal.Api.Domain;
using TPFinal.Api.Infrastructure;

namespace TPFinal.Api.Application;

/// <summary>
/// Implementación de servicio para la gestión de la lista de seguimiento (watchlist).
/// </summary>
/// <remarks>
/// Permite obtener, agregar, eliminar y reordenar películas en la watchlist de un usuario,
/// manteniendo la consistencia de posiciones.
/// </remarks>
public class WatchlistService : IWatchlistService
{
    private readonly AppDbContext _db; // Contexto de la base de datos
    private readonly ILogger<WatchlistService> _logger; // Logger para registrar eventos e información

    public WatchlistService(AppDbContext db, ILogger<WatchlistService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todos los elementos de la watchlist de un usuario.
    /// </summary>
    /// <remarks>
    /// Si la watchlist no existe, se crea una nueva vacía.
    /// Los elementos se devuelven ordenados por su posición.
    /// </remarks>
    public async Task<IReadOnlyList<WatchlistItemDto>> GetMyAsync(Guid userId)
    {
        try
        {
            var wl = await GetOrCreateWatchlist(userId); // Obtener o crear watchlist

            var items = await _db.WatchListItems
                .Where(i => i.WatchListId == wl.Id)
                .Include(i => i.Movie)
                .OrderBy(i => i.Position)
                .Select(i => new WatchlistItemDto(i.MovieId, i.Movie!.Title, i.Movie.Poster, i.Position))
                .ToListAsync(); // Obtener y mapear items

            return items; // Devolver lista de items
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en GetMyAsync(userId:{UserId})", userId); // Log de error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }
    }

    /// <summary>
    /// Agrega una película a la watchlist del usuario.
    /// </summary>
    /// <remarks>
    /// Si la película ya está en la watchlist, no realiza ninguna acción.
    /// Si no se especifica posición, se agrega al final; si se especifica,
    /// se inserta en esa posición y se ajustan las demás.
    /// </remarks>
    public async Task AddAsync(Guid userId, string imdbId, int? position = null)
    {
        try
        {
            var wl = await GetOrCreateWatchlist(userId); // Obtener o crear watchlist

            var exists = await _db.WatchListItems.AnyAsync(i => i.WatchListId == wl.Id && i.MovieId == imdbId); // Verificar si ya existe
            if (exists) return;

            var movie = await _db.Movies.FindAsync(imdbId); // Verificar que la película exista
            if (movie is null) throw new InvalidOperationException("La película no existe en BD."); 

            int pos;    // posición final a asignar
            if (position is null) // agregar al final
            {
                var lastPos = await _db.WatchListItems
                    .Where(i => i.WatchListId == wl.Id)
                    .Select(i => (int?)i.Position)
                    .MaxAsync() ?? 0;

                pos = lastPos + 1; 
            }
            else // insertar en posición específica
            {
                pos = Math.Max(1, position.Value); 

                var toShift = await _db.WatchListItems
                    .Where(i => i.WatchListId == wl.Id && i.Position >= pos)
                    .ToListAsync(); 

                foreach (var it in toShift) it.Position += 1; 
            }

            _db.WatchListItems.Add(new WatchListItem
            {
                WatchListId = wl.Id,
                MovieId = imdbId,
                Position = pos
            }); // Agregar nuevo item

            await _db.SaveChangesAsync(); // Guardar cambios
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operación inválida en AddAsync(userId:{UserId}, imdb:{Imdb})", userId, imdbId); // Log de error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Fallo al persistir AddAsync(userId:{UserId}, imdb:{Imdb})", userId, imdbId); // Log de error
            throw new InvalidOperationException("No se pudo agregar a la watchlist.", ex); // Re-lanzar la excepción para que el controlador pueda manejarla
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado en AddAsync(userId:{UserId}, imdb:{Imdb})", userId, imdbId); // Log de error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }
    }

    /// <summary>
    /// Elimina una película de la watchlist de un usuario.
    /// </summary>
    /// <remarks>
    /// Si la película no está en la watchlist, no realiza ninguna acción.
    /// Al eliminar, se ajustan las posiciones de los demás elementos para mantener la secuencia
    /// sin huecos.
    /// </remarks>
    public async Task RemoveAsync(Guid userId, string imdbId)
    {
        try
        {
            var wl = await GetOrCreateWatchlist(userId); // Obtener o crear watchlist

            var item = await _db.WatchListItems.FirstOrDefaultAsync(i => i.WatchListId == wl.Id && i.MovieId == imdbId);
            if (item is null) return; // No existe → nada que hacer

            var removedPos = item.Position; 
            _db.WatchListItems.Remove(item); // Marcar para eliminación

            // compactar posiciones: bajar en 1 a todos los > removedPos
            var toCompact = await _db.WatchListItems
                .Where(i => i.WatchListId == wl.Id && i.Position > removedPos)
                .ToListAsync();

            foreach (var it in toCompact) it.Position -= 1;

            await _db.SaveChangesAsync(); // Guardar cambios
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Fallo al persistir RemoveAsync(userId:{UserId}, imdb:{Imdb})", userId, imdbId); // Log de error
            throw new InvalidOperationException("No se pudo eliminar de la watchlist.", ex); // Re-lanzar la excepción para que el controlador pueda manejarla
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado en RemoveAsync(userId:{UserId}, imdb:{Imdb})", userId, imdbId); // Log de error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }
    }

    /// <summary>
    /// Reordena la posición de una película en la watchlist de un usuario.
    /// </summary>
    /// <remarks>
    /// Si la película no está en la watchlist, lanza una excepción.
    /// La nueva posición se ajusta a los límites (1 a N).
    /// Se mantiene la secuencia de posiciones sin huecos.
    /// </remarks>
    public async Task ReorderAsync(Guid userId, string imdbId, int newPosition)
    {
        try
        {
            if (newPosition < 1) newPosition = 1; 

            var wl = await GetOrCreateWatchlist(userId); // Obtener o crear watchlist

            // Traer todos los items de la lista, ordenados
            var items = await _db.WatchListItems
                .Where(i => i.WatchListId == wl.Id)
                .OrderBy(i => i.Position)
                .ToListAsync();

            var item = items.FirstOrDefault(i => i.MovieId == imdbId)
                       ?? throw new InvalidOperationException("La película no está en tu watchlist."); // Verificar que exista

            // Normalizar límites
            var maxPos = items.Count;
            if (newPosition > maxPos) newPosition = maxPos;

            // Reconstruir el orden en memoria
            items.Remove(item);
            items.Insert(newPosition - 1, item);

            // Transacción para evitar colisiones por el índice único (WatchListId, Position)
            using var tx = await _db.Database.BeginTransactionAsync();

            // Subir todas las posiciones con un offset grande para evitar duplicados intermedios)
            foreach (var it in items)
                it.Position += 100_000;

            await _db.SaveChangesAsync();  // Guardar cambios

            // Reasignar posiciones finales
            for (int i = 0; i < items.Count; i++)
                items[i].Position = i + 1;

            await _db.SaveChangesAsync(); // Guardar cambios
            await tx.CommitAsync(); // Confirmar transacción
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operación inválida en ReorderAsync(userId:{UserId}, imdb:{Imdb}, newPos:{Pos})", userId, imdbId, newPosition); // Log de error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Fallo al persistir ReorderAsync(userId:{UserId}, imdb:{Imdb}, newPos:{Pos})", userId, imdbId, newPosition); // Log de error
            throw new InvalidOperationException("No se pudo reordenar la watchlist.", ex); // Re-lanzar la excepción para que el controlador pueda manejarla
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado en ReorderAsync(userId:{UserId}, imdb:{Imdb}, newPos:{Pos})", userId, imdbId, newPosition); // Log de error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }
    }


    /// <summary>
    /// Obtiene la watchlist del usuario o la crea si no existe.
    /// </summary>
    /// <remarks>
    /// Garantiza que cada usuario tenga una única watchlist.
    /// </remarks>
    private async Task<WatchList> GetOrCreateWatchlist(Guid userId)
    {
        try
        {
            var wl = await _db.WatchLists.FirstOrDefaultAsync(w => w.UserId == userId); // Buscar watchlist existente
            if (wl is not null) return wl; // Ya existe → retorno inmediato

            wl = new WatchList(userId); // Crear nueva watchlist
            _db.WatchLists.Add(wl); // Agregar a la BD
            await _db.SaveChangesAsync(); // Guardar cambios
            return wl; // Retornar la nueva watchlist
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Fallo al crear watchlist para userId:{UserId}", userId); // Log de error
            throw new InvalidOperationException("No se pudo crear la watchlist.", ex); // Re-lanzar la excepción para que el controlador pueda manejarla
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado en GetOrCreateWatchlist(userId:{UserId})", userId); // Log de error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }
    }
}
