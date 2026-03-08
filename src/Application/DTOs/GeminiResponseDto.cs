using GestionDocumental.Domain.Enums;

namespace GestionDocumental.Application.DTOs;

public sealed class GeminiResponseDto
{
    public string? Folio { get; set; }
    public string Remitente { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public bool EsUrgente { get; set; }
    public string EstatusSugerido { get; set; } = "ARCHIVAR";
    
    public EstatusAccion ObtenerEstatusAccion()
    {
        return EstatusSugerido.ToUpper() switch
        {
            "RESPUESTA" => EstatusAccion.Respuesta,
            "GESTION" => EstatusAccion.Gestion,
            "AVISO" => EstatusAccion.Aviso,
            _ => EstatusAccion.Archivar
        };
    }
}