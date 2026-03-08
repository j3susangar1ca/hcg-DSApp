// Tests/Infrastructure.Tests/Crypto/CryptoSealerTests.cs
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using GestionDocumental.Infrastructure.Services;

namespace GestionDocumental.Infrastructure.Tests.Crypto;

public sealed class CryptoSealerTests
{
    private readonly CryptoSealer _sut = new();

    [Fact]
    public async Task Hash_ArchivoOriginal_CoincideCon_HashRecalculado()
    {
        // Arrange
        var ruta = Path.GetTempFileName();
        await File.WriteAllTextAsync(ruta, "contenido de prueba");

        // Act
        var hash1 = await _sut.GenerarHashSha256Async(ruta);
        var hash2 = await _sut.GenerarHashSha256Async(ruta);

        // Assert
        hash1.Should().Be(hash2);

        // Cleanup
        File.Delete(ruta);
    }

    [Fact]
    public async Task Hash_ArchivoModificado_NoCoincideCon_HashOriginal()
    {
        // Arrange
        var ruta = Path.GetTempFileName();
        await File.WriteAllTextAsync(ruta, "contenido original");
        var hashOriginal = await _sut.GenerarHashSha256Async(ruta);

        // Act
        await File.AppendAllTextAsync(ruta, "modificación");
        var hashModificado = await _sut.GenerarHashSha256Async(ruta);

        // Assert
        hashModificado.Should().NotBe(hashOriginal);
        var integridad = await _sut.ValidarIntegridadAsync(ruta, hashOriginal);
        integridad.Should().BeFalse();

        // Cleanup
        File.Delete(ruta);
    }

    [Fact]
    public async Task Hash_DebeSer_StringHexadecimal_64Caracteres()
    {
        // Arrange
        var ruta = Path.GetTempFileName();
        await File.WriteAllTextAsync(ruta, "algún contenido");

        // Act
        var hash = await _sut.GenerarHashSha256Async(ruta);

        // Assert
        hash.Should().HaveLength(64);
        hash.Should().MatchRegex("^[0-9a-f]{64}$");

        // Cleanup
        File.Delete(ruta);
    }

    [Fact]
    public async Task Hash_ArchivoVacio_DebeSer_HashConocido()
    {
        // Arrange
        var ruta = Path.GetTempFileName();
        await File.WriteAllTextAsync(ruta, ""); // archivo vacío

        // Act
        var hash = await _sut.GenerarHashSha256Async(ruta);

        // Assert
        var hashVacioEsperado = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
        hash.Should().Be(hashVacioEsperado);

        // Cleanup
        File.Delete(ruta);
    }

    [Fact]
    public async Task GenerarHashSha256Async_ArchivoNoExistente_DebeLanzarFileNotFoundException()
    {
        // Arrange
        var ruta = @"C:\no_existe.pdf";

        // Act
        Func<Task> act = async () => await _sut.GenerarHashSha256Async(ruta);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }
}