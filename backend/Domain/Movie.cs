namespace TPFinal.Api.Domain;

/// <summary>
/// Clase que representa una película o serie.
/// </summary>
public class Movie
{
    public string ImdbId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public MovieType Type { get; set; }
    public string? Genre { get; set; }
    public DateOnly? Released { get; set; }
    public int? Runtime { get; set; }
    public string? Poster { get; set; }
    public string? Country { get; set; }
    public decimal? RatingIMDB { get; set; }
    public List<TeamMember> TeamMembers { get; set; } = new();
    public List<Rating>? Ratings { get; set; }
    public List<WatchListItem>? WatchListItems { get; set; }

    public Movie() { }
    public Movie(string imdbId, string title, MovieType type)
    {
        ImdbId = imdbId; Title = title; Type = type;
    }
}
