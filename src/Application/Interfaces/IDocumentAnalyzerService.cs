using GestionDocumental.Application.DTOs;

namespace GestionDocumental.Application.Interfaces;

public interface IDocumentAnalyzerService
{
    Task<GeminiResponseDto> AnalizarDocumentoAsync(string textoDocumento, 
        CancellationToken cancellationToken = default);
    string ConstruirPromptSistema();
}