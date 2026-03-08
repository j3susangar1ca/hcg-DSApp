using CommunityToolkit.Mvvm.ComponentModel;

namespace GestionDocumental.Presentation.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _tituloAplicacion = "Sistema de Gestión Documental CADIDO";

    [ObservableProperty]
    private string _versionAplicacion = "1.0.0";

    [ObservableProperty]
    private bool _isNavigationEnabled = true;

    [ObservableProperty]
    private object? _currentView;

    public MainViewModel()
    {
        // Inicialización de la vista principal
    }

    public void NavigateToIngreso()
    {
        CurrentView = new IngresoPageViewModel();
    }

    public void NavigateToClasificacion()
    {
        CurrentView = new ClasificacionPageViewModel();
    }

    public void NavigateToArchivado()
    {
        CurrentView = new ArchivadoPageViewModel();
    }

    public void NavigateToConsulta()
    {
        CurrentView = new ConsultaPageViewModel();
    }

    public async Task CerrarAplicacionAsync()
    {
        // Aquí se podría realizar limpieza antes de cerrar
        // Por ejemplo, guardar estado, confirmar cierre, etc.
    }
}

public class IngresoPageViewModel { }
public class ClasificacionPageViewModel { }
public class ArchivadoPageViewModel { }
public class ConsultaPageViewModel { }