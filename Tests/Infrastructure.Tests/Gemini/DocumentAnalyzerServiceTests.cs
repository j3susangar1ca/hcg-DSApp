// Tests/Infrastructure.Tests/Gemini/DocumentAnalyzerServiceTests.cs
using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using GestionDocumental.Application.DTOs;
using GestionDocumental.Application.Interfaces;
using GestionDocumental.Domain.Enums;
using GestionDocumental.Infrastructure.Services;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GestionDocumental.Infrastructure.Tests.Gemini;

public sealed class DocumentAnalyzerServiceTests
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<GeminiOptions> _options;
    private readonly DocumentAnalyzerService _sut;

    public DocumentAnalyzerServiceTests()
    {
        _httpClient = Substitute.For<HttpClient>();
        _options = Substitute.For<IOptions<GeminiOptions>>();
        _options.Value.Returns(new GeminiOptions
        {
            ApiKey = "fake-key",
            Endpoint = "https://fake.com"
        });
        _sut = new DocumentAnalyzerService(_httpClient, _options);
    }

    [Fact]
    public async Task AnalizarDocumento_RespuestaJsonValida_DebeMapearCorrectamente()
    {
        // Arrange
        var textoEntrada = "texto de prueba";
        var respuestaJson = """
        {
            "folio": "FOL-123",
            "remitente": "Empresa X",
            "asunto": "Solicitud de información",
            "es_urgente": true,
            "estatus_sugerido": "RESPUESTA"
        }
        """;
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(respuestaJson, Encoding.UTF8, "application/json")
        };
        _httpClient.SendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(httpResponse);

        // Act
        var dto = await _sut.AnalizarDocumentoAsync(textoEntrada);

        // Assert
        dto.Folio.Should().Be("FOL-123");
        dto.Remitente.Should().Be("Empresa X");
        dto.Asunto.Should().Be("Solicitud de información");
        dto.EsUrgente.Should().BeTrue();
        dto.EstatusSugerido.Should().Be("RESPUESTA");
        dto.ObtenerEstatusAccion().Should().Be(EstatusAccion.Respuesta);
    }

    [Fact]
    public async Task AnalizarDocumento_RespuestaIrregular_DebeControlarErrorSinRomperse()
    {
        // Arrange
        var textoEntrada = "texto";
        var respuestaJson = "{ mal formado ";
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(respuestaJson, Encoding.UTF8, "application/json")
        };
        _httpClient.SendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(httpResponse);

        // Act
        var dto = await _sut.AnalizarDocumentoAsync(textoEntrada);

        // Assert
        dto.Should().NotBeNull();
        dto.Folio.Should().BeNull();
        dto.Remitente.Should().BeEmpty(); // o lo que devuelva el fallback
        dto.Asunto.Should().BeEmpty();
        dto.EsUrgente.Should().BeFalse();
        dto.EstatusSugerido.Should().Be("ARCHIVAR");
    }

    [Fact]
    public async Task AnalizarDocumento_Timeout_DebeImplementarRetry()
    {
        // Arrange
        var textoEntrada = "texto";
        _httpClient.SendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .ThrowsAsync<TaskCanceledException>(); // simula timeout

        // Act
        Func<Task> act = async () => await _sut.AnalizarDocumentoAsync(textoEntrada);

        // Assert: debería reintentar hasta agotar los intentos y lanzar excepción final
        await act.Should().ThrowAsync<Exception>();
        // Verificar que SendAsync se llamó más de una vez (por el retry)
        await _httpClient.Received(3).SendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnalizarDocumento_RateLimit_DebeImplementarExponentialBackoff()
    {
        // Arrange
        var textoEntrada = "texto";
        var rateLimitResponse = new HttpResponseMessage((HttpStatusCode)429); // TooManyRequests
        _httpClient.SendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>())
            .Returns(rateLimitResponse);

        // Act
        Func<Task> act = async () => await _sut.AnalizarDocumentoAsync(textoEntrada);

        // Assert: después de reintentos debería lanzar HttpRequestException
        await act.Should().ThrowAsync<HttpRequestException>();
        // Verificar que se llamó varias veces (el backoff lo controla Polly)
        await _httpClient.Received(3).SendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ConstruirPromptSistema_DebeIncluir_NormasCADIDO()
    {
        // Arrange
        var prompt = _sut.ConstruirPromptSistema();

        // Assert
        prompt.Should().Contain("JSON");
        prompt.Should().Contain("RESPUESTA");
        prompt.Should().Contain("GESTION");
        prompt.Should().Contain("AVISO");
        prompt.Should().Contain("ARCHIVAR");
        prompt.Should().Contain("CADIDO");
    }
}