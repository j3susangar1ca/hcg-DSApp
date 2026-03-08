// Tests/Domain.Tests/Entities/DocumentoPrincipalTests.cs
using GestionDocumental.Domain.Entities;
using GestionDocumental.Domain.Enums;
using FluentAssertions;

namespace GestionDocumental.Domain.Tests.Entities;

public sealed class DocumentoPrincipalTests
{
    [Fact]
    public void CrearDocumento_ConPropiedadesRequeridas_DebeInicializarCorrectamente()
    {
        // Arrange
        var id = Guid.NewGuid();
        var folio = "FOL-001";
        var remitente = "Juan Perez";
        var asunto = "Solicitud de permiso";
        var ruta = @"\\server\docs\temp.pdf";
        var hash = "a".PadLeft(64, '0');
        var estatus = EstatusAccion.Gestion;
        var fase = FaseCicloVida.Nacimiento;
        var fecha = DateTime.UtcNow;

        // Act
        var documento = new DocumentoPrincipal
        {
            Id = id,
            FolioOficial = folio,
            Remitente = remitente,
            Asunto = asunto,
            RutaRedActual = ruta,
            HashCriptografico = hash,
            EstatusAccion = estatus,
            FaseCicloVida = fase,
            FechaCreacion = fecha,
            IsUrgente = false
        };

        // Assert
        documento.Id.Should().Be(id);
        documento.FolioOficial.Should().Be(folio);
        documento.Remitente.Should().Be(remitente);
        documento.Asunto.Should().Be(asunto);
        documento.RutaRedActual.Should().Be(ruta);
        documento.HashCriptografico.Should().Be(hash);
        documento.EstatusAccion.Should().Be(estatus);
        documento.FaseCicloVida.Should().Be(fase);
        documento.FechaCreacion.Should().Be(fecha);
        documento.IsUrgente.Should().BeFalse();
        documento.CadidoId.Should().BeNull();
        documento.Bitacoras.Should().BeEmpty();
        documento.CatalogoCadido.Should().BeNull();
    }

    [Fact]
    public void CambiarFase_ActualizaPropiedad_Correctamente()
    {
        // Arrange
        var documento = new DocumentoPrincipal
        {
            Id = Guid.NewGuid(),
            FolioOficial = "FOL-001",
            Remitente = "R",
            Asunto = "A",
            RutaRedActual = "R",
            HashCriptografico = "H",
            EstatusAccion = EstatusAccion.Archivar,
            FaseCicloVida = FaseCicloVida.Nacimiento,
            FechaCreacion = DateTime.UtcNow
        };

        // Act
        documento.FaseCicloVida = FaseCicloVida.Ingresado;

        // Assert
        documento.FaseCicloVida.Should().Be(FaseCicloVida.Ingresado);
    }
}