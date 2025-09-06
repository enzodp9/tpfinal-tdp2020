namespace TPFinal.Api.Domain;

/// <summary>
/// Clase que representa un miembro del equipo de una película (actor, director, guionista).
/// </summary>
public class TeamMember
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public MemberType Type { get; set; } // Cast / Director / Writer

    public string MovieId { get; set; } = null!;
    public Movie? Movie { get; set; }

    public TeamMember() { }
    public TeamMember(string name, MemberType type, string movieId)
    {
        Id = Guid.NewGuid();
        Name = name;
        Type = type;
        MovieId = movieId;
    }
}
