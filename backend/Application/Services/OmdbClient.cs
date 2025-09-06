using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TPFinal.Api.Domain;

namespace TPFinal.Api.Application;

/// <summary>
/// Cliente para la API de OMDb.
/// </summary>
/// <remarks>
/// Encapsula las llamadas a la API de OMDb para buscar y obtener detalles de películas,
/// y mapea las respuestas a entidades del dominio y DTOs de búsqueda.
/// </remarks>
public class OmdbClient : IOmdbClient
{
    private readonly HttpClient _http; // Cliente HTTP para realizar las solicitudes
    private readonly IConfiguration _cfg; // Configuración para obtener la URL base y la clave API

    // Configuraciones para deserialización JSON
    private static readonly JsonSerializerOptions _jsonOpts = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public OmdbClient(HttpClient http, IConfiguration cfg) { _http = http; _cfg = cfg; }

    /// <summary>
    /// Obtiene el detalle de una película por IMDb Id o por título.
    /// </summary>
    /// <remarks>
    /// Si se proporciona un IMDb Id (comienza con "tt"), se busca por Id; de lo contrario, se busca por título.
    /// Devuelve null si no se encuentra la película o si ocurre un error en la solicitud
    /// </remarks>
    public async Task<Movie?> FetchAsync(string imdbIdOrTitle)
    {
        var baseUrl = _cfg["Omdb:BaseUrl"] ?? "https://www.omdbapi.com/"; 
        var key = _cfg["Omdb:ApiKey"] ?? throw new InvalidOperationException("OMDb ApiKey no configurada.");

        var url = imdbIdOrTitle.StartsWith("tt", StringComparison.OrdinalIgnoreCase)
            ? $"{baseUrl}?i={imdbIdOrTitle}&plot=short&apikey={key}"
            : $"{baseUrl}?t={Uri.EscapeDataString(imdbIdOrTitle)}&plot=short&apikey={key}";

        var resp = await _http.GetAsync(url);
        var text = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) return null;

        var full = JsonSerializer.Deserialize<OmdbFullDto>(text, _jsonOpts);
        if (full is null || !string.Equals(full.Response, "True", StringComparison.OrdinalIgnoreCase)) return null;

        // 🔽 map inline, sin helpers
        var mv = new Movie
        {
            ImdbId = full.imdbID!,
            Title = full.Title ?? "",
            Type = (full.Type?.ToLowerInvariant()) switch
            {
                "series" => MovieType.Series,
                _ => MovieType.Movie
            },
            Genre = full.Genre,
            Poster = (full.Poster is not null && !full.Poster.Equals("N/A", StringComparison.OrdinalIgnoreCase)) ? full.Poster : null,
            Country = full.Country,
            RatingIMDB = decimal.TryParse(full.imdbRating, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null,
            Released = DateTime.TryParse(full.Released, out var dt) ? DateOnly.FromDateTime(dt) : null,
            Runtime = !string.IsNullOrWhiteSpace(full.Runtime) && int.TryParse(new string(full.Runtime.Where(char.IsDigit).ToArray()), out var n) ? n : null
        };

        if (!string.IsNullOrWhiteSpace(full.Director))
        {
            foreach (var dName in full.Director.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                mv.TeamMembers.Add(new TeamMember(dName, MemberType.Director, mv.ImdbId));
        }

        if (!string.IsNullOrWhiteSpace(full.Writer))
        {
            foreach (var wName in full.Writer.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                mv.TeamMembers.Add(new TeamMember(wName, MemberType.Writer, mv.ImdbId));
        }

        if (!string.IsNullOrWhiteSpace(full.Actors))
        {
            foreach (var aName in full.Actors.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                mv.TeamMembers.Add(new TeamMember(aName, MemberType.Cast, mv.ImdbId));
        }

        return mv;
    }

    /// <summary>
    /// Busca películas por título parcial en OMDb.
    /// </summary>
    /// <remarks>
    /// Devuelve una lista de DTOs con los resultados de la búsqueda.
    /// Si no se encuentran resultados o si ocurre un error, devuelve una lista vacía.
    /// </remarks>
    public async Task<IReadOnlyList<MovieSearchItemDto>> SearchAsync(string query)
    {
        var baseUrl = _cfg["Omdb:BaseUrl"] ?? "https://www.omdbapi.com/";
        var key = _cfg["Omdb:ApiKey"] ?? throw new InvalidOperationException("OMDb ApiKey no configurada.");

        var url = $"{baseUrl}?s={Uri.EscapeDataString(query)}&apikey={key}";
        var resp = await _http.GetAsync(url);
        var text = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) return Array.Empty<MovieSearchItemDto>();

        var res = JsonSerializer.Deserialize<OmdbSearchDto>(text, _jsonOpts);
        if (res is null || res.Search is null || !string.Equals(res.Response, "True", StringComparison.OrdinalIgnoreCase))
            return Array.Empty<MovieSearchItemDto>();

        return res.Search
            .Where(s => !string.IsNullOrWhiteSpace(s.imdbID))
            .Select(s => new MovieSearchItemDto(s.imdbID!, s.Title ?? "", s.Year ?? "", s.Type ?? "", s.Poster))
            .ToList();
    }
}
