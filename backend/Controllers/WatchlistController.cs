using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TPFinal.Api.Application;

namespace TPFinal.Api.Controllers;

/// <summary>
/// Endpoints para administrar la lista de seguimiento (watchlist) del usuario.
/// </summary>
[ApiController]
[Route("api/watchlist")]
[Authorize]
public class WatchlistController : ControllerBase
{
    private readonly IWatchlistService _svc;

    public WatchlistController(IWatchlistService svc) => _svc = svc;

    private Guid GetUserId()
    {
        var uid = User.FindFirstValue("uid");
        if (string.IsNullOrWhiteSpace(uid)) throw new UnauthorizedAccessException("uid no presente en el token");
        return Guid.Parse(uid);
    }

    /// <summary>
    /// Obtiene mi watchlist ordenada por posición.
    /// </summary>
    /// <remarks>
    /// Requiere token JWT en el header Authorization.
    /// </remarks>
    /// <returns>Lista de películas en mi watchlist.</returns>
    /// GET /api/watchlist
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WatchlistItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<WatchlistItemDto>>> GetMine()
    {
        try
        {
            var userId = GetUserId(); // Obtenemos el Id del usuario autenticado
            var items = await _svc.GetMyAsync(userId); // Obtenemos los items de su watchlist
            return Ok(items); // Retorno 200 con la lista de items
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message }); // Retorno 500 para otros errores
        }
    }

    /// <summary>
    /// Agrega una película a mi watchlist.
    /// </summary>
    /// <remarks>
    /// Requiere token JWT en el header Authorization.
    /// </remarks>
    /// <param name="req">IMDb Id y posición opcional (al final por defecto).</param>
    /// <returns>204 si se agregó correctamente.</returns>
    /// POST /api/watchlist
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Add([FromBody] AddToWatchlistRequest req)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(req.ImdbId))
                return BadRequest(new { error = "ImdbId requerido" }); // Retorno 400 si no se provee ImdbId

            var userId = GetUserId(); // Obtenemos el Id del usuario autenticado
            await _svc.AddAsync(userId, req.ImdbId, req.Position);  // Intentamos agregar la película a su watchlist
            return NoContent(); // Retorno 204 si se agregó correctamente
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message }); // Retorno 404 si la película no existe
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message }); // Retorno 400 si la posición es inválida o ya está en la watchlist
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message }); // Retorno 500 para otros errores    
        }
    }

    /// <summary>
    /// Elimina una película de mi watchlist.
    /// </summary>
    /// <remarks>
    /// Requiere token JWT en el header Authorization.
    /// </remarks>
    /// <param name="imdbId">IMDb Id de la película a eliminar.</param>
    /// <returns>204 si se eliminó correctamente.</returns>
    /// DELETE /api/watchlist/{imdbId}
    [HttpDelete("{imdbId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Remove(string imdbId)
    {
        try
        {
            var userId = GetUserId(); // Obtenemos el Id del usuario autenticado
            await _svc.RemoveAsync(userId, imdbId); // Intentamos eliminar la película de su watchlist
            return NoContent(); // Retorno 204 si se eliminó correctamente
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message }); // Retorno 500 para otros errores
        }
    }

    /// <summary>
    /// Cambia la posición de una película dentro de mi watchlist.
    /// </summary>
    /// <remarks>
    /// Requiere token JWT en el header Authorization.
    /// </remarks>
    /// <param name="req">IMDb Id y nueva posición.</param>
    /// <returns>204 si se reordenó correctamente.</returns>
    /// PATCH /api/watchlist/reorder
    [HttpPatch("reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Reorder([FromBody] ReorderRequest req)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(req.ImdbId) || req.NewPosition <= 0)
                return BadRequest(new { error = "Datos inválidos" }); // Retorno 400 si no se provee ImdbId o posición inválida

            var userId = GetUserId(); // Obtenemos el Id del usuario autenticado
            await _svc.ReorderAsync(userId, req.ImdbId, req.NewPosition); // Intentamos reordenar la película en su watchlist
            return NoContent(); // Retorno 204 si se reordenó correctamente
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message }); // Retorno 404 si la película no está en la watchlist
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message }); // Retorno 400 si la nueva posición es inválida
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message }); // Retorno 500 para otros errores     
        }
    }
}
