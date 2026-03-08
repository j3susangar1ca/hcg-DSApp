using GestionDocumental.Domain.Enums;
namespace GestionDocumental.Domain.Entities;

public sealed class DocumentoPrincipal
{
    public Guid Id { get; init; } = Guid.NewGuid();
    
    // C# 14: field keyword elimina backing field explícito
    public required string FolioOficial 
    { 
        get; 
        set => field = !string.IsNullOrWhiteSpace(value) 
            ? value 
            : throw new ArgumentException("Folio requerido"); 
    }
    
    public required string Remitente 
    { 
        get; 
        set => field = !string.IsNullOrWhiteSpace(value) 
            ? value 
            : throw new ArgumentException("Remitente requerido"); 
    }
    
    public required string Asunto 
    { 
        get; 
        set => field = !string.IsNullOrWhiteSpace(value) 
            ? value 
            : throw new ArgumentException("Asunto requerido"); 
    }
    
    public required string RutaRedActual { get; set; }
    public required string HashCriptografico { get; set; }
    public EstatusAccion EstatusAccion { get; set; }
    public FaseCicloVida FaseCicloVida { get; set; }
    public bool IsUrgente { get; set; }
    public DateTime FechaCreacion { get; init; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }

    public Guid? CadidoId { get; set; }
    public CatalogoCadido? CatalogoCadido { get; set; }
    
    // C# 14: Collection expressions
    public ICollection<BitacoraTrazabilidad> Bitacoras { get; set; } = [];
}
