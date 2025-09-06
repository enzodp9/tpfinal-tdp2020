namespace TPFinal.Api.Domain;

/// <summary>
/// Clase que representa una lista de seguimiento de un usuario.
/// </summary>

public class WatchList
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public List<WatchListItem> Items { get; set; } = new();

    public WatchList() { }
    public WatchList(Guid userId) { Id = Guid.NewGuid(); UserId = userId; }

}

