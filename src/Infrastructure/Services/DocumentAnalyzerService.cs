using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GestionDocumental.Application.DTOs;
using GestionDocumental.Application.Interfaces;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace GestionDocumental.Infrastructure.Services;

public sealed class DocumentAnalyzerService : IDocumentAnalyzerService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;

    public DocumentAnalyzerService(HttpClient httpClient, IOptions<GeminiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<GeminiResponseDto> AnalizarDocumentoAsync(string textoDocumento, 
        CancellationToken cancellationToken = default)
    {
        var policy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    Console.WriteLine($"Reintento {retryCount} tras {timespan}s");
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

        try
        {
            var response = await policy.ExecuteAsync(async (ct) =>
            {
                var request = new HttpRequestMessage(HttpMethod.Post, 
                    $"{_options.Endpoint}?key={_options.ApiKey}")
                {
                    Content = content
                };
                
                var httpResponse = await _httpClient.SendAsync(request, ct);
                httpResponse.EnsureSuccessStatusCode();
                return httpResponse;
            }, cancellationToken);

            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponseDto>(responseString, options) 
                                 ?? new GeminiResponseDto();
            
            return geminiResponse;
        }
        catch (JsonException)
        {
            // En caso de error de parsing JSON, retornamos valores por defecto
            return new GeminiResponseDto
            {
                Remitente = string.Empty,
                Asunto = string.Empty,
                EsUrgente = false,
                EstatusSugerido = "ARCHIVAR"
            };
        }
        catch (Exception)
        {
            // En caso de otros errores, retornamos valores por defecto
            return new GeminiResponseDto
            {
                Remitente = string.Empty,
                Asunto = string.Empty,
                EsUrgente = false,
                EstatusSugerido = "ARCHIVAR"
            };
        }
    }

    public string ConstruirPromptSistema()
    {
        return @"
            Eres un asistente experto en análisis documental según las normas CADIDO.
            Analiza el texto proporcionado y extrae la siguiente información en formato JSON:
            - folio: número de folio si está presente
            - remitente: nombre del remitente
            - asunto: asunto principal del documento
            - es_urgente: booleano indicando si es urgente
            - estatus_sugerido: uno de [RESPUESTA, GESTION, AVISO, ARCHIVAR]
            
            Las categorías son:
            - RESPUESTA: documentos que requieren respuesta formal
            - GESTION: documentos que requieren acción administrativa
            - AVISO: documentos informativos
            - ARCHIVAR: documentos históricos
            
            Responde ÚNICAMENTE con el objeto JSON sin texto adicional.";
    }
}

public class GeminiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
}