using System.Security.Cryptography;
using GestionDocumental.Application.Interfaces;

namespace GestionDocumental.Infrastructure.Services;

public sealed class CryptoSealer(ICryptoOptions options) : ICryptoSealer
{
    private readonly ICryptoOptions _options = options;

    public async Task<string> GenerarHashSha256Async(string rutaArchivo, 
        CancellationToken ct = default)
    {
        if (!File.Exists(rutaArchivo))
            throw new FileNotFoundException($"Archivo no existe: {rutaArchivo}");

        using var stream = File.OpenRead(rutaArchivo);
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream, ct).ConfigureAwait(false);
        
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public async Task<bool> ValidarIntegridadAsync(string rutaArchivo, string hashOriginal, 
        CancellationToken ct = default)
    {
        try
        {
            var hashActual = await GenerarHashSha256Async(rutaArchivo, ct).ConfigureAwait(false);
            
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(hashActual),
                Convert.FromHexString(hashOriginal));
        }
        catch
        {
            return false;
        }
    }
}

public interface ICryptoOptions
{
    int HashAlgorithmId { get; set; }
}
