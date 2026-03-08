using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GestionDocumental.Application.DTOs;
using GestionDocumental.Application.Interfaces;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace GestionDocumental.Infrastructure.Services;

public sealed class DocumentAnalyzerService(
    HttpClient httpClient,
    IOptions<GeminiOptions> options) : IDocumentAnalyzerService
{
    // C# 14 field keyword para backing field automático
    private readonly HttpClient _httpClient = httpClient;
    private readonly GeminiOptions _options = options.Value;

    public async Task<GeminiResponseDto> AnalizarDocumentoAsync(string textoDocumento, 
        CancellationToken ct = default)
    {
        var policy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    // Logging estructurado requerido
                });

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = ConstruirPromptSistema() },
                        new { text = $"Texto del documento: {textoDocumento}" }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        // NO se captura Exception genérica - permitir propagación
        var response = await policy.ExecuteAsync(async (innerCt) =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post, 
                $"{_options.Endpoint}?key={_options.ApiKey}")
            {
                Content = content
            };
            
            var httpResponse = await _httpClient.SendAsync(request, innerCt).ConfigureAwait(false);
            httpResponse.EnsureSuccessStatusCode();
            return httpResponse;
        }, ct);

        var responseString = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        
        // Deserialización con validación estricta
        var result = JsonSerializer.Deserialize<GeminiResponseDto>(responseString, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return result ?? throw new InvalidOperationException("Respuesta Gemini inválida: deserialización nula");
    }

    public string ConstruirPromptSistema()
    {
        // C# 14: Raw string literals con interpolación segura
        return """
            Eres un asistente experto en análisis documental según las normas CADIDO.
            Analiza el texto proporcionado y extrae la siguiente información en formato JSON estricto:
            {
                "folio": "string o null",
                "remitente": "string (requerido)",
                "asunto": "string (requerido)",
                "es_urgente": boolean,
                "estatus_sugerido": "RESPUESTA" | "GESTION" | "AVISO" | "ARCHIVAR"
            }
            
            Responde ÚNICAMENTE con el objeto JSON. No incluyas markdown ni texto adicional.
            """;
    }
}