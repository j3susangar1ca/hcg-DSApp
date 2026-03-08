using GestionDocumental.Application.Interfaces;

namespace GestionDocumental.Infrastructure.Services;

public sealed class NetworkStorageManager : INetworkStorageManager
{
    private readonly string _rutaTemporalBase = Path.Combine(Path.GetTempPath(), "GestionDocumental");
    private readonly string _rutaDefinitivaBase = @"\\servidor\documentos\definitivos";

    public NetworkStorageManager()
    {
        Directory.CreateDirectory(_rutaTemporalBase);
    }

    public async Task<string> CopiarATemporalAsync(string rutaOrigen, 
        CancellationToken ct = default)
    {
        if (!File.Exists(rutaOrigen))
            throw new FileNotFoundException($"Origen no existe: {rutaOrigen}");

        var nombreArchivo = Path.GetFileName(rutaOrigen);
        var rutaDestino = GenerarRutaUnica(_rutaTemporalBase, nombreArchivo);

        // Uso de Streams para emular CopyAsync con soporte de CancellationToken
        using (var sourceStream = new FileStream(rutaOrigen, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
        using (var destinationStream = new FileStream(rutaDestino, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
        {
            await sourceStream.CopyToAsync(destinationStream, ct).ConfigureAwait(false);
        }
        
        return rutaDestino;
    }

    public async Task<string> MoverADefinitivoAsync(string rutaTemporal, string codigoCadido, 
        int anio, string folio, CancellationToken ct = default)
    {
        var nombreFinal = GenerarNombreConvencional(codigoCadido, anio, folio);
        var carpetaDestino = Path.Combine(_rutaDefinitivaBase, anio.ToString(), codigoCadido);
        
        Directory.CreateDirectory(carpetaDestino);

        var rutaFinal = Path.Combine(carpetaDestino, nombreFinal);
        
        if (File.Exists(rutaFinal))
            throw new InvalidOperationException($"Archivo existente: {rutaFinal}");

        // Move no tiene versión Async directa en File en muchas versiones de .NET; usamos Task.Run para no bloquear
        await Task.Run(() => File.Move(rutaTemporal, rutaFinal), ct).ConfigureAwait(false);
        
        return rutaFinal;
    }

    public string GenerarNombreConvencional(string codigoCadido, int anio, string folio)
    {
        return $"{codigoCadido}_{anio}_{folio}.pdf";
    }

    private static string GenerarRutaUnica(string directorioBase, string nombreArchivo)
    {
        var rutaDestino = Path.Combine(directorioBase, nombreArchivo);
        if (!File.Exists(rutaDestino)) return rutaDestino;

        var nombreSinExtension = Path.GetFileNameWithoutExtension(nombreArchivo);
        var extension = Path.GetExtension(nombreArchivo);
        
        return Path.Combine(directorioBase, $"{nombreSinExtension}_{Guid.NewGuid().ToString()[..8]}{extension}");
    }

    public async Task<bool> VerificarAccesibilidadAsync(string ruta, 
        CancellationToken ct = default)
    {
        try
        {
            if (!Directory.Exists(ruta) && !File.Exists(ruta)) return false;
            
            if (Directory.Exists(ruta))
            {
                await Task.Run(() => Directory.GetFiles(ruta, "*", SearchOption.TopDirectoryOnly), ct)
                    .ConfigureAwait(false);
            }
            else
            {
                using var fs = File.OpenRead(ruta);
                byte[] buffer = new byte[1];
                await fs.ReadAsync(buffer, 0, 1, ct).ConfigureAwait(false);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
}
