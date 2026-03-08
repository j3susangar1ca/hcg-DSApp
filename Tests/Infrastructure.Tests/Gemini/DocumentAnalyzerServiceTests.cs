// Tests/Infrastructure.Tests/Gemini/DocumentAnalyzerServiceTests.cs
using System.Net;
using System.Text;
using FluentAssertions;
using GestionDocumental.Application.DTOs;
using GestionDocumental.Domain.Enums;
using GestionDocumental.Infrastructure.Services;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace GestionDocumental.Infrastructure.Tests.Gemini;

// ── Fake handler: reemplaza el mock de HttpClient (SendAsync no es virtual) ──
file sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
    public int CallCount { get; private set; }

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        => _responder = responder;

    // Simula timeout/excepción
    public FakeHttpMessageHandler(Exception exception)
        => _responder = _ => throw exception;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(_responder(request));
    }
}

public sealed class DocumentAnalyzerServiceTests
{
    private static IOptions<GeminiOptions> FakeOptions() =>
        Options.Create(new GeminiOptions { ApiKey = "fake-key", Endpoint = "https://fake.com" });

    private static DocumentAnalyzerService CrearSut(HttpMessageHandler handler) =>
        new(new HttpClient(handler), FakeOptions());

    // ── 1. JSON válido ────────────────────────────────────────────────────────
    [Fact]
    public async Task AnalizarDocumento_RespuestaJsonValida_DebeMapearCorrectamente()
    {
        var respuestaJson = """
        {
            "folio": "FOL-123",
            "remitente": "Empresa X",
            "asunto": "Solicitud de información",
            "es_urgente": true,
            "estatus_sugerido": "RESPUESTA"
        }
        """;

        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(respuestaJson, Encoding.UTF8, "application/json")
            });

        var sut = CrearSut(handler);
        var dto = await sut.AnalizarDocumentoAsync("texto de prueba");

        dto.Folio.Should().Be("FOL-123");
        dto.Remitente.Should().Be("Empresa X");
        dto.Asunto.Should().Be("Solicitud de información");
        dto.EsUrgente.Should().BeTrue();
        dto.EstatusSugerido.Should().Be("RESPUESTA");
        dto.ObtenerEstatusAccion().Should().Be(EstatusAccion.Respuesta);
    }

    // ── 2. JSON mal formado → fallback silencioso ─────────────────────────────
    [Fact]
    public async Task AnalizarDocumento_RespuestaIrregular_DebeControlarErrorSinRomperse()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ mal formado ", Encoding.UTF8, "application/json")
            });

        var sut = CrearSut(handler);
        var dto = await sut.AnalizarDocumentoAsync("texto");

        dto.Should().NotBeNull();
        dto.Folio.Should().BeNull();
        dto.Remitente.Should().BeEmpty();
        dto.Asunto.Should().BeEmpty();
        dto.EsUrgente.Should().BeFalse();
        dto.EstatusSugerido.Should().Be("ARCHIVAR");
    }

    // ── 3. Timeout → debe reintentar 3 veces ─────────────────────────────────
    [Fact]
    public async Task AnalizarDocumento_Timeout_DebeImplementarRetry()
    {
        int llamadas = 0;
        var handler = new FakeHttpMessageHandler(_ =>
        {
            llamadas++;
            throw new TaskCanceledException("timeout simulado");
        });

        var sut = CrearSut(handler);
        Func<Task> act = async () => await sut.AnalizarDocumentoAsync("texto");

        await act.Should().ThrowAsync<Exception>();
        llamadas.Should().Be(3); // 3 intentos (Polly retry)
    }

    // ── 4. Rate-limit 429 → exponential backoff, lanza HttpRequestException ───
    [Fact]
    public async Task AnalizarDocumento_RateLimit_DebeImplementarExponentialBackoff()
    {
        int llamadas = 0;
        var handler = new FakeHttpMessageHandler(_ =>
        {
            llamadas++;
            return new HttpResponseMessage((HttpStatusCode)429);
        });

        var sut = CrearSut(handler);
        Func<Task> act = async () => await sut.AnalizarDocumentoAsync("texto");

        await act.Should().ThrowAsync<HttpRequestException>();
        llamadas.Should().Be(3);
    }

    // ── 5. Prompt contiene términos CADIDO ────────────────────────────────────
    [Fact]
    public void ConstruirPromptSistema_DebeIncluir_NormasCADIDO()
    {
        var sut = CrearSut(new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)));

        var prompt = sut.ConstruirPromptSistema();

        prompt.Should().Contain("JSON");
        prompt.Should().Contain("RESPUESTA");
        prompt.Should().Contain("GESTION");
        prompt.Should().Contain("AVISO");
        prompt.Should().Contain("ARCHIVAR");
        prompt.Should().Contain("CADIDO");
    }
}