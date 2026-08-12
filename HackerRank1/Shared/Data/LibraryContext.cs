using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LibraryService.WebAPI.Data;

public class LibraryContext : DbContext
{
    public LibraryContext(DbContextOptions<LibraryContext> options)
        : base(options)
    { }

    public DbSet<Library> Libraries { get; set; }
    public DbSet<Book> Books { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>()
            .HasOne(b => b.Library)
            .WithMany()
            .HasForeignKey(b => b.LibraryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class Book
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public int LibraryId { get; set; }
    public virtual Library? Library { get; set; }
}

public class Library
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;
}
