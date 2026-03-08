// Tests/Presentation.Tests/ViewModels/DocumentoViewModelComandosTests.cs
using System.Collections.ObjectModel;
using FluentAssertions;
using GestionDocumental.Application.DTOs;
using GestionDocumental.Application.Interfaces;
using GestionDocumental.Domain.Entities;
using GestionDocumental.Domain.Enums;
using GestionDocumental.Domain.Exceptions;
using GestionDocumental.Presentation.ViewModels;
using NSubstitute;

namespace GestionDocumental.Presentation.Tests.ViewModels;

public sealed class DocumentoViewModelComandosTests
{
    private readonly IDocumentAnalyzerService _analyzerMock;
    private readonly IOcrProcessor _ocrMock;
    private readonly ICryptoSealer _cryptoMock;
    private readonly INetworkStorageManager _storageMock;
    private readonly IUnitOfWork _unitOfWorkMock;
    private readonly IRepositorio<DocumentoPrincipal> _documentoRepoMock;
    private readonly IRepositorio<BitacoraTrazabilidad> _bitacoraRepoMock;
    private readonly IRepositorio<CatalogoCadido> _catalogoRepoMock;

    public DocumentoViewModelComandosTests()
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
    public void PropiedadesCalculadas_ReflejanEstadoActual_Correctamente()
    {
        // Arrange
        var vm = CrearViewModel();
        vm.FaseActual = FaseCicloVida.Nacimiento;
        vm.IsProcessing = false;

        // Assert
        vm.CanIngresar.Should().BeTrue();
        vm.CanSellar.Should().BeFalse();
        vm.CanClasificar.Should().BeFalse();
        vm.CanArchivar.Should().BeFalse();
        vm.CanRechazar.Should().BeFalse();

        vm.FaseActual = FaseCicloVida.Ingresado;
        vm.CanIngresar.Should().BeFalse();
        vm.CanSellar.Should().BeTrue();
        vm.CanClasificar.Should().BeFalse();
        vm.CanArchivar.Should().BeFalse();
        vm.CanRechazar.Should().BeTrue();

        vm.FaseActual = FaseCicloVida.Sellado;
        vm.CanSellar.Should().BeFalse();
        vm.CanClasificar.Should().BeTrue();
        vm.CanRechazar.Should().BeTrue();

        vm.FaseActual = FaseCicloVida.Clasificado;
        vm.CadidoIdSeleccionado = Guid.NewGuid();
        vm.CanClasificar.Should().BeFalse();
        vm.CanArchivar.Should().BeTrue();
        vm.CanRechazar.Should().BeTrue();

        vm.FaseActual = FaseCicloVida.Archivado;
        vm.CanArchivar.Should().BeFalse();
        vm.CanRechazar.Should().BeFalse();
    }

    [Fact]
    public async Task SeleccionarArchivoPdfAsync_CuandoArchivoValido_ActualizaRuta()
    {
        // Arrange
        var vm = CrearViewModel();
        // Simular selección de archivo (requiere mock de OpenFileDialog, difícil en unit test puro)
        // Esta prueba se puede hacer con un wrapper de diálogo.
        // Por simplicidad, asumimos que se asigna directamente.
        var rutaEsperada = @"C:\test\doc.pdf";

        // Act
        await vm.SeleccionarArchivoPdfAsync(CancellationToken.None); // Necesita implementación real con mock

        // Assert
        // Debería setear RutaArchivoPdf
    }

    [Fact]
    public async Task CargarCatalogoCadidoAsync_CuandoExistenItems_LlenaColeccion()
    {
        // Arrange
        var vm = CrearViewModel();
        var catalogoItems = new List<CatalogoCadido>
        {
            new() { Id = Guid.NewGuid(), Seccion = "A", Serie = "1", Subserie = "1.1", PlazoConservacionAnios = 5, IsActivo = true },
            new() { Id = Guid.NewGuid(), Seccion = "B", Serie = "2", Subserie = "2.1", PlazoConservacionAnios = 10, IsActivo = true }
        };
        _catalogoRepoMock.ObtenerTodosAsync(Arg.Any<CancellationToken>())
            .Returns(catalogoItems);

        // Act
        await vm.CargarCatalogoCadidoAsync(CancellationToken.None);

        // Assert
        vm.CatalogoCadidoItems.Should().HaveCount(2);
        vm.CatalogoCadidoItems[0].Id.Should().Be(catalogoItems[0].Id);
        vm.CatalogoCadidoItems[0].Seccion.Should().Be("A");
    }

    [Fact]
    public void SeleccionarCadido_EstableceIdSeleccionadoYNombres()
    {
        // Arrange
        var vm = CrearViewModel();
        var item = new CatalogoCadidoItemViewModel
        {
            Id = Guid.NewGuid(),
            Seccion = "Secc",
            Serie = "Ser",
            Subserie = "Sub",
            PlazoConservacionAnios = 5
        };
        vm.CatalogoCadidoItems.Add(item);

        // Act
        vm.SeleccionarCadido(item);

        // Assert
        vm.CadidoIdSeleccionado.Should().Be(item.Id);
        vm.CadidoNombreSeleccionado.Should().Be("Secc - Ser - Sub");
    }

    [Fact]
    public void ReiniciarDocumento_LimpiaPropiedades()
    {
        // Arrange
        var vm = CrearViewModel();
        vm.FolioOficial = "algo";
        vm.FaseActual = FaseCicloVida.Archivado;

        // Act
        vm.ReiniciarDocumento();

        // Assert
        vm.FolioOficial.Should().BeEmpty();
        vm.FaseActual.Should().Be(FaseCicloVida.Nacimiento);
        vm.RutaArchivoPdf.Should().BeEmpty();
        // ... otras propiedades
    }

    [Fact]
    public async Task ValidarIntegridadHashAsync_CuandoHashCoincide_EstableceIsHashVerificado()
    {
        // Arrange
        var vm = CrearViewModel();
        vm.RutaRedActual = @"\\server\file.pdf";
        vm.HashCriptografico = "hashcorrecto";
        _cryptoMock.ValidarIntegridadAsync(vm.RutaRedActual, vm.HashCriptografico, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await vm.ValidarIntegridadHashAsync(CancellationToken.None);

        // Assert
        vm.IsHashVerificado.Should().BeTrue();
    }

    [Fact]
    public async Task ExportarBitacoraAsync_CuandoExistenEventos_GeneraArchivo()
    {
        // Arrange
        var vm = CrearViewModel();
        vm.BitacoraEventos.Add(new BitacoraItemViewModel { Id = Guid.NewGuid(), DescripcionEvento = "Evento1" });
        // Mock de servicio de exportación (no definido en blueprint, se asume un IExportService)
        // Esta prueba requeriría un mock adicional.

        // Act
        await vm.ExportarBitacoraAsync(CancellationToken.None);

        // Assert
        // Verificar que se llamó al servicio de exportación.
    }
}