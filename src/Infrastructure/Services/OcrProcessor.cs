using GestionDocumental.Application.Interfaces;

namespace GestionDocumental.Infrastructure.Services;

public sealed class OcrProcessor : IOcrProcessor
{
    public async Task<string> ExtraerTextoAsync(string rutaArchivo, 
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(rutaArchivo))
        {
            throw new FileNotFoundException($"El archivo PDF no existe: {rutaArchivo}");
        }

        // Simulamos extracción de texto desde PDF
        // En implementación real se usaría una librería como iTextSharp, PDFsharp, etc.
        // Por ahora retornamos contenido simulado
        var contenido = await File.ReadAllTextAsync(rutaArchivo, cancellationToken);
        
        // En una implementación real, aquí iría la lógica OCR real
        return contenido.Length > 100 ? contenido[..100] : contenido;
    }

    public bool PuedeProcesar(string rutaArchivo)
    {
        return Path.GetExtension(rutaArchivo).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
    }
}