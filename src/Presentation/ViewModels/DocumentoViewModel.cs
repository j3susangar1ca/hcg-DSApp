using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GestionDocumental.Application.DTOs;
using GestionDocumental.Application.Interfaces;
using GestionDocumental.Domain.Enums;
using GestionDocumental.Domain.Exceptions;
using Microsoft.UI.Dispatching;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace GestionDocumental.Presentation.ViewModels;

public sealed partial class DocumentoViewModel : ObservableObject, IDisposable
{
    private readonly IDocumentAnalyzerService _analyzerService;
    private readonly IOcrProcessor _ocrProcessor;
    private readonly ICryptoSealer _cryptoSealer;
    private readonly INetworkStorageManager _storageManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly DispatcherQueue? _dispatcherQueue;

    [ObservableProperty] private string _folioOficial = string.Empty;
    [ObservableProperty] private string _remitente = string.Empty;
    [ObservableProperty] private string _asunto = string.Empty;
    [ObservableProperty] private string _rutaRedActual = string.Empty;
    [ObservableProperty] private string _rutaArchivoPdf = string.Empty;
    [ObservableProperty] private FaseCicloVida _faseActual = FaseCicloVida.Nacimiento;
    [ObservableProperty] private bool _isUrgente;
    [ObservableProperty] private bool _isProcessing;
    [ObservableProperty] private string _mensajeEstado = "Listo";
    [ObservableProperty] private Guid? _cadidoIdSeleccionado;

    public bool CanClasificar => FaseActual == FaseCicloVida.Sellado && !IsProcessing;
    public bool CanArchivar => FaseActual == FaseCicloVida.Clasificado && !IsProcessing;

    public DocumentoViewModel(IDocumentAnalyzerService analyzerService, IOcrProcessor ocrProcessor, ICryptoSealer cryptoSealer, INetworkStorageManager storageManager, IUnitOfWork unitOfWork)
    {
        _analyzerService = analyzerService; _ocrProcessor = ocrProcessor; _cryptoSealer = cryptoSealer; _storageManager = storageManager; _unitOfWork = unitOfWork;
        try { _dispatcherQueue = DispatcherQueue.GetForCurrentThread(); } catch { _dispatcherQueue = null; }
    }

    private void EnqueueUI(Action action) { if (_dispatcherQueue != null) _dispatcherQueue.TryEnqueue(() => action()); else action(); }

    public async Task SeleccionarArchivoPdfAsync(nint windowHandle)
    {
        var picker = new FileOpenPicker();
        InitializeWithWindow.Initialize(picker, windowHandle);
        picker.FileTypeFilter.Add(".pdf");
        var file = await picker.PickSingleFileAsync();
        if (file != null) RutaArchivoPdf = file.Path;
    }

    public async Task IngresarDocumentoAsync(CancellationToken ct)
    {
        IsProcessing = true;
        try {
            RutaRedActual = await _storageManager.CopiarATemporalAsync(RutaArchivoPdf, ct);
            EnqueueUI(() => { FaseActual = FaseCicloVida.Ingresado; });
            await SellarDocumentoAsync(ct);
        } finally { EnqueueUI(() => IsProcessing = false); }
    }

    public async Task SellarDocumentoAsync(CancellationToken ct)
    {
        var hash = await _cryptoSealer.GenerarHashSha256Async(RutaRedActual, ct);
        EnqueueUI(() => { FaseActual = FaseCicloVida.Sellado; });
    }

    public async Task ClasificarDocumentoAsync(CancellationToken ct)
    {
        IsProcessing = true;
        try {
            var texto = await _ocrProcessor.ExtraerTextoAsync(RutaRedActual, ct);
            var res = await _analyzerService.AnalizarDocumentoAsync(texto, ct);
            EnqueueUI(() => {
                Remitente = res.Remitente; Asunto = res.Asunto; IsUrgente = res.EsUrgente;
                FaseActual = FaseCicloVida.Clasificado;
            });
        } finally { EnqueueUI(() => IsProcessing = false); }
    }

    public async Task ArchivarDocumentoAsync(CancellationToken ct)
    {
        IsProcessing = true;
        try { EnqueueUI(() => { FaseActual = FaseCicloVida.Archivado; }); }
        finally { EnqueueUI(() => IsProcessing = false); }
    }

    public void Dispose() => _unitOfWork.Dispose();
}