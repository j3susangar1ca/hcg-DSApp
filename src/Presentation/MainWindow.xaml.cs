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
        var app = (global::Microsoft.UI.Xaml.Application.Current as App);
        ViewModel = app.Services.GetRequiredService<DocumentoViewModel>();

        BtnCargar.Click += async (s, e) => {
            var handle = WindowNative.GetWindowHandle(this);
            await ViewModel.SeleccionarArchivoPdfAsync(handle);
            if (!string.IsNullOrEmpty(ViewModel.RutaArchivoPdf)) {
                try { PdfViewer.Source = new Uri(ViewModel.RutaArchivoPdf); } catch {}
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
}