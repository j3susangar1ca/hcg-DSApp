using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using GestionDocumental.Presentation.ViewModels;
using System;
using WinRT.Interop;

namespace GestionDocumental.Presentation;

public sealed partial class MainWindow : Window
{
    public DocumentoViewModel ViewModel { get; }

    public MainWindow()
    {
        this.InitializeComponent();
        
        // Intentar obtener el servicio de forma segura
        try {
            var app = (global::Microsoft.UI.Xaml.Application.Current as App);
            ViewModel = app.Services.GetRequiredService<DocumentoViewModel>();
            
            BtnCargar.Click += async (s, e) => {
                var handle = WindowNative.GetWindowHandle(this);
                await ViewModel.SeleccionarArchivoPdfAsync(handle);
                if (!string.IsNullOrEmpty(ViewModel.RutaArchivoPdf)) {
                    // Inicializar WebView2 solo cuando hay un archivo
                    await PdfViewer.EnsureCoreWebView2Async();
                    PdfViewer.Source = new Uri(ViewModel.RutaArchivoPdf);
                    await ViewModel.IngresarDocumentoAsync(default);
                }
            };

            BtnIA.Click += async (s, e) => await ViewModel.ClasificarDocumentoAsync(default);
            BtnArchivar.Click += async (s, e) => await ViewModel.ArchivarDocumentoAsync(default);

            ViewModel.PropertyChanged += (s, e) => {
                BtnIA.IsEnabled = ViewModel.CanClasificar;
                BtnArchivar.IsEnabled = ViewModel.CanArchivar;
                StatusText.Text = $"Fase: {ViewModel.FaseActual}";
            };
        }
        catch (Exception ex) {
            // Esto nos permitirá ver el error en la consola si algo falla internamente
            System.Diagnostics.Debug.WriteLine($"Error de inicio: {ex.Message}");
        }
    }
}