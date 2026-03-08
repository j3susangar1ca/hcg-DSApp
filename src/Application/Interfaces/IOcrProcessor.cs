namespace GestionDocumental.Application.Interfaces;

public interface IOcrProcessor
{
    Task<string> ExtraerTextoAsync(string rutaArchivo, CancellationToken cancellationToken = default);
    bool PuedeProcesar(string rutaArchivo);
}