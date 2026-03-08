namespace GestionDocumental.Application.Interfaces;

public interface INetworkStorageManager
{
    Task<string> CopiarATemporalAsync(string rutaOrigen, CancellationToken cancellationToken = default);
    Task<string> MoverADefinitivoAsync(string rutaTemporal, string codigoCadido, int anio, 
        string folio, CancellationToken cancellationToken = default);
    string GenerarNombreConvencional(string codigoCadido, int anio, string folio);
    Task<bool> VerificarAccesibilidadAsync(string ruta, CancellationToken cancellationToken = default);
}