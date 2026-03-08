namespace GestionDocumental.Domain.Entities;

public sealed class BitacoraTrazabilidad
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required Guid DocumentoId { get; set; }
    public required string FaseAnterior { get; set; }
    public required string FaseNueva { get; set; }
    public DateTime FechaTransaccion { get; set; } = DateTime.UtcNow;
    public required string DescripcionEvento { get; set; }

    public DocumentoPrincipal? DocumentoPrincipal { get; set; }
}