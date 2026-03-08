using Microsoft.UI.Xaml;
using GestionDocumental.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace GestionDocumental.Presentation;

public sealed partial class MainWindow : Window
{
    public DocumentoViewModel ViewModel { get; }

    public MainWindow()
    {
        this.InitializeComponent();
        ViewModel = (Application.Current as App)!.Services.GetRequiredService<DocumentoViewModel>();
        
        // Eventos de botones
        BtnCargar.Click += async (s, e) => {
            await ViewModel.SeleccionarArchivoPdfAsync(default);
            if (!string.IsNullOrEmpty(ViewModel.RutaArchivoPdf))
            {
                CargarPdfEnVisor(ViewModel.RutaArchivoPdf);
                await ViewModel.IngresarDocumentoAsync(default);
            }
        };

        BtnIA.Click += async (s, e) => await ViewModel.ClasificarDocumentoAsync(default);
        BtnArchivar.Click += async (s, e) => await ViewModel.ArchivarDocumentoAsync(default);
        
        ViewModel.PropertyChanged += (s, e) => ActualizarInterfaz();
    }

    private void CargarPdfEnVisor(string path)
    {
        try {
            PlaceholderText.Visibility = Visibility.Collapsed;
            PdfViewer.Source = new Uri(path);
        } catch {
            StatusText.Text = "Error al visualizar el PDF.";
        }
    }

    private void ActualizarInterfaz()
    {
        FaseInfoBar.Title = $"Fase Actual: {ViewModel.FaseActual}";
        BtnIA.IsEnabled = ViewModel.CanClasificar;
        BtnArchivar.IsEnabled = ViewModel.CanArchivar;
    }
}
