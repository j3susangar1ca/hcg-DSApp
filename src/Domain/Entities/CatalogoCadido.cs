namespace GestionDocumental.Domain.Entities;

public sealed class CatalogoCadido
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Seccion { get; set; }
    public required string Serie { get; set; }
    public required string Subserie { get; set; }
    public int PlazoConservacionAnios { get; set; }
    public bool IsActivo { get; set; } = true;

    public ICollection<DocumentoPrincipal> Documentos { get; set; } = [];
}