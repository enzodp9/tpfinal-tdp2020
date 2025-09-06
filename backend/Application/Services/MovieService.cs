using Microsoft.EntityFrameworkCore;
using TPFinal.Api.Domain;
using TPFinal.Api.Infrastructure;

namespace TPFinal.Api.Application;

/// <summary>
/// Implementación de servicio para la gestión de películas.
/// </summary>
/// <remarks>
/// Proporciona métodos para buscar, obtener y asegurar la existencia de películas en la base de datos, interactuando con la API de OMDb cuando es necesario.
/// </remarks>

public class MovieService : IMovieService
{
    private readonly AppDbContext _db; // Contexto de la base de datos
    private readonly IOmdbClient _omdb; // Cliente para interactuar con la API de OMDb

    private readonly ILogger<MovieService> _logger; // Logger para registrar eventos e información

    public MovieService(AppDbContext db, IOmdbClient omdb, ILogger<MovieService> logger)
    {
        _db = db;
        _omdb = omdb;
        _logger = logger;
    }

    /// <summary>
    /// Busca películas en la base de datos local según los filtros proporcionados.
    /// </summary>
    /// <remarks>
    /// Aplica filtros opcionales por título (contiene), género (contiene) y tipo (movie/series).
    /// Devuelve una lista de películas que coinciden con los criterios, ordenadas por título.
    /// </remarks>
    private async Task<IReadOnlyList<Movie>> SearchWithFiltersAsync(string? title, string? genre, string? type)
    {
        try
        {
            var q = _db.Movies.AsQueryable(); // Consulta inicial de todas las películas

            if (!string.IsNullOrWhiteSpace(title))
            {
                var like = $"%{title.Trim()}%";
                q = q.Where(m => EF.Functions.Like(m.Title, like));
            } // Filtrado por título (LIKE)

            if (!string.IsNullOrWhiteSpace(genre))
            {
                var g = genre.Trim().ToLower();
                q = q.Where(m => (m.Genre ?? "").ToLower().Contains(g));
            } // Filtrado por género (Contains)

            if (!string.IsNullOrWhiteSpace(type))
            {
                var t = type.Trim().ToLower();
                q = q.Where(m =>
                    (m.Type == MovieType.Movie && (t == "movie")) ||
                    (m.Type == MovieType.Series && (t == "series"))
                );
            } // Filtrado por tipo (movie/series/pelicula/serie)

            return await q.OrderBy(m => m.Title).ToListAsync(); // Ejecución de la consulta y retorno de resultados ordenados por título
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en SearchWithFiltersAsync(title: {Title}, genre: {Genre}, type: {Type})", title, genre, type); // Log del error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }
    }

    /// <summary>
    /// Obtiene una película por IMDB Id con relaciones (TeamMembers, Ratings).
    /// </summary>
    /// <remarks>
    /// Si no se encuentra la película, devuelve null.
    /// </remarks>
    public async Task<Movie?> GetByIdAsync(string imdbId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(imdbId))
                throw new ArgumentException("imdbId requerido", nameof(imdbId)); // Validación del parámetro

            return await _db.Movies
                .Include(m => m.TeamMembers)
                .Include(m => m.Ratings)
                .FirstOrDefaultAsync(m => m.ImdbId == imdbId); // Búsqueda de la película en la base de datos con las relaciones incluidas
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en GetByIdAsync(imdbId: {ImdbId})", imdbId); // Registro del error
            throw;
        }
    }

    /// <summary>
    /// Asegura que las películas existan en la base de datos local según los criterios proporcionados.
    /// </summary>
    /// <remarks>
    /// Si no existen, las obtiene desde OMDb y las guarda en la base de datos.
    /// Devuelve la lista de películas que coinciden con los criterios.
    /// </remarks>
    public async Task<IReadOnlyList<Movie>> EnsureAndSearchAsync(
    string? imdbId = null,
    string? title = null,
    string? genre = null,
    string? type = null)
    {
        try
        {
            // 1) Caso búsqueda por imdbId (único)
            if (!string.IsNullOrWhiteSpace(imdbId))
            {
                var found = await _db.Movies
                    .Include(m => m.TeamMembers)
                    .FirstOrDefaultAsync(m => m.ImdbId == imdbId); // Búsqueda por IMDb ID con relaciones

                if (found is not null)
                    return new[] { found }; // Ya está en BD → retorno inmediato

                // No está en BD → asegurar desde OMDb
                var detailed = await _omdb.FetchAsync(imdbId)
                               ?? throw new InvalidOperationException("No se encontró en OMDb"); // Traer detalle desde OMDb
                _db.Movies.Add(detailed); // Agregar a la BD
                await _db.SaveChangesAsync(); // Guardar cambios

                return new[] { detailed }; // Retorno del detalle
            }

            // 2) Caso búsqueda por filtros (varios)
            // Intentar buscar locales con los filtros
            var local = await SearchWithFiltersAsync(title, genre, type);
            if (local.Count > 0)
                return local; // Si hubo locales, retorno inmediato

            // 3) Si no hubo locales, intentar buscar en OMDb (sólo si hay título)
            if (!string.IsNullOrWhiteSpace(title))
            {
                var omdbItems = await _omdb.SearchAsync(title.Trim()); // Búsqueda en OMDb por título

                // filtrar por type (movie/series/pelicula/serie) si vino
                if (!string.IsNullOrWhiteSpace(type))
                {
                    var tt = type.Trim().ToLower();
                    var norm = tt == "pelicula" ? "movie" : tt; // normaliza “pelicula”
                    omdbItems = omdbItems
                        .Where(i => (i.Type ?? "").ToLower() == norm)
                        .ToList();
                } // Filtrado por tipo si se proporcionó

                // Traer detalle por imdbId y persistir si no existe
                foreach (var it in omdbItems)
                {
                    if (!await _db.Movies.AnyAsync(m => m.ImdbId == it.ImdbId))
                    {
                        var detailed = await _omdb.FetchAsync(it.ImdbId);
                        if (detailed is not null)
                            _db.Movies.Add(detailed);
                    }
                } // Iteración para asegurar cada película
                if (_db.ChangeTracker.HasChanges())
                    await _db.SaveChangesAsync(); // Guardar cambios si hubo adiciones

                return await SearchWithFiltersAsync(title, genre, type); // Reintentar búsqueda local con los filtros (ya persistido)
            }

            // 4) No se proveyó imdbId ni title → retorno vacío
            return Array.Empty<Movie>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en EnsureAndSearchAsync(imdbId:{ImdbId}, title:{Title}, genre:{Genre}, type:{Type})",
                imdbId, title, genre, type); // Log del error
            throw; // Re-lanzar la excepción para que el controlador pueda manejarla
        }
    }
}
