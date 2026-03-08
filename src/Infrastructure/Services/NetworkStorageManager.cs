using GestionDocumental.Application.Interfaces;

namespace GestionDocumental.Infrastructure.Services;

public sealed class NetworkStorageManager : INetworkStorageManager
{
    private readonly string _rutaTemporalBase = Path.Combine(Path.GetTempPath(), "GestionDocumental");
    private readonly string _rutaDefinitivaBase = @"\\servidor\documentos\definitivos";

    public NetworkStorageManager()
    {
        if (!Directory.Exists(_rutaTemporalBase))
        {
            Directory.CreateDirectory(_rutaTemporalBase);
        }
    }

    public async Task<string> CopiarATemporalAsync(string rutaOrigen, 
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(rutaOrigen))
        {
            throw new FileNotFoundException($"El archivo de origen no existe: {rutaOrigen}");
        }

        var nombreArchivo = Path.GetFileName(rutaOrigen);
        var rutaDestino = Path.Combine(_rutaTemporalBase, nombreArchivo);
        
        // Si ya existe, generamos un nombre único
        if (File.Exists(rutaDestino))
        {
            var nombreSinExtension = Path.GetFileNameWithoutExtension(nombreArchivo);
            var extension = Path.GetExtension(nombreArchivo);
            var contador = 1;
            
            do
            {
                rutaDestino = Path.Combine(_rutaTemporalBase, 
                    $"{nombreSinExtension}_{contador}{extension}");
                contador++;
            } while (File.Exists(rutaDestino));
        }

        await File.CopyAsync(rutaOrigen, rutaDestino, cancellationToken);
        return rutaDestino;
    }

    public async Task<string> MoverADefinitivoAsync(string rutaTemporal, string codigoCadido, 
        int anio, string folio, CancellationToken cancellationToken = default)
    {
        var nombreFinal = GenerarNombreConvencional(codigoCadido, anio, folio);
        var carpetaDestino = Path.Combine(_rutaDefinitivaBase, anio.ToString(), codigoCadido);
        
        if (!Directory.Exists(carpetaDestino))
        {
            Directory.CreateDirectory(carpetaDestino);
        }

        var rutaFinal = Path.Combine(carpetaDestino, nombreFinal);
        
        if (File.Exists(rutaFinal))
        {
            throw new InvalidOperationException($"Ya existe un archivo con este nombre: {rutaFinal}");
        }

        await File.MoveAsync(rutaTemporal, rutaFinal, cancellationToken);
        return rutaFinal;
    }

    public string GenerarNombreConvencional(string codigoCadido, int anio, string folio)
    {
        return $"{codigoCadido}_{anio}_{folio}.pdf";
    }

    public async Task<bool> VerificarAccesibilidadAsync(string ruta, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (Directory.Exists(ruta) || File.Exists(ruta))
            {
                // Intentamos leer un directorio o archivo para verificar acceso
                if (Directory.Exists(ruta))
                {
                    _ = Directory.GetFiles(ruta, "*", SearchOption.TopDirectoryOnly);
                }
                else
                {
                    using var fs = File.OpenRead(ruta);
                }
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }
}