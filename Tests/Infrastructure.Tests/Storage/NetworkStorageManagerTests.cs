// Tests/Infrastructure.Tests/Storage/NetworkStorageManagerTests.cs
using FluentAssertions;
using GestionDocumental.Infrastructure.Services;
using NSubstitute;

namespace GestionDocumental.Infrastructure.Tests.Storage;

public sealed class NetworkStorageManagerTests
{
    private readonly NetworkStorageManager _sut = new();

    [Fact]
    public async Task CopiarATemporal_RutaInexistente_DebeLanzar_FileNotFoundException()
    {
        // Arrange
        var rutaOrigen = @"Z:\fake\no_existe.pdf";

        // Act
        Func<Task> act = async () => await _sut.CopiarATemporalAsync(rutaOrigen);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact(Skip = "Requiere una ruta de red configurada")]
    public async Task CopiarATemporal_RutaValida_DebeCopiarArchivo()
    {
        // Esta prueba requiere una ruta de red real y permisos.
        // Se omite en un entorno genérico.
    }

    [Fact]
    public void GenerarNombreConvencional_DebeSeguir_PatronCADIDO_AÑO_FOLIO()
    {
        // Arrange
        var codigoCadido = "EXP-2024";
        var año = 2024;
        var folio = "0001-A";

        // Act
        var nombre = _sut.GenerarNombreConvencional(codigoCadido, año, folio);

        // Assert
        nombre.Should().Be("EXP-2024_2024_0001-A.pdf");
    }

    [Fact]
    public async Task VerificarAccesibilidad_RutaNoDisponible_DebeRetornar_False()
    {
        // Arrange
        var ruta = @"\\servidor\no_existe";

        // Act
        var accesible = await _sut.VerificarAccesibilidadAsync(ruta);

        // Assert
        accesible.Should().BeFalse();
    }

    [Fact(Skip = "Requiere una ruta de red real")]
    public async Task VerificarAccesibilidad_RutaDisponible_DebeRetornar_True()
    {
        // Arrange
        var ruta = @"\\server\shared";

        // Act
        var accesible = await _sut.VerificarAccesibilidadAsync(ruta);

        // Assert
        accesible.Should().BeTrue();
    }
}