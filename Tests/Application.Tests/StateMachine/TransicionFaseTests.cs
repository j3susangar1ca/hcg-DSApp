using FluentAssertions;
using GestionDocumental.Application.Interfaces;
using GestionDocumental.Domain.Enums;
using GestionDocumental.Domain.Exceptions;
using GestionDocumental.Domain.Entities;
using GestionDocumental.Presentation.ViewModels;
using NSubstitute;
using Xunit;

namespace GestionDocumental.Application.Tests.StateMachine;

public class TransicionFaseTests
{
    private readonly IDocumentAnalyzerService _analyzer = Substitute.For<IDocumentAnalyzerService>();
    private readonly IOcrProcessor _ocr = Substitute.For<IOcrProcessor>();
    private readonly ICryptoSealer _crypto = Substitute.For<ICryptoSealer>();
    private readonly INetworkStorageManager _storage = Substitute.For<INetworkStorageManager>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private DocumentoViewModel CrearVM() => new DocumentoViewModel(_analyzer, _ocr, _crypto, _storage, _uow);

    [Fact]
    public async Task Transicion_Archivado_SinCadidoId_DebeLanzarExcepcionDeNegocio()
    {
        var vm = CrearVM();
        vm.FaseActual = FaseCicloVida.Clasificado;
        vm.CadidoIdSeleccionado = null;
        var act = () => vm.ArchivarDocumentoAsync(CancellationToken.None);
        await act.Should().ThrowAsync<ExcepcionDeNegocio>().WithMessage("*no cumple las fases requeridas*");
    }

    [Fact]
    public async Task Transicion_Clasificado_A_Archivado_ConCadido_DebeSerValida()
    {
        var vm = CrearVM();
        vm.FaseActual = FaseCicloVida.Clasificado;
        var id = Guid.NewGuid();
        var cat = new CatalogoCadido { Id = id, Seccion = "A", Serie = "B", Subserie = "C" };
        _uow.Catalogos.BuscarAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CatalogoCadido, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CatalogoCadido> { cat });
        vm.CadidoIdSeleccionado = id;
        await vm.ArchivarDocumentoAsync(CancellationToken.None);
        vm.FaseActual.Should().Be(FaseCicloVida.Archivado);
    }

    [Fact]
    public async Task Transicion_A_Rechazado_DesdeNacimiento_DebeSerInvalida()
    {
        var vm = CrearVM();
        vm.FaseActual = FaseCicloVida.Nacimiento;
        var act = () => vm.RechazarDocumentoAsync(CancellationToken.None);
        await act.Should().ThrowAsync<ExcepcionDeNegocio>().WithMessage("*No se puede rechazar*");
    }
}
