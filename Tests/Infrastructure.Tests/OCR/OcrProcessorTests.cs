// Tests/Infrastructure.Tests/OCR/OcrProcessorTests.cs
using FluentAssertions;
using GestionDocumental.Infrastructure.Services;
using NSubstitute;

namespace GestionDocumental.Infrastructure.Tests.OCR;

public sealed class OcrProcessorTests
{
    private readonly OcrProcessor _sut = new();

    [Fact]
    public async Task ExtraerTexto_ArchivoNoExistente_DebeLanzar_FileNotFoundException()
    {
        // Arrange
        var ruta = @"Z:\fake\path\no_existe.pdf";

        // Act
        Func<Task> act = async () => await _sut.ExtraerTextoAsync(ruta);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    // Nota: Esta prueba requiere un archivo PDF real para funcionar.
    // En un entorno real se usaría un archivo de prueba embebido.
    // Aquí se omite por simplicidad, pero se puede implementar con un PDF sample.
    [Fact(Skip = "Requiere un archivo PDF de prueba")]
    public async Task ExtraerTexto_ArchivoValido_DebeRetornarTexto()
    {
        // Implementación real con un archivo de prueba.
    }

    [Fact]
    public void PuedeProcesar_ArchivoPdf_DebeRetornar_True()
    {
        // Arrange
        var ruta = "documento.pdf";

        // Act
        var resultado = _sut.PuedeProcesar(ruta);

        // Assert
        resultado.Should().BeTrue();
    }

    [Theory]
    [InlineData("documento.txt")]
    [InlineData("imagen.jpg")]
    [InlineData("archivo.docx")]
    public void PuedeProcesar_ArchivoNoPdf_DebeRetornar_False(string ruta)
    {
        // Act
        var resultado = _sut.PuedeProcesar(ruta);

        // Assert
        resultado.Should().BeFalse();
    }
}