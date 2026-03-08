// src/Infrastructure/Services/DocumentAnalyzerService.cs
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GestionDocumental.Application.DTOs;
using GestionDocumental.Application.Interfaces;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace GestionDocumental.Infrastructure.Services;

public sealed class GeminiOptions
{
    public string ApiKey  { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
}

// DTO interno para deserializar la respuesta de Gemini
file sealed class GeminiJsonResponse
{
    [JsonPropertyName("folio")]
    public string? Folio { get; set; }

    [JsonPropertyName("remitente")]
    public string Remitente { get; set; } = string.Empty;

    [JsonPropertyName("asunto")]
    public string Asunto { get; set; } = string.Empty;

    [JsonPropertyName("es_urgente")]
    public bool EsUrgente { get; set; }

    [JsonPropertyName("estatus_sugerido")]
    public string EstatusSugerido { get; set; } = "ARCHIVAR";
}

public sealed class DocumentAnalyzerService : IDocumentAnalyzerService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;

    // 1 intento inicial + 2 reintentos = 3 llamadas en total
    private static readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy =
        Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .OrResult(r => r.StatusCode == (HttpStatusCode)429)
            .WaitAndRetryAsync(
                retryCount: 2,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

    public DocumentAnalyzerService(HttpClient httpClient, IOptions<GeminiOptions> options)
    {
        _httpClient = httpClient;
        _options    = options.Value;
    }

    public async Task<GeminiResponseDto> AnalizarDocumentoAsync(
        string textoDocumento, CancellationToken ct = default)
    {
        var requestBody = JsonSerializer.Serialize(new
        {
            prompt = ConstruirPromptSistema(),
            text   = textoDocumento
        });

        HttpResponseMessage response;
        try
        {
            response = await _retryPolicy.ExecuteAsync(() =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
                {
                    Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
                };
                request.Headers.TryAddWithoutValidation("x-api-key", _options.ApiKey);
                return _httpClient.SendAsync(request, ct);
            });
        }
        catch (Exception ex) when (ex is TaskCanceledException or HttpRequestException)
        {
            throw;
        }

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"Gemini devolvio {(int)response.StatusCode}");

        var json = await response.Content.ReadAsStringAsync(ct);

        try
        {
            var parsed = JsonSerializer.Deserialize<GeminiJsonResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return new GeminiResponseDto
            {
                Folio           = parsed?.Folio,
                Remitente       = parsed?.Remitente ?? string.Empty,
                Asunto          = parsed?.Asunto    ?? string.Empty,
                EsUrgente       = parsed?.EsUrgente ?? false,
                EstatusSugerido = parsed?.EstatusSugerido ?? "ARCHIVAR"
            };
        }
        catch (JsonException)
        {
            // Fallback silencioso ante JSON invalido
            return new GeminiResponseDto();
        }
    }

    public string ConstruirPromptSistema() =>
        "Eres un asistente experto en analisis documental segun las normas CADIDO. " +
        "Analiza el texto y responde UNICAMENTE con un objeto JSON con estas propiedades: " +
        "folio (string o null), remitente (string), asunto (string), es_urgente (boolean), " +
        "estatus_sugerido (uno de RESPUESTA, GESTION, AVISO, ARCHIVAR). " +
        "No incluyas explicaciones fuera del JSON.";
}
