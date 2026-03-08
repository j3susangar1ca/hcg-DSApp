// Tests/Domain.Tests/Entities/CatalogoCadidoTests.cs
using GestionDocumental.Domain.Entities;

namespace GestionDocumental.Domain.Tests.Entities;

public sealed class CatalogoCadidoTests
{
    [Fact]
    public void CrearCatalogo_ConPropiedadesRequeridas_DebeInicializarCorrectamente()
    {
        // Arrange
        var id = Guid.NewGuid();
        var seccion = "Sección A";
        var serie = "Serie 1";
        var subserie = "Subserie 1.1";
        var plazo = 5;

        // Act
        var catalogo = new CatalogoCadido
        {
            Id = id,
            Seccion = seccion,
            Serie = serie,
            Subserie = subserie,
            PlazoConservacionAnios = plazo,
            IsActivo = true
        };

        // Assert
        catalogo.Id.Should().Be(id);
        catalogo.Seccion.Should().Be(seccion);
        catalogo.Serie.Should().Be(serie);
        catalogo.Subserie.Should().Be(subserie);
        catalogo.PlazoConservacionAnios.Should().Be(plazo);
        catalogo.IsActivo.Should().BeTrue();
        catalogo.Documentos.Should().BeEmpty();
    }
}