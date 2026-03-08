using GestionDocumental.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionDocumental.Infrastructure.Data;

public sealed class DocumentoDbContext : DbContext
{
    public DocumentoDbContext(DbContextOptions<DocumentoDbContext> options) : base(options) { }

    public DbSet<DocumentoPrincipal> Documentos { get; set; } = null!;
    public DbSet<BitacoraTrazabilidad> Bitacoras { get; set; } = null!;
    public DbSet<CatalogoCadido> Catalogos { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DocumentoPrincipal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FolioOficial).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Remitente).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Asunto).IsRequired().HasMaxLength(500);
            entity.Property(e => e.RutaRedActual).IsRequired().HasMaxLength(500);
            entity.Property(e => e.HashCriptografico).IsRequired().HasMaxLength(64);
            entity.HasIndex(e => e.FolioOficial).IsUnique();
            entity.HasOne(d => d.CatalogoCadido)
                  .WithMany(c => c.Documentos)
                  .HasForeignKey(d => d.CadidoId);
        });

        modelBuilder.Entity<BitacoraTrazabilidad>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FaseAnterior).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FaseNueva).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DescripcionEvento).IsRequired().HasMaxLength(1000);
            entity.HasOne(b => b.DocumentoPrincipal)
                  .WithMany(d => d.Bitacoras)
                  .HasForeignKey(b => b.DocumentoId);
        });

        modelBuilder.Entity<CatalogoCadido>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Seccion).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Serie).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Subserie).IsRequired().HasMaxLength(100);
        });
    }
}
