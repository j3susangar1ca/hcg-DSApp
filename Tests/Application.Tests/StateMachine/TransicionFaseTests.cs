// Tests/Application.Tests/StateMachine/TransicionFaseTests.cs
using FluentAssertions;
using GestionDocumental.Application.DTOs;
using GestionDocumental.Application.Interfaces;
using GestionDocumental.Domain.Entities;
using GestionDocumental.Domain.Enums;
using GestionDocumental.Domain.Exceptions;
using GestionDocumental.Presentation.ViewModels;
using NSubstitute;

namespace GestionDocumental.Application.Tests.StateMachine;

public sealed class TransicionFaseTests
{
    private readonly IDocumentAnalyzerService _analyzerMock;
    private readonly IOcrProcessor _ocrMock;
    private readonly ICryptoSealer _cryptoMock;
    private readonly INetworkStorageManager _storageMock;
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly IRepositorio<DocumentoPrincipal> _documentoRepoMock;
    private readonly IRepositorio<BitacoraTrazabilidad> _bitacoraRepoMock;
    private readonly IRepositorio<CatalogoCadido> _catalogoRepoMock;

    public TransicionFaseTests()
    {
        _analyzerMock = Substitute.For<IDocumentAnalyzerService>();
        _ocrMock = Substitute.For<IOcrProcessor>();
        _cryptoMock = Substitute.For<ICryptoSealer>();
        _storageMock = Substitute.For<INetworkStorageManager>();
        _unitOfWorkMock = Substitute.For<IUnitOfWork>();
        _documentoRepoMock = Substitute.For<IRepositorio<DocumentoPrincipal>>();
        _bitacoraRepoMock = Substitute.For<IRepositorio<BitacoraTrazabilidad>>();
        _catalogoRepoMock = Substitute.For<IRepositorio<CatalogoCadido>>();

        _unitOfWorkMock.Documentos.Returns(_documentoRepoMock);
        _unitOfWorkMock.Bitacoras.Returns(_bitacoraRepoMock);
        _unitOfWorkMock.Catalogos.Returns(_catalogoRepoMock);
    }

    private DocumentoViewModel CrearViewModel() => new(
        _analyzerMock,
        _ocrMock,
        _cryptoMock,
        _storageMock,
        _unitOfWorkMock);

    [Fact]
    public async Task Transicion_Archivado_SinCadidoId_DebeLanzarExcepcionDeNegocio()
    {
        // Arrange
        var vm = CrearViewModel();
        vm.FaseActual = FaseCicloVida.Clasificado;
        vm.CadidoIdSeleccionado = null;
        vm.RutaRedActual = @"\\server\docs\file.pdf";
        vm.HashCriptografico = "hash";
        vm.FolioOficial = "FOL-001";
        vm.Remitente = "R";
        vm.Asunto = "A";

        // Act
        Func<Task> act = async () => await vm.ArchivarDocumentoAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExcepcionDeNegocio>()
            .WithMessage("*no cumple las fases requeridas*");
    }

    [Fact]
    public async Task Transicion_Archivado_SinSellado_Previo_DebeLanzarExcepcion()
    {
        // Arrange
        var vm = CrearViewModel();
        vm.FaseActual = FaseCicloVida.Clasificado;
        vm.CadidoIdSeleccionado = Guid.NewGuid();
        vm.RutaRedActual = @"\\server\docs\file.pdf";
        vm.HashCriptografico = null!; // sin hash

        // Act
        Func<Task> act = async () => await vm.ArchivarDocumentoAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExcepcionDeNegocio>();
    }

    [Fact]
    public async Task Transicion_Archivado_SinIngreso_Previo_DebeLanzarExcepcion()
    {
        // Arrange
        var vm = CrearViewModel();
        vm.FaseActual = FaseCicloVida.Clasificado;
        vm.CadidoIdSeleccionado = Guid.NewGuid();
        vm.RutaRedActual = null!; // sin ruta

        // Act
        Func<Task> act = async () => await vm.ArchivarDocumentoAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExcepcionDeNegocio>();
    }

    [Fact]
    public async Task Transicion_Nacimiento_A_Ingresado_DebeSerValida()
    {
        // Arrange
        var vm = CrearViewModel();
        vm.FaseActual = FaseCicloVida.Nacimiento;
        vm.RutaArchivoPdf = @"C:\temp\doc.pdf";
        vm.RutaRedActual = @"\\server\temp\doc.pdf";

        _storageMock.CopiarATemporalAsync(vm.RutaArchivoPdf, Arg.Any<CancellationToken>())
            .Returns(vm.RutaRedActual);

        // Act
        await vm.IngresarDocumentoAsync(CancellationToken.None);

        // Assert
        vm.FaseActual.Should().Be(FaseCicloVida.Ingresado);
    }

    [Fact]
    public async Task Transicion_Ingresado_A_Sellado_DebeSerValida()
    {
        // Arrange
        var vm = CrearViewModel();
        vm.FaseActual = FaseCicloVida.Ingresado;
        vm.RutaRedActual = @"\\server\temp\doc.pdf";
        var hashEsperado = "a".PadLeft(64, 'a');
        _cryptoMock.GenerarHashSha256Async(vm.RutaRedActual, Arg.Any<CancellationToken>())
            .Returns(hashEsperado);

        // Act
        await vm.SellarDocumentoAsync(CancellationToken.None);

        // Assert
        vm.FaseActual.Should().Be(FaseCicloVida.Sellado);
        vm.HashCriptografico.Should().Be(hashEsperado);
    }

    [Fact]
    public async Task Transicion_Sellado_A_Clasificado_DebeSerValida()
    {
        // Arrange
        var vm = CrearViewModel();
        vm.FaseActual = FaseCicloVida.Sellado;
        vm.RutaRedActual = @"\\server\temp\doc.pdf";
        vm.TextoExtraidoOcr = "texto del documento";
        var geminiResponse = new GeminiResponseDto
        {
            Remitente = "Remitente IA",
            Asunto = "Asunto IA",
            EsUrgente = true,
            EstatusSugerido = "GESTION"
        };
        _ocrMock.ExtraerTextoAsync(vm.RutaRedActual, Arg.Any<CancellationToken>())
            .Returns(vm.TextoExtraidoOcr);
        _analyzerMock.AnalizarDocumentoAsync(vm.TextoExtraidoOcr, Arg.Any<CancellationToken>())
            .Returns(geminiResponse);

        // Act
        await vm.ClasificarDocumentoAsync(CancellationToken.None);

        // Assert
        vm.FaseActual.Should().Be(FaseCicloVida.Clasificado);
        vm.Remitente.Should().Be(geminiResponse.Remitente);
        vm.Asunto.Should().Be(geminiResponse.Asunto);
        vm.IsUrgente.Should().BeTrue();
        vm.EstatusAccionSeleccionado.Should().Be(EstatusAccion.Gestion);
    }

    [Fact]
    public async Task Transicion_Clasificado_A_Archivado_ConCadido_DebeSerValida()
    {
        // Arrange
        var vm = CrearViewModel();
        vm.FaseActual = FaseCicloVida.Clasificado;
        vm.CadidoIdSeleccionado = Guid.NewGuid();
        vm.RutaRedActual = @"\\server\temp\doc.pdf";
        vm.HashCriptografico = "hash";
        vm.FolioOficial = "FOL-001";
        var rutaDefinitiva = @"\\server\final\CADIDO_2025_FOL-001.pdf";
        _storageMock.MoverADefinitivoAsync(
                vm.RutaRedActual,
                Arg.Any<string>(),
                Arg.Any<int>(),
                vm.FolioOficial,
                Arg.Any<CancellationToken>())
            .Returns(rutaDefinitiva);

        // Act
        await vm.ArchivarDocumentoAsync(CancellationToken.None);

        // Assert
        vm.FaseActual.Should().Be(FaseCicloVida.Archivado);
        vm.RutaRedActual.Should().Be(rutaDefinitiva);
    }

    [Theory]
    [InlineData(FaseCicloVida.Ingresado)]
    [InlineData(FaseCicloVida.Sellado)]
    [InlineData(FaseCicloVida.Clasificado)]
    public async Task Transicion_A_Rechazado_DebeSerValida_DesdeFasesIntermedias(FaseCicloVida faseOrigen)
    {
        // Arrange
        var vm = CrearViewModel();
        vm.FaseActual = faseOrigen;

        // Act
        await vm.RechazarDocumentoAsync(CancellationToken.None);

        // Assert
        vm.FaseActual.Should().Be(FaseCicloVida.Rechazado);
    }

    [Fact]
    public async Task Transicion_A_Rechazado_DesdeNacimiento_DebeSerInvalida()
    {
        // Arrange
        var vm = CrearViewModel();
        vm.FaseActual = FaseCicloVida.Nacimiento;

        // Act
        Func<Task> act = async () => await vm.RechazarDocumentoAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ExcepcionDeNegocio>();
    }
}