using Microsoft.AspNetCore.Mvc;
using TPFinal.Api.Application;
using TPFinal.Api.Domain;

namespace TPFinal.Api.Controllers;

/// <summary>
/// Endpoints para consultar películas y realizar búsquedas.
/// </summary>
[ApiController]
[Route("api/movies")]
public class MoviesController : ControllerBase
{
    private readonly IMovieService _svc;
    public MoviesController(IMovieService svc) => _svc = svc;

    /// <summary>
    /// Obtiene el detalle completo de una película por su ID de IMDb.
    /// </summary>
    /// <remarks>
    /// Busca la película en la base de datos local. Si no existe, devuelve 404.
    /// </remarks>
    /// <param name="imdbId"></param>
    /// <returns>Detalle completo de la película</returns> 
    // GET /api/movies/{imdbId} 
    [HttpGet("{imdbId}")]
    [ProducesResponseType(typeof(MovieDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MovieDetailDto>> GetById(string imdbId)
    {
        try
        {
            var m = await _svc.GetByIdAsync(imdbId); // Llamada al servicio para obtener la película por su ID de IMDb
            if (m is null) return NotFound(); // Retorno 404 si no se encuentra la película

            // Mapeo a DTO para la respuesta
            string? director = string.Join(", ",
                m.TeamMembers.Where(t => t.Type == MemberType.Director).Select(t => t.Name));
            if (string.IsNullOrWhiteSpace(director)) director = null;

            string? writer = string.Join(", ",
                m.TeamMembers.Where(t => t.Type == MemberType.Writer).Select(t => t.Name));
            if (string.IsNullOrWhiteSpace(writer)) writer = null;

            string? actors = string.Join(", ",
                m.TeamMembers.Where(t => t.Type == MemberType.Cast).Select(t => t.Name));
            if (string.IsNullOrWhiteSpace(actors)) actors = null;

            return Ok(new MovieDetailDto(
                m.ImdbId,
                m.Title,
                m.Type == MovieType.Movie ? "movie" : "series",
                m.Genre,
                m.Released?.ToString("yyyy-MM-dd"),
                m.Runtime,
                m.Poster,
                m.Country,
                m.RatingIMDB,
                director,
                writer,
                actors,
                m.Released?.Year
            )); // Retorno de la respuesta con el DTO
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message }); // Retorno 400 si hay error en los parámetros
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message }); // Retorno 404 si no se encuentra la película
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message }); // Retorno 500 para otros errores
        }
    }

    /// <summary>
    /// Obtiene una lista de películas según los criterios de búsqueda.
    /// </summary>
    /// <remarks>
    /// Soporta búsqueda por ID de IMDb, título (contiene), género y tipo (movie o series).
    /// Si no existen localmente, las obtiene desde OMDb y las guarda en la base de datos.
    /// </remarks>
    /// <param name="imdbId">ID de IMDb de la película (opcional).</param>
    /// <param name="title">Título o parte del título de la película (opcional, contiene).</param>
    /// <param name="genre">Género de la película (opcional, contiene).</param>
    /// <param name="type">Tipo de contenido: "movie" o "series" (opcional).</param>
    /// <returns>Lista de películas que coinciden con los criterios de búsqueda.</returns>
    /// GET /api/movies/search?imdbId={title}={genre}=type={type}
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<MovieListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<MovieListItemDto>>> Search(
    [FromQuery] string? imdbId,
    [FromQuery] string? title,
    [FromQuery] string? genre,
    [FromQuery] string? type)
    {
        try
        {
            var list = await _svc.EnsureAndSearchAsync(imdbId, title, genre, type); // Llamada al servicio para obtener las películas

            var result = list.Select(m => new MovieListItemDto(
                m.ImdbId,
                m.Title,
                m.Type == MovieType.Movie ? "movie" : "series",
                m.Genre,
                m.Poster,
                m.RatingIMDB,
                m.Released?.Year
            )); // Mapeo a DTOs para la respuesta

            return Ok(result); // Retorno de la respuesta con el resultado
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message }); // Retorno 400 si hay error en los parámetros
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message }); // Retorno 404 si no se encuentra la película
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message }); // Retorno 500 para otros errores
        }
    }
}
