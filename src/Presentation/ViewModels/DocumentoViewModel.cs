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
    private readonly DispatcherQueue? _dispatcherQueue;

    [ObservableProperty] private Guid _documentoId;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsFolioValid))] private string _folioOficial = string.Empty;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsRemitenteValid))] private string _remitente = string.Empty;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IsAsuntoValid))] private string _asunto = string.Empty;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanSellar))] private string _rutaRedActual = string.Empty;
    [ObservableProperty] private string _hashCriptografico = string.Empty;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanIngresar), nameof(CanSellar), nameof(CanClasificar), nameof(CanArchivar), nameof(CanRechazar))] private FaseCicloVida _faseActual = FaseCicloVida.Nacimiento;
    [ObservableProperty] private bool _isUrgente;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanArchivar))] private Guid? _cadidoIdSeleccionado;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanIngresar))] private string _rutaArchivoPdf = string.Empty;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanIngresar), nameof(CanSellar), nameof(CanClasificar), nameof(CanArchivar), nameof(CanRechazar))] private bool _isProcessing;

    public ObservableCollection<CatalogoCadidoItemViewModel> CatalogoCadidoItems { get; } = [];
    public ObservableCollection<BitacoraItemViewModel> BitacoraEventos { get; } = [];

    public bool IsFolioValid => !string.IsNullOrWhiteSpace(FolioOficial);
    public bool IsRemitenteValid => !string.IsNullOrWhiteSpace(Remitente);
    public bool IsAsuntoValid => !string.IsNullOrWhiteSpace(Asunto);

    public bool CanIngresar => FaseActual == FaseCicloVida.Nacimiento && !string.IsNullOrEmpty(RutaArchivoPdf) && !IsProcessing;
    public bool CanSellar => FaseActual == FaseCicloVida.Ingresado && !string.IsNullOrEmpty(RutaRedActual) && !IsProcessing;
    public bool CanClasificar => FaseActual == FaseCicloVida.Sellado && !string.IsNullOrEmpty(RutaArchivoPdf) && !IsProcessing;
    public bool CanArchivar => FaseActual == FaseCicloVida.Clasificado && CadidoIdSeleccionado.HasValue && !IsProcessing;
    public bool CanRechazar => FaseActual != FaseCicloVida.Nacimiento && FaseActual != FaseCicloVida.Archivado && FaseActual != FaseCicloVida.Rechazado && !IsProcessing;

    public DocumentoViewModel(IDocumentAnalyzerService analyzerService, IOcrProcessor ocrProcessor, ICryptoSealer cryptoSealer, INetworkStorageManager storageManager, IUnitOfWork unitOfWork)
    {
        _analyzerService = analyzerService; _ocrProcessor = ocrProcessor; _cryptoSealer = cryptoSealer; _storageManager = storageManager; _unitOfWork = unitOfWork;
        try { _dispatcherQueue = DispatcherQueue.GetForCurrentThread(); } catch { _dispatcherQueue = null; }
    }

    private void EnqueueUI(Action action) { if (_dispatcherQueue != null) _dispatcherQueue.TryEnqueue(() => action()); else action(); }

    [RelayCommand(CanExecute = nameof(CanArchivar))]
    public async Task ArchivarDocumentoAsync(CancellationToken ct)
    {
        if (!CadidoIdSeleccionado.HasValue) throw new ExcepcionDeNegocio("El documento no cumple las fases requeridas para archivar (CADIDO faltante)");
        IsProcessing = true;
        try {
            var cat = (await _unitOfWork.Catalogos.BuscarAsync(c => c.Id == CadidoIdSeleccionado, ct)).FirstOrDefault();
            if (cat == null) throw new ExcepcionDeNegocio("Catalogo no encontrado");
            EnqueueUI(() => { FaseActual = FaseCicloVida.Archivado; });
        } finally { EnqueueUI(() => IsProcessing = false); }
    }

    [RelayCommand(CanExecute = nameof(CanRechazar))]
    public async Task RechazarDocumentoAsync(CancellationToken ct)
    {
        if (FaseActual == FaseCicloVida.Nacimiento) 
            throw new ExcepcionDeNegocio("No se puede rechazar un documento en fase de Nacimiento");
        
        EnqueueUI(() => { FaseActual = FaseCicloVida.Rechazado; });
        await Task.CompletedTask;
    }

    public void Dispose() => _unitOfWork.Dispose();
}

public class CatalogoCadidoItemViewModel { public Guid Id { get; set; } public string Seccion { get; set; } = ""; public string Serie { get; set; } = ""; public string Subserie { get; set; } = ""; public int PlazoConservacionAnios { get; set; } }
public class BitacoraItemViewModel { public Guid Id { get; set; } public DateTime Fecha { get; set; } public string FaseAnterior { get; set; } = ""; public string FaseNueva { get; set; } = ""; public string DescripcionEvento { get; set; } = ""; }
public record FaseChangedMessage(FaseCicloVida NuevaFase);
