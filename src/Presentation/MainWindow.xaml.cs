using Microsoft.UI.Xaml;
using GestionDocumental.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GestionDocumental.Presentation;

public sealed partial class MainWindow : Window
{
    public DocumentoViewModel ViewModel { get; }

    public MainWindow()
    {
        this.InitializeComponent();
        
        // Obtenemos el ViewModel del contenedor de servicios
        ViewModel = (Application.Current as App)!.Services.GetRequiredService<DocumentoViewModel>();
        
        // Enlazamos los eventos de los botones a los comandos del ViewModel
        BtnCargar.Click += async (s, e) => await ViewModel.SeleccionarArchivoPdfAsync(default);
        BtnIA.Click += async (s, e) => await ViewModel.ClasificarDocumentoAsync(default);
        BtnArchivar.Click += async (s, e) => await ViewModel.ArchivarDocumentoAsync(default);
        
        // Observamos cambios en la fase para actualizar la UI
        ViewModel.PropertyChanged += (s, e) => {
            if (e.PropertyName == nameof(ViewModel.FaseActual))
            {
                ActualizarInterfaz();
            }
        };
    }

    private void ActualizarInterfaz()
    {
        FaseInfoBar.Title = $"Fase Actual: {ViewModel.FaseActual}";
        BtnIA.IsEnabled = ViewModel.CanClasificar;
        BtnArchivar.IsEnabled = ViewModel.CanArchivar;
        
        if (ViewModel.FaseActual == Domain.Enums.FaseCicloVida.Ingresado)
        {
            // Auto-sellar después de ingresar (flujo optimizado)
            _ = ViewModel.SellarDocumentoAsync(default);
        }
    }
}
