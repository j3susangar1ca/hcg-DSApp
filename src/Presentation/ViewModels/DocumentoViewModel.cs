using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GestionDocumental.Application.DTOs;
using GestionDocumental.Application.Interfaces;
using GestionDocumental.Domain.Enums;
using GestionDocumental.Domain.Exceptions;
using Microsoft.UI.Dispatching;

namespace GestionDocumental.Presentation.ViewModels;

public sealed partial class DocumentoViewModel : ObservableObject, IDisposable
{
    private readonly IDocumentAnalyzerService _analyzerService;
    private readonly IOcrProcessor _ocrProcessor;
    private readonly ICryptoSealer _cryptoSealer;
    private readonly INetworkStorageManager _storageManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly DispatcherQueue _dispatcherQueue;
    
    private readonly WeakReferenceMessenger _messenger = WeakReferenceMessenger.Default;

    [ObservableProperty]
    private Guid _documentoId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFolioValid))]
    private string _folioOficial = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRemitenteValid))]
    private string _remitente = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAsuntoValid))]
    private string _asunto = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSellar))]
    private string _rutaRedActual = string.Empty;

    [ObservableProperty]
    private string _hashCriptografico = string.Empty;

    [ObservableProperty]
    private bool _isHashVerificado;

    [ObservableProperty]
    private EstatusAccion _estatusAccionSeleccionado = EstatusAccion.Archivar;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanIngresar), nameof(CanSellar), nameof(CanClasificar), nameof(CanArchivar), nameof(CanRechazar))]
    private FaseCicloVida _faseActual = FaseCicloVida.Nacimiento;

    [ObservableProperty]
    private bool _isUrgente;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanArchivar))]
    private Guid? _cadidoIdSeleccionado;

    [ObservableProperty]
    private string _cadidoNombreSeleccionado = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanIngresar))]
    private string _rutaArchivoPdf = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanIngresar), nameof(CanSellar), nameof(CanClasificar), nameof(CanArchivar), nameof(CanRechazar))]
    private bool _isProcessing;

    [ObservableProperty]
    private string _mensajeEstado = "Sistema listo para procesar documentos.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanClasificar))]
    private string _textoExtraidoOcr = string.Empty;

    public ObservableCollection<CatalogoCadidoItemViewModel> CatalogoCadidoItems { get; } = [];

    public ObservableCollection<BitacoraItemViewModel> BitacoraEventos { get; } = [];

    public bool IsFolioValid => !string.IsNullOrWhiteSpace(FolioOficial);
    public bool IsRemitenteValid => !string.IsNullOrWhiteSpace(Remitente);
    public bool IsAsuntoValid => !string.IsNullOrWhiteSpace(Asunto);

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
        
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread() 
            ?? throw new InvalidOperationException("ViewModel must be constructed on UI thread");
        
        _messenger.Register<FaseChangedMessage>(this, (r, m) => OnFaseChangedMessageReceived(m));
    }

    [RelayCommand]
    private async Task SeleccionarArchivoPdfAsync(CancellationToken ct)
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        picker.FileTypeFilter.Add(".pdf");
        
        // Simulación para compatibilidad hasta tener el setup completo del App.xaml.cs de WinUI 3
        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            RutaArchivoPdf = file.Path;
        }
    }

    [RelayCommand(CanExecute = nameof(CanIngresar))]
    private async Task IngresarDocumentoAsync(CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(RutaArchivoPdf);
        
        IsProcessing = true;
        MensajeEstado = "Ingresando documento...";

        try
        {
            RutaRedActual = await _storageManager.CopiarATemporalAsync(RutaArchivoPdf, ct).ConfigureAwait(false);

            await RegistrarEventoBitacoraAsync("Nacimiento", FaseCicloVida.Ingresado.ToString(), 
                "Documento ingresado. Origen: " + RutaArchivoPdf, ct).ConfigureAwait(false);

            _dispatcherQueue.TryEnqueue(() => 
            {
                FaseActual = FaseCicloVida.Ingresado;
                MensajeEstado = "Documento ingresado exitosamente";
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new ExcepcionDeNegocio("Error al ingresar: " + ex.Message, ex);
        }
        finally
        {
            _dispatcherQueue.TryEnqueue(() => IsProcessing = false);
        }
    }

    [RelayCommand(CanExecute = nameof(CanSellar))]
    private async Task SellarDocumentoAsync(CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(RutaRedActual);
        
        IsProcessing = true;
        MensajeEstado = "Sellando documento...";

        try
        {
            var hash = await _cryptoSealer.GenerarHashSha256Async(RutaRedActual, ct).ConfigureAwait(false);

            await RegistrarEventoBitacoraAsync("Ingresado", FaseCicloVida.Sellado.ToString(),
                "Documento sellado. Hash: " + hash[..16] + "...", ct).ConfigureAwait(false);

            _dispatcherQueue.TryEnqueue(() =>
            {
                HashCriptografico = hash;
                FaseActual = FaseCicloVida.Sellado;
                MensajeEstado = "Documento sellado exitosamente";
            });
        }
        finally
        {
            _dispatcherQueue.TryEnqueue(() => IsProcessing = false);
        }
    }

    [RelayCommand(CanExecute = nameof(CanClasificar))]
    private async Task ClasificarDocumentoAsync(CancellationToken ct)
    {
        IsProcessing = true;
        MensajeEstado = "Clasificando documento...";

        try
        {
            var texto = await _ocrProcessor.ExtraerTextoAsync(RutaRedActual, ct).ConfigureAwait(false);
            var analisis = await _analyzerService.AnalizarDocumentoAsync(texto, ct).ConfigureAwait(false);

            _dispatcherQueue.TryEnqueue(() =>
            {
                TextoExtraidoOcr = texto;
                FolioOficial = analisis.Folio ?? GenerateFolioAutomatico();
                Remitente = analisis.Remitente;
                Asunto = analisis.Asunto;
                IsUrgente = analisis.EsUrgente;
                EstatusAccionSeleccionado = analisis.ObtenerEstatusAccion();
                FaseActual = FaseCicloVida.Clasificado;
            });

            await RegistrarEventoBitacoraAsync("Sellado", FaseCicloVida.Clasificado.ToString(),
                "Clasificado: " + FolioOficial, ct).ConfigureAwait(false);
        }
        finally
        {
            _dispatcherQueue.TryEnqueue(() => IsProcessing = false);
        }
    }

    [RelayCommand(CanExecute = nameof(CanArchivar))]
    private async Task ArchivarDocumentoAsync(CancellationToken ct)
    {
        if (!CadidoIdSeleccionado.HasValue) throw new ExcepcionDeNegocio("CADIDO requerido para archivar");

        IsProcessing = true;
        
        try
        {
            var catalogo = await _unitOfWork.Catalogos
                .BuscarAsync(c => c.Id == CadidoIdSeleccionado.Value, ct)
                .ContinueWith(t => t.Result.FirstOrDefault(), ct).ConfigureAwait(false);

            if (catalogo == null) throw new ExcepcionDeNegocio("Catálogo no encontrado");

            var rutaFinal = await _storageManager.MoverADefinitivoAsync(
                RutaRedActual, catalogo.Subserie.Replace(" ", "_"), 
                DateTime.Now.Year, FolioOficial, ct).ConfigureAwait(false);

            _dispatcherQueue.TryEnqueue(() =>
            {
                RutaRedActual = rutaFinal;
                FaseActual = FaseCicloVida.Archivado;
            });
        }
        finally
        {
            _dispatcherQueue.TryEnqueue(() => IsProcessing = false);
        }
    }

    public async Task CargarCatalogoCadidoAsync(CancellationToken ct)
    {
        var catalogos = await _unitOfWork.Catalogos.ObtenerTodosAsync(ct).ConfigureAwait(false);
        var items = catalogos.Where(c => c.IsActivo)
            .Select(c => new CatalogoCadidoItemViewModel
            {
                Id = c.Id, Seccion = c.Seccion, Serie = c.Serie, 
                Subserie = c.Subserie, PlazoConservacionAnios = c.PlazoConservacionAnios
            }).ToList();

        _dispatcherQueue.TryEnqueue(() =>
        {
            CatalogoCadidoItems.Clear();
            foreach (var item in items) CatalogoCadidoItems.Add(item);
        });
    }

    private async Task RegistrarEventoBitacoraAsync(string faseAnterior, string faseNueva, string descripcion, CancellationToken ct)
    {
        var bitacora = new Domain.Entities.BitacoraTrazabilidad
        {
            DocumentoId = DocumentoId != default ? DocumentoId : Guid.NewGuid(),
            FaseAnterior = faseAnterior, FaseNueva = faseNueva, DescripcionEvento = descripcion
        };

        await _unitOfWork.Bitacoras.AgregarAsync(bitacora, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        _dispatcherQueue.TryEnqueue(() =>
        {
            BitacoraEventos.Add(new BitacoraItemViewModel
            {
                Id = bitacora.Id, Fecha = bitacora.FechaTransaccion,
                FaseAnterior = bitacora.FaseAnterior, FaseNueva = bitacora.FaseNueva,
                DescripcionEvento = bitacora.DescripcionEvento
            });
        });
    }

    private void OnFaseChangedMessageReceived(FaseChangedMessage m) { }
    
    private string GenerateFolioAutomatico()
    {
        var anio = DateTime.Now.Year;
        var numero = new Random().Next(1000, 9999);
        return "FOL-" + anio + "-" + numero;
    }

    public void Dispose()
    {
        _messenger.UnregisterAll(this);
        _unitOfWork.Dispose();
    }
}

public record FaseChangedMessage(FaseCicloVida NuevaFase);

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

