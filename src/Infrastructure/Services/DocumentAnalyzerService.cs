using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GestionDocumental.Application.DTOs;
using GestionDocumental.Application.Interfaces;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace GestionDocumental.Infrastructure.Services;

public class GeminiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
}

public sealed class DocumentAnalyzerService(
    HttpClient httpClient,
    IOptions<GeminiOptions> options) : IDocumentAnalyzerService
{
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
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

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
        
        var result = JsonSerializer.Deserialize<GeminiResponseDto>(responseString, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return result ?? throw new InvalidOperationException("Respuesta Gemini nula");
    }

    public string ConstruirPromptSistema()
    {
        return """
            Eres un asistente experto en análisis documental según las normas CADIDO.
            Analiza el texto y extrae JSON: folio, remitente, asunto, es_urgente, estatus_sugerido.
            """;
    }
}
