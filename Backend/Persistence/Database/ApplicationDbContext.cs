using System.Diagnostics.CodeAnalysis;
using Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Database;

[Obsolete("This class is not used anymore.")]
[ExcludeFromCodeCoverage(Justification ="We are not currently utilizing a relational database in our application.")]
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public virtual DbSet<Summary> Summaries { get; set; }
    public virtual DbSet<Document> Documents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>()
            .Property(d => d.Embedding)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(float.Parse).ToArray()
            )
            .IsRequired();
    }
}