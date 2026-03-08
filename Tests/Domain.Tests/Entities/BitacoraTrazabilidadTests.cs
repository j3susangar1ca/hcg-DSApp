// Tests/Domain.Tests/Entities/BitacoraTrazabilidadTests.cs
using GestionDocumental.Domain.Entities;

namespace GestionDocumental.Domain.Tests.Entities;

public sealed class BitacoraTrazabilidadTests
{
    [Fact]
    public void CrearBitacora_ConPropiedadesRequeridas_DebeInicializarCorrectamente()
    {
        // Arrange
        var id = Guid.NewGuid();
        var documentoId = Guid.NewGuid();
        var faseAnterior = "Nacimiento";
        var faseNueva = "Ingresado";
        var fecha = DateTime.UtcNow;
        var descripcion = "Documento ingresado al sistema";

        // Act
        var bitacora = new BitacoraTrazabilidad
        {
            Id = id,
            DocumentoId = documentoId,
            FaseAnterior = faseAnterior,
            FaseNueva = faseNueva,
            FechaTransaccion = fecha,
            DescripcionEvento = descripcion
        };

        // Assert
        bitacora.Id.Should().Be(id);
        bitacora.DocumentoId.Should().Be(documentoId);
        bitacora.FaseAnterior.Should().Be(faseAnterior);
        bitacora.FaseNueva.Should().Be(faseNueva);
        bitacora.FechaTransaccion.Should().Be(fecha);
        bitacora.DescripcionEvento.Should().Be(descripcion);
        bitacora.DocumentoPrincipal.Should().BeNull();
    }
}