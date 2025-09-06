using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TPFinal.Api.Application;

namespace TPFinal.Api.Controllers;

/// <summary>
/// Endpoints para crear, actualizar, borrar y consultar calificaciones de películas.
/// </summary>
[ApiController]
[Route("api/ratings")]
public class RatingsController : ControllerBase
{
    private readonly IRatingService _svc;

    public RatingsController(IRatingService svc) => _svc = svc;


    /// <summary>
    /// Extrae el UserId del token JWT.
    /// </summary>
    /// <remarks>
    ///  Devuelve el Guid del usuario o Guid.Empty si no se encuentra o es inválido
    /// (en cuyo caso el endpoint debería rechazar la petición por falta de autorización).
    /// </remarks>
    private Guid GetUserId()
    {
        var uid = User.FindFirstValue("uid");
        return Guid.TryParse(uid, out var g) ? g : Guid.Empty;
    }

    /// <summary>
    /// Crea o actualiza mi calificación para una película.
    /// </summary>
    /// <remarks>
    /// Si ya tengo una calificación para esa película, se actualiza.
    /// Si no, se crea una nueva.
    /// La calificación debe estar entre 1 y 5.
    /// La película debe existir en la base de datos (si no, se devuelve 404).
    /// </remarks>
    /// <param name="req">Datos de calificación (1..5) y comentario opcional.</param>
    /// <returns>La calificación resultante.</returns>
    /// POST /api/ratings
    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(RatingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RatingDto>> Upsert([FromBody] RateUpsertDto req)
    {
        try
        {
            var userId = GetUserId(); //Obtengo el userId del token
            if (userId == Guid.Empty) return Unauthorized(); // Si no está presente, retorno 401

            var dto = await _svc.UpsertAsync(userId, req); // Llamada al servicio para crear o actualizar la calificación
            return Ok(dto); // Retorno 200 con la calificación resultante
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { error = ex.Message }); // Retorno 400 si la calificación está fuera de rango
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message }); // Retorno 404 si la película no existe
        }
    }

    /// <summary>
    /// Borra mi calificación de la película indicada.
    /// </summary>
    /// <remarks>
    /// Si no tengo una calificación para esa película, no hace nada.
    /// </remarks>
    /// <param name="imdbId">IMDb Id de la película.</param>
    /// <returns>NoContent si se borró o no existía, 401 si no estoy autenticado.</returns>
    /// DELETE /api/ratings/{imdbId}
    [Authorize]
    [HttpDelete("{imdbId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteMine(string imdbId)
    {
        try
        {
            var userId = GetUserId(); //Obtengo el userId del token
            if (userId == Guid.Empty) return Unauthorized(); // Si no está presente, retorno 401

            await _svc.DeleteAsync(userId, imdbId); // Llamada al servicio para borrar la calificación
            return NoContent(); // Retorno 204 si se borró o no existía
        }
        catch (ArgumentException ex)
        { 
            return BadRequest(new { error = ex.Message }); // Retorno 400 si hay error en los parámetros
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message }); // Retorno 500 para otros errores
        }
    }

    /// <summary>
    /// Obtiene todas las calificaciones de una película.
    /// </summary>
    /// <remarks>
    /// El resultado se ordena de forma descendente por fecha.
    /// </remarks>
    /// <param name="imdbId">IMDb Id de la película.</param>
    /// <returns>Lista de calificaciones o 400 si hay error en los parámetros.</returns>
    /// GET /api/ratings/movie/{imdbId}
    [HttpGet("movie/{imdbId}")]
    [ProducesResponseType(typeof(IEnumerable<RatingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<RatingDto>>> GetByMovie(string imdbId)
    {
        try
        {
            var list = await _svc.GetByMovieAsync(imdbId); // Llamada al servicio para obtener las calificaciones
            return Ok(list); // Retorno 200 con la lista de calificaciones
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message }); // Retorno 400 si hay error en los parámetros
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message }); // Retorno 500 para otros errores
        }
    }

    /// <summary>
    /// Obtiene el resumen (cantidad y promedio) de calificaciones de una película.
    /// </summary>
    /// <remarks>
    /// Devuelve null si la película no existe.
    /// </remarks>
    /// <param name="imdbId">IMDb Id de la película.</param>
    /// <returns>Resumen de calificaciones o 404 si la película no existe.</returns>
    /// GET /api/ratings/movie/{imdbId}/summary
    [HttpGet("movie/{imdbId}/summary")]
    [ProducesResponseType(typeof(MovieRatingSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MovieRatingSummaryDto>> GetSummary(string imdbId)
    {
        try
        {
            var res = await _svc.GetMovieSummaryAsync(imdbId); // Llamada al servicio para obtener el resumen
            if (res is null) return NotFound(new { error = "Película inexistente" }); // Retorno 404 si la película no existe
            return Ok(res); // Retorno 200 con el resumen de calificaciones
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message }); // Retorno 400 si hay error en los parámetros
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message }); // Retorno 500 para otros errores
        }
    }

    /// <summary>
    /// Obtiene mis calificaciones ordenadas por fecha descendente.
    /// </summary>
    /// <remarks>
    /// Requiere token JWT en el header Authorization.
    /// </remarks>
    /// <returns>Lista de mis calificaciones o 401 si no estoy autenticado.</returns>
    /// GET /api/ratings/me
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(IEnumerable<RatingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<RatingDto>>> GetMine()
    {
        try
        {
            var userId = GetUserId(); //Obtengo el userId del token
            if (userId == Guid.Empty) return Unauthorized(); // Si no está presente, retorno 401

            var list = await _svc.GetMineAsync(userId); // Llamada al servicio para obtener mis calificaciones
            return Ok(list); // Retorno 200 con la lista de mis calificaciones
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message }); // Retorno 500 para otros errores
        }
    }
}
