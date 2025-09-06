using Microsoft.EntityFrameworkCore;
using TPFinal.Api.Domain;
using TPFinal.Api.Infrastructure;

namespace TPFinal.Api.Application;

/// <summary>
/// Implementación de servicio para la gestión de calificaciones de películas.
/// </summary>
/// <remarks>
/// Permite crear/actualizar calificaciones, eliminarlas y consultarlas por película o por usuario,
/// devolviendo DTOs listos para la API.
/// </remarks>
public class RatingService : IRatingService
{
    private readonly AppDbContext _db; // Contexto de la base de datos
    private readonly ILogger<RatingService> _logger; // Logger para registrar eventos e información
    public RatingService(AppDbContext db, ILogger<RatingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Crea o actualiza la calificación de una película por un usuario.
    /// </summary>
    /// <remarks>
    /// Si la calificación ya existe, se actualiza; si no, se crea una nueva.
    /// Valida que la calificación esté entre 1 y 5, y que la película exista en la base de datos.
    /// </remarks>
    public async Task<RatingDto> UpsertAsync(Guid userId, RateUpsertDto dto)
    {
        try
        {
            if (dto.Qualification is < 1 or > 5)
                throw new ArgumentOutOfRangeException(nameof(dto.Qualification), "La calificación debe estar entre 1 y 5."); // Validación de rango

            var movie = await _db.Movies.FirstOrDefaultAsync(m => m.ImdbId == dto.ImdbId)
                        ?? throw new InvalidOperationException("La película no existe en BD. Creala primero desde /api/movies."); // Validación de existencia

            var rating = await _db.Ratings.FirstOrDefaultAsync(r => r.UserId == userId && r.MovieId == dto.ImdbId); // Buscar calificación existente
            if (rating is null)
            {
                rating = new Rating(dto.Qualification, dto.Comment, userId, dto.ImdbId); // Crear nueva calificación
                _db.Ratings.Add(rating); // Guardar en base de datos
            }
            else
            {
                rating.Qualification = dto.Qualification; // Actualizar calificación existente
                rating.Comment = dto.Comment; // Actualizar comentario
                rating.Date = DateTime.UtcNow; // Actualizar fecha
                _db.Ratings.Update(rating); // Marcar para actualización
            }

            await _db.SaveChangesAsync(); // Guardar cambios

            await _db.Entry(rating).Reference(r => r.Movie).LoadAsync(); // Cargar referencia a Movie
            await _db.Entry(rating).Reference(r => r.User).LoadAsync(); // Cargar referencia a User

            return new RatingDto(
                rating.Id,
                rating.Movie!.ImdbId,
                rating.Movie.Title,
                rating.Qualification,
                rating.Comment,
                rating.Date,
                rating.User!.Username,
                rating.User.Fullname,
                rating.User.AvatarUrl
            ); // Retornar DTO de calificación
        }
        catch (ArgumentOutOfRangeException ex)
        {
            _logger.LogWarning(ex, "Validación fallida en UpsertAsync(userId:{UserId}, imdb:{Imdb})", userId, dto.ImdbId); // Log de error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Operación inválida en UpsertAsync(userId:{UserId}, imdb:{Imdb})", userId, dto.ImdbId); // Log de error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Fallo al persistir UpsertAsync(userId:{UserId}, imdb:{Imdb})", userId, dto.ImdbId); // Log de error
            throw new InvalidOperationException("No se pudo guardar la calificación.", ex); // Re-lanzar la excepción para que el controlador pueda manejarla
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado en UpsertAsync(userId:{UserId}, imdb:{Imdb})", userId, dto.ImdbId); // Log de error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }
    }


    /// <summary>
    /// Elimina una calificación de un usuario para una película específica.
    /// </summary>
    /// <remarks>
    /// Si el usuario o la película no existe, se lanza ArgumentException.
    /// Si la calificación no existe, no se realiza ninguna acción.
    /// </remarks>
    public async Task DeleteAsync(Guid userId, string imdbId)
    {
        try
        {
            // Validar existencia de usuario
            var userExists = await _db.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
                throw new ArgumentException("Usuario inexistente.");

            // Validar existencia de película
            var movieExists = await _db.Movies.AnyAsync(m => m.ImdbId == imdbId);
            if (!movieExists)
                throw new ArgumentException("Película inexistente.");

            // Buscar la calificación
            var rating = await _db.Ratings
                .FirstOrDefaultAsync(r => r.UserId == userId && r.MovieId == imdbId);

            if (rating is null) return; // No existe calificación → nada que borrar

            _db.Ratings.Remove(rating); // Marcar para eliminación
            await _db.SaveChangesAsync(); // Guardar cambios
        }
        catch (ArgumentException)
        {
            _logger.LogWarning("Validación fallida en DeleteAsync(userId:{UserId}, imdb:{Imdb})", userId, imdbId); // Log de error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex,
                "Fallo al persistir DeleteAsync(userId:{UserId}, imdb:{Imdb})", userId, imdbId); // Log de error
            throw new InvalidOperationException("No se pudo eliminar la calificación.", ex); // Re-lanzar la excepción para que el controlador pueda manejarla
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error inesperado en DeleteAsync(userId:{UserId}, imdb:{Imdb})", userId, imdbId); // Log de error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }
    }

    /// <summary>
    /// Obtiene todas las calificaciones asociadas a una película.
    /// </summary>
    /// <remarks>
    /// El resultado se ordena de forma descendente por fecha.
    /// </remarks>
    public async Task<IReadOnlyList<RatingDto>> GetByMovieAsync(string imdbId)
    {
        try
        {
            return await _db.Ratings
                .AsNoTracking()
                .Where(r => r.Movie!.ImdbId == imdbId)
                .OrderByDescending(r => r.Date)
                .Select(r => new RatingDto(
                    r.Id,
                    r.Movie!.ImdbId,
                    r.Movie.Title,
                    r.Qualification,
                    r.Comment,
                    r.Date,
                    r.User!.Username,
                    r.User.Fullname,
                    r.User.AvatarUrl
                ))
                .ToListAsync(); // Ejecutar y retornar lista de DTOs
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en GetByMovieAsync(imdb:{Imdb})", imdbId); // Log del error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }
    }

    /// <summary>
    /// Obtiene un resumen de las calificaciones de una película.
    /// </summary>
    /// <remarks>
    /// Si la película existe pero no tiene calificaciones, se devuelve un resumen con valores en cero.
    /// </remarks>
    public async Task<MovieRatingSummaryDto?> GetMovieSummaryAsync(string imdbId)
    {
        try
        {
            var movie = await _db.Movies.FirstOrDefaultAsync(m => m.ImdbId == imdbId); // Verificar que la película exista
            if (movie is null)
                throw new ArgumentException("Película inexistente.");// No existe la película

            var agg = await _db.Ratings
                .Where(r => r.MovieId == imdbId)
                .GroupBy(r => 1)
                .Select(g => new { Count = g.Count(), Average = g.Average(x => x.Qualification) })
                .FirstOrDefaultAsync();  // Agregación para contar y promediar

            if (agg is null)
                return new MovieRatingSummaryDto(imdbId, movie.Title, 0, 0); // No hay calificaciones

            return new MovieRatingSummaryDto(imdbId, movie.Title, agg.Count, (decimal)agg.Average); // Retornar resumen con datos
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validación fallida en GetMovieSummaryAsync(imdb:{Imdb})", imdbId); // Log del error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en GetMovieSummaryAsync(imdb:{Imdb})", imdbId); // Log del error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }
    }

    /// <summary>
    /// Obtiene todas las calificaciones realizadas por un usuario.
    /// </summary>
    /// <remarks>
    /// El resultado se ordena de forma descendente por fecha.
    /// </remarks>
    public async Task<IReadOnlyList<RatingDto>> GetMineAsync(Guid userId)
    {
        try
        {
            return await _db.Ratings
                .AsNoTracking()
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.Date)
                .Select(r => new RatingDto(
                    r.Id,
                    r.Movie!.ImdbId,
                    r.Movie.Title,
                    r.Qualification,
                    r.Comment,
                    r.Date,
                    r.User!.Username,     // es el mismo user
                    r.User.Fullname,
                    r.User.AvatarUrl
                ))
                .ToListAsync(); // Ejecutar y retornar lista de DTOs
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en GetMineAsync(userId:{UserId})", userId); // Log del error
            throw;  // Re-lanzar la excepción para que el controlador pueda manejarla
        }
    }
}
