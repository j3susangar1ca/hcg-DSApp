// Tests/Presentation.Tests/ViewModels/MainViewModelTests.cs
using FluentAssertions;
using GestionDocumental.Presentation.ViewModels;
using NSubstitute;

namespace GestionDocumental.Presentation.Tests.ViewModels;

public sealed class MainViewModelTests
{
    private readonly MainViewModel _sut = new();

    [Fact]
    public void Constructor_InicializaPropiedadesCorrectamente()
    {
        // Assert
        _sut.TituloAplicacion.Should().Be("Sistema de Gestión Documental CADIDO");
        _sut.VersionAplicacion.Should().Be("1.0.0");
        _sut.IsNavigationEnabled.Should().BeTrue();
        _sut.CurrentView.Should().BeNull();
    }

    [Fact]
    public void NavigateToIngreso_CambiaCurrentView()
    {
        // Act
        _sut.NavigateToIngreso();

        // Assert
        _sut.CurrentView.Should().BeOfType<IngresoPageViewModel>(); // Asumiendo que existe un VM para la página
    }

    [Fact]
    public void NavigateToClasificacion_CambiaCurrentView()
    {
        // Act
        _sut.NavigateToClasificacion();

        // Assert
        _sut.CurrentView.Should().BeOfType<ClasificacionPageViewModel>();
    }

    [Fact]
    public void NavigateToArchivado_CambiaCurrentView()
    {
        // Act
        _sut.NavigateToArchivado();

        // Assert
        _sut.CurrentView.Should().BeOfType<ArchivadoPageViewModel>();
    }

    [Fact]
    public void NavigateToConsulta_CambiaCurrentView()
    {
        // Act
        _sut.NavigateToConsulta();

        // Assert
        _sut.CurrentView.Should().BeOfType<ConsultaPageViewModel>();
    }

    [Fact]
    public async Task CerrarAplicacionAsync_EjecutaSinErrores()
    {
        // Act
        Func<Task> act = async () => await _sut.CerrarAplicacionAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }
}