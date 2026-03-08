using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestionDocumental.Application.DTOs;
using GestionDocumental.Application.Interfaces;
using GestionDocumental.Domain.Enums;
using GestionDocumental.Domain.Exceptions;

namespace GestionDocumental.Presentation.ViewModels;

public partial class DocumentoViewModel : ObservableObject
{
    private readonly IDocumentAnalyzerService _analyzerService;
    private readonly IOcrProcessor _ocrProcessor;
    private readonly ICryptoSealer _cryptoSealer;
    private readonly INetworkStorageManager _storageManager;
    private readonly IUnitOfWork _unitOfWork;

    [ObservableProperty]
    private string _rutaArchivoPdf = string.Empty;

    [ObservableProperty]
    private string _rutaRedActual = string.Empty;

    [ObservableProperty]
    private string _folioOficial = string.Empty;

    [ObservableProperty]
    private string _remitente = string.Empty;

    [ObservableProperty]
    private string _asunto = string.Empty;

    [ObservableProperty]
    private string _hashCriptografico = string.Empty;

    [ObservableProperty]
    private bool _isUrgente;

    [ObservableProperty]
    private FaseCicloVida _faseActual = FaseCicloVida.Nacimiento;

    [ObservableProperty]
    private EstatusAccion _estatusAccionSeleccionado = EstatusAccion.Archivar;

    [ObservableProperty]
    private Guid? _cadidoIdSeleccionado;

    [ObservableProperty]
    private string _cadidoNombreSeleccionado = string.Empty;

    [ObservableProperty]
    private string _textoExtraidoOcr = string.Empty;

    [ObservableProperty]
    private bool _isHashVerificado;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private string _mensajeEstado = string.Empty;

    public ObservableCollection<CatalogoCadidoItemViewModel> CatalogoCadidoItems { get; } = [];

    public ObservableCollection<BitacoraItemViewModel> BitacoraEventos { get; } = [];

    public bool CanIngresar => FaseActual == FaseCicloVida.Nacimiento && !string.IsNullOrEmpty(RutaArchivoPdf) && !IsProcessing;
    public bool CanSellar => FaseActual == FaseCicloVida.Ingresado && !string.IsNullOrEmpty(RutaRedActual) && !IsProcessing;
    public bool CanClasificar => FaseActual == FaseCicloVida.Sellado && !string.IsNullOrEmpty(TextoExtraidoOcr) && !IsProcessing;
    public bool CanArchivar => FaseActual == FaseCicloVida.Clasificado && CadidoIdSeleccionado.HasValue && !IsProcessing;
    public bool CanRechazar => FaseActual is FaseCicloVida.Ingresado or FaseCicloVida.Sellado or FaseCicloVida.Clasificado && !IsProcessing;

    public DocumentoViewModel(
        IDocumentAnalyzerService analyzerService,
        IOcrProcessor ocrProcessor,
        ICryptoSealer cryptoSealer,
        INetworkStorageManager storageManager,
        IUnitOfWork unitOfWork)
    {
        _analyzerService = analyzerService;
        _ocrProcessor = ocrProcessor;
        _cryptoSealer = cryptoSealer;
        _storageManager = storageManager;
        _unitOfWork = unitOfWork;

        // Observar cambios en la fase para actualizar comandos
        PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(FaseActual) or nameof(IsProcessing) or nameof(CadidoIdSeleccionado))
            {
                OnPropertyChanged(nameof(CanIngresar));
                OnPropertyChanged(nameof(CanSellar));
                OnPropertyChanged(nameof(CanClasificar));
                OnPropertyChanged(nameof(CanArchivar));
                OnPropertyChanged(nameof(CanRechazar));
            }
        };
    }

    [RelayCommand]
    public async Task SeleccionarArchivoPdfAsync(CancellationToken cancellationToken = default)
    {
        // En una implementación real, usaríamos un OpenFileDialog
        // Por ahora simulamos la selección
        RutaArchivoPdf = @"C:\temp\documento.pdf"; // Esto sería reemplazado por la ruta real seleccionada
    }

    [RelayCommand(CanExecute = nameof(CanIngresar))]
    public async Task IngresarDocumentoAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(RutaArchivoPdf))
        {
            throw new ExcepcionDeNegocio("Debe seleccionar un archivo PDF para ingresar.");
        }

        try
        {
            IsProcessing = true;
            MensajeEstado = "Ingresando documento...";

            // Copiar archivo a ubicación temporal en red
            RutaRedActual = await _storageManager.CopiarATemporalAsync(RutaArchivoPdf, cancellationToken);

            // Registrar evento en bitácora
            var descripcion = $"Documento ingresado al sistema. Origen: {RutaArchivoPdf}";
            await RegistrarEventoBitacoraAsync("Nacimiento", FaseCicloVida.Ingresado.ToString(), descripcion, cancellationToken);

            FaseActual = FaseCicloVida.Ingresado;
            MensajeEstado = "Documento ingresado exitosamente";
        }
        catch (Exception ex)
        {
            throw new ExcepcionDeNegocio($"Error al ingresar el documento: {ex.Message}", ex);
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSellar))]
    public async Task SellarDocumentoAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(RutaRedActual))
        {
            throw new ExcepcionDeNegocio("No se puede sellar un documento sin ubicación de red válida.");
        }

        try
        {
            IsProcessing = true;
            MensajeEstado = "Sellando documento...";

            // Generar hash criptográfico
            HashCriptografico = await _cryptoSealer.GenerarHashSha256Async(RutaRedActual, cancellationToken);

            // Registrar evento en bitácora
            var descripcion = $"Documento sellado criptográficamente. Hash: {HashCriptografico[..16]}...";
            await RegistrarEventoBitacoraAsync("Ingresado", FaseCicloVida.Sellado.ToString(), descripcion, cancellationToken);

            FaseActual = FaseCicloVida.Sellado;
            MensajeEstado = "Documento sellado exitosamente";
        }
        catch (Exception ex)
        {
            throw new ExcepcionDeNegocio($"Error al sellar el documento: {ex.Message}", ex);
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanClasificar))]
    public async Task ClasificarDocumentoAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(RutaRedActual))
        {
            throw new ExcepcionDeNegocio("No se puede clasificar un documento sin ubicación de red válida.");
        }

        try
        {
            IsProcessing = true;
            MensajeEstado = "Clasificando documento...";

            // Extraer texto usando OCR
            TextoExtraidoOcr = await _ocrProcessor.ExtraerTextoAsync(RutaRedActual, cancellationToken);

            // Analizar documento con IA
            var respuestaGemini = await _analyzerService.AnalizarDocumentoAsync(TextoExtraidoOcr, cancellationToken);

            // Actualizar propiedades del documento
            FolioOficial = respuestaGemini.Folio ?? GenerateFolioAutomatico();
            Remitente = respuestaGemini.Remitente;
            Asunto = respuestaGemini.Asunto;
            IsUrgente = respuestaGemini.EsUrgente;
            EstatusAccionSeleccionado = respuestaGemini.ObtenerEstatusAccion();

            // Registrar evento en bitácora
            var descripcion = $"Documento clasificado. Folio: {FolioOficial}, Remitente: {Remitente}, Estatus: {EstatusAccionSeleccionado}";
            await RegistrarEventoBitacoraAsync("Sellado", FaseCicloVida.Clasificado.ToString(), descripcion, cancellationToken);

            FaseActual = FaseCicloVida.Clasificado;
            MensajeEstado = "Documento clasificado exitosamente";
        }
        catch (Exception ex)
        {
            throw new ExcepcionDeNegocio($"Error al clasificar el documento: {ex.Message}", ex);
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanArchivar))]
    public async Task ArchivarDocumentoAsync(CancellationToken cancellationToken = default)
    {
        if (!CadidoIdSeleccionado.HasValue)
        {
            throw new ExcepcionDeNegocio("No se puede archivar un documento sin un catálogo CADIDO asociado.");
        }

        if (string.IsNullOrEmpty(RutaRedActual))
        {
            throw new ExcepcionDeNegocio("No se puede archivar un documento sin haber sido ingresado previamente.");
        }

        if (string.IsNullOrEmpty(HashCriptografico))
        {
            throw new ExcepcionDeNegocio("No se puede archivar un documento sin haber sido sellado previamente.");
        }

        try
        {
            IsProcessing = true;
            MensajeEstado = "Archivando documento...";

            // Mover archivo a ubicación definitiva
            var catalogo = await _unitOfWork.Catalogos.ObtenerPorIdAsync(CadidoIdSeleccionado.Value, cancellationToken);
            if (catalogo == null)
            {
                throw new ExcepcionDeNegocio("Catálogo CADIDO no encontrado.");
            }

            var anio = DateTime.Now.Year;
            var rutaFinal = await _storageManager.MoverADefinitivoAsync(
                RutaRedActual, catalogo.Subserie.Replace(" ", "_"), anio, FolioOficial, cancellationToken);

            RutaRedActual = rutaFinal;

            // Registrar evento en bitácora
            var descripcion = $"Documento archivado definitivamente en: {rutaFinal}";
            await RegistrarEventoBitacoraAsync("Clasificado", FaseCicloVida.Archivado.ToString(), descripcion, cancellationToken);

            FaseActual = FaseCicloVida.Archivado;
            MensajeEstado = "Documento archivado exitosamente";
        }
        catch (Exception ex)
        {
            throw new ExcepcionDeNegocio($"Error al archivar el documento: {ex.Message}", ex);
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRechazar))]
    public async Task RechazarDocumentoAsync(CancellationToken cancellationToken = default)
    {
        if (FaseActual == FaseCicloVida.Nacimiento)
        {
            throw new ExcepcionDeNegocio("No se puede rechazar un documento en fase de nacimiento.");
        }

        try
        {
            IsProcessing = true;
            MensajeEstado = "Rechazando documento...";

            // Registrar evento en bitácora
            var descripcion = $"Documento rechazado en la fase {FaseActual}. Motivo: Evaluación negativa.";
            await RegistrarEventoBitacoraAsync(FaseActual.ToString(), FaseCicloVida.Rechazado.ToString(), descripcion, cancellationToken);

            FaseActual = FaseCicloVida.Rechazado;
            MensajeEstado = "Documento rechazado";
        }
        catch (Exception ex)
        {
            throw new ExcepcionDeNegocio($"Error al rechazar el documento: {ex.Message}", ex);
        }
        finally
        {
            IsProcessing = false;
        }
    }

    public async Task CargarCatalogoCadidoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var catalogos = await _unitOfWork.Catalogos.ObtenerTodosAsync(cancellationToken);
            CatalogoCadidoItems.Clear();
            
            foreach (var catalogo in catalogos.Where(c => c.IsActivo))
            {
                CatalogoCadidoItems.Add(new CatalogoCadidoItemViewModel
                {
                    Id = catalogo.Id,
                    Seccion = catalogo.Seccion,
                    Serie = catalogo.Serie,
                    Subserie = catalogo.Subserie,
                    PlazoConservacionAnios = catalogo.PlazoConservacionAnios
                });
            }
        }
        catch (Exception ex)
        {
            throw new ExcepcionDeNegocio($"Error al cargar el catálogo CADIDO: {ex.Message}", ex);
        }
    }

    public void SeleccionarCadido(CatalogoCadidoItemViewModel item)
    {
        CadidoIdSeleccionado = item.Id;
        CadidoNombreSeleccionado = $"{item.Seccion} - {item.Serie} - {item.Subserie}";
    }

    public void ReiniciarDocumento()
    {
        RutaArchivoPdf = string.Empty;
        RutaRedActual = string.Empty;
        FolioOficial = string.Empty;
        Remitente = string.Empty;
        Asunto = string.Empty;
        HashCriptografico = string.Empty;
        IsUrgente = false;
        FaseActual = FaseCicloVida.Nacimiento;
        CadidoIdSeleccionado = null;
        CadidoNombreSeleccionado = string.Empty;
        TextoExtraidoOcr = string.Empty;
        IsHashVerificado = false;
        IsProcessing = false;
        MensajeEstado = string.Empty;
        BitacoraEventos.Clear();
    }

    public async Task ValidarIntegridadHashAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(RutaRedActual) || string.IsNullOrEmpty(HashCriptografico))
        {
            IsHashVerificado = false;
            return;
        }

        IsHashVerificado = await _cryptoSealer.ValidarIntegridadAsync(RutaRedActual, HashCriptografico, cancellationToken);
    }

    private async Task RegistrarEventoBitacoraAsync(string faseAnterior, string faseNueva, 
        string descripcion, CancellationToken cancellationToken)
    {
        var bitacora = new Domain.Entities.BitacoraTrazabilidad
        {
            DocumentoId = Guid.NewGuid(), // En implementación real, usar el ID del documento real
            FaseAnterior = faseAnterior,
            FaseNueva = faseNueva,
            DescripcionEvento = descripcion,
            FechaTransaccion = DateTime.UtcNow
        };

        await _unitOfWork.Bitacoras.AgregarAsync(bitacora, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        BitacoraEventos.Add(new BitacoraItemViewModel
        {
            Id = bitacora.Id,
            Fecha = bitacora.FechaTransaccion,
            FaseAnterior = bitacora.FaseAnterior,
            FaseNueva = bitacora.FaseNueva,
            DescripcionEvento = bitacora.DescripcionEvento
        });
    }

    private string GenerateFolioAutomatico()
    {
        var anio = DateTime.Now.Year;
        var numero = new Random().Next(1000, 9999);
        return $"FOL-{anio}-{numero}";
    }
}

public class CatalogoCadidoItemViewModel : ObservableObject
{
    public Guid Id { get; set; }
    public string Seccion { get; set; } = string.Empty;
    public string Serie { get; set; } = string.Empty;
    public string Subserie { get; set; } = string.Empty;
    public int PlazoConservacionAnios { get; set; }
}

public class BitacoraItemViewModel : ObservableObject
{
    public Guid Id { get; set; }
    public DateTime Fecha { get; set; }
    public string FaseAnterior { get; set; } = string.Empty;
    public string FaseNueva { get; set; } = string.Empty;
    public string DescripcionEvento { get; set; } = string.Empty;
}