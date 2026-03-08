namespace GestionDocumental.Application.Interfaces;

public interface ICryptoSealer
{
    Task<string> GenerarHashSha256Async(string rutaArchivo, CancellationToken cancellationToken = default);
    Task<bool> ValidarIntegridadAsync(string rutaArchivo, string hashOriginal, 
        CancellationToken cancellationToken = default);
}