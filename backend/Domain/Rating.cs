namespace TPFinal.Api.Domain;

/// <summary>
/// Clase que representa una calificación de una película realizada por un usuario.
/// </summary>
public class Rating
{
    public Guid Id { get; set; }
    public int Qualification { get; set; } // 1..5
    public string? Comment { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string MovieId { get; set; } = null!;
    public Movie? Movie { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public Rating() { }
    public Rating(int qualification, string? comment, Guid userId, string movieId)
    {
        Id = Guid.NewGuid();
        Qualification = qualification;
        Comment = comment;
        UserId = userId;
        MovieId = movieId;
        Date = DateTime.UtcNow;
    }
}
