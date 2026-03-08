using System.Security.Cryptography;
using System.Text;
using GestionDocumental.Application.Interfaces;

namespace GestionDocumental.Infrastructure.Services;

public sealed class CryptoSealer : ICryptoSealer
{
    public async Task<string> GenerarHashSha256Async(string rutaArchivo, 
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(rutaArchivo))
        {
            throw new FileNotFoundException($"El archivo no existe: {rutaArchivo}");
        }

        using var stream = File.OpenRead(rutaArchivo);
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
        
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public async Task<bool> ValidarIntegridadAsync(string rutaArchivo, string hashOriginal, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var hashActual = await GenerarHashSha256Async(rutaArchivo, cancellationToken);
            return hashActual.Equals(hashOriginal, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}