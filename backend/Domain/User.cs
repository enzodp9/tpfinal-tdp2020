namespace TPFinal.Api.Domain;

/// <summary>
/// Clase que representa un usuario del sistema.
/// </summary>
public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = "";
    public string Fullname { get; set; } = "";
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>(); // SHA256
    public byte[] PasswordSalt { get; set; } = Array.Empty<byte>(); // 16 bytes
    public UserType Type { get; set; } // Administrator / BaseUser
    public string? AvatarUrl { get; set; }
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    public ICollection<WatchList> WatchLists { get; set; } = new List<WatchList>();

    public User() { }

    public User(string username, string fullname, byte[] passwordHash, byte[] passwordSalt, UserType type)
    {
        Id = Guid.NewGuid();
        Username = username;
        Fullname = fullname;
        PasswordHash = passwordHash;
        PasswordSalt = passwordSalt;
        Type = type;
    }
}

