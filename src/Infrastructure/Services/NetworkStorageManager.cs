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

        // .NET 10: File.CopyAsync con CancellationToken nativo
        await File.CopyAsync(rutaOrigen, rutaDestino, ct).ConfigureAwait(false);
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

        // .NET 10: File.MoveAsync con CancellationToken
        await File.MoveAsync(rutaTemporal, rutaFinal, ct).ConfigureAwait(false);
        return rutaFinal;
    }

    public string GenerarNombreConvencional(string codigoCadido, int anio, string folio)
    {
        // C# 14: Interpolación de cadenas con expresiones
        return $"{codigoCadido}_{anio}_{folio}.pdf";
    }

    private static string GenerarRutaUnica(string directorioBase, string nombreArchivo)
    {
        var rutaDestino = Path.Combine(directorioBase, nombreArchivo);
        if (!File.Exists(rutaDestino)) return rutaDestino;

        var nombreSinExtension = Path.GetFileNameWithoutExtension(nombreArchivo);
        var extension = Path.GetExtension(nombreArchivo);
        
        // C# 14: params collections en lugar de arrays explícitos
        return string.Join("_", [nombreSinExtension, Guid.NewGuid().ToString()[..8], extension]);
    }

    public async Task<bool> VerificarAccesibilidadAsync(string ruta, 
        CancellationToken ct = default)
    {
        try
        {
            if (!Directory.Exists(ruta) && !File.Exists(ruta)) return false;
            
            if (Directory.Exists(ruta))
            {
                _ = await Task.Run(() => Directory.GetFiles(ruta, "*", SearchOption.TopDirectoryOnly), ct)
                    .ConfigureAwait(false);
            }
            else
            {
                using var fs = File.OpenRead(ruta);
                await fs.ReadAsync([], ct).ConfigureAwait(false); // Operación no-op con ct
            }
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }
}