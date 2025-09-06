namespace TPFinal.Api.Domain;

/// <summary>
/// Clase que representa una película en la lista de seguimiento de un usuario incluyendo la posición en la que se encuentra.
/// </summary>
public class WatchListItem
{
    public Guid WatchListId { get; set; }
    public WatchList? WatchList { get; set; }
    public string MovieId { get; set; } = null!;
    public Movie? Movie { get; set; }
    public int Position { get; set; }

    public WatchListItem() { }
    public WatchListItem(Guid watchListId, string movieId, int position)
    {
        WatchListId = watchListId;
        MovieId = movieId;
        Position = position;
    }
}

