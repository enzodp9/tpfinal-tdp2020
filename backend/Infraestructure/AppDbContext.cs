using Microsoft.EntityFrameworkCore;
using TPFinal.Api.Domain;

namespace TPFinal.Api.Infrastructure;

/// <summary>
/// Contexto de la base de datos de la aplicación.
/// </summary>
/// <remarks>
/// Define las entidades y sus relaciones, así como las configuraciones específicas de la base de datos.
/// </remarks>

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

    public DbSet<User> Users => Set<User>(); 
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<WatchList> WatchLists => Set<WatchList>();
    public DbSet<WatchListItem> WatchListItems => Set<WatchListItem>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // Users
        mb.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Username).HasMaxLength(64).IsRequired();
            e.Property(u => u.Fullname).HasMaxLength(128);
            e.Property(u => u.AvatarUrl).HasMaxLength(512);
            e.HasIndex(u => u.Username).IsUnique();
        });

        // Movies
        mb.Entity<Movie>(e =>
        {
            e.HasKey(m => m.ImdbId);
            e.Property(m => m.ImdbId).HasMaxLength(16).IsRequired();
            e.Property(m => m.Title).HasMaxLength(256).IsRequired();
            e.Property(m => m.Genre).HasMaxLength(256);
            e.Property(m => m.Country).HasMaxLength(128);
            e.Property(m => m.Poster).HasMaxLength(512);
            e.Property(m => m.RatingIMDB).HasPrecision(3, 1);
            e.Property(m => m.Released).HasColumnType("date");
        });

        // TeamMembers
        mb.Entity<TeamMember>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Name).HasMaxLength(128).IsRequired();
            e.Property(t => t.MovieId).HasMaxLength(16).IsRequired();

            e.HasOne(t => t.Movie)
             .WithMany(m => m.TeamMembers)
             .HasForeignKey(t => t.MovieId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Ratings
        mb.Entity<Rating>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Qualification).IsRequired();
            e.Property(r => r.Comment).HasMaxLength(1024);
            e.Property(r => r.MovieId).HasMaxLength(16).IsRequired();
            e.Property(r => r.Date).HasColumnType("datetime");

            e.HasOne(r => r.User)
             .WithMany(u => u.Ratings)
             .HasForeignKey(r => r.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(r => r.Movie)
             .WithMany(m => m.Ratings!)
             .HasForeignKey(r => r.MovieId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(r => new { r.UserId, r.MovieId }).IsUnique();

            e.ToTable(tb =>
            {
                tb.HasCheckConstraint("CK_Rating_Qualification_Range", "[Qualification] BETWEEN 1 AND 5");
            });
        });

        // WatchLists
        mb.Entity<WatchList>(e =>
        {
            e.HasKey(w => w.Id);

            e.HasOne(w => w.User)
             .WithMany(u => u.WatchLists)
             .HasForeignKey(w => w.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // WatchListItems
        mb.Entity<WatchListItem>(e =>
        {
            e.HasKey(wi => new { wi.WatchListId, wi.MovieId });

            e.Property(wi => wi.MovieId).HasMaxLength(16).IsRequired();

            e.HasOne(wi => wi.WatchList)
             .WithMany(w => w.Items)
             .HasForeignKey(wi => wi.WatchListId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(wi => wi.Movie)
             .WithMany(m => m.WatchListItems!)
             .HasForeignKey(wi => wi.MovieId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(wi => new { wi.WatchListId, wi.Position }).IsUnique();
        });
    }
}
