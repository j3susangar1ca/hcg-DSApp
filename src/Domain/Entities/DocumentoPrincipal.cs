using GestionDocumental.Domain.Enums;
namespace GestionDocumental.Domain.Entities;

public sealed class DocumentoPrincipal
{
    private string _folioOficial = string.Empty;
    private string _remitente = string.Empty;
    private string _asunto = string.Empty;

    public Guid Id { get; init; } = Guid.NewGuid();
    
    public required string FolioOficial 
    { 
        get => _folioOficial;
        set => _folioOficial = !string.IsNullOrWhiteSpace(value) 
            ? value 
            : throw new ArgumentException("Folio requerido"); 
    }
    
    public required string Remitente 
    { 
        get => _remitente;
        set => _remitente = !string.IsNullOrWhiteSpace(value) 
            ? value 
            : throw new ArgumentException("Remitente requerido"); 
    }
    
    public required string Asunto 
    { 
        get => _asunto;
        set => _asunto = !string.IsNullOrWhiteSpace(value) 
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
    
    public ICollection<BitacoraTrazabilidad> Bitacoras { get; set; } = new List<BitacoraTrazabilidad>();
}

