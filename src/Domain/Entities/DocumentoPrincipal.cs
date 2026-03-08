using GestionDocumental.Domain.Enums;

namespace GestionDocumental.Domain.Entities;

public sealed class DocumentoPrincipal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string FolioOficial { get; set; }
    public required string Remitente { get; set; }
    public required string Asunto { get; set; }
    public required string RutaRedActual { get; set; }
    public required string HashCriptografico { get; set; }
    public EstatusAccion EstatusAccion { get; set; }
    public FaseCicloVida FaseCicloVida { get; set; }
    public bool IsUrgente { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }

    public Guid? CadidoId { get; set; }
    public CatalogoCadido? CatalogoCadido { get; set; }
    public ICollection<BitacoraTrazabilidad> Bitacoras { get; set; } = [];
}