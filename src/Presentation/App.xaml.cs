using Microsoft.UI.Xaml;

namespace GestionDocumental.Presentation;

public partial class App : Microsoft.UI.Xaml.Application
{
    // Se marca como anulable con '?' para evitar la advertencia CS8618
    private Window? m_window;

    public App()
    {
        this.InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        m_window = new MainWindow();
        m_window.Activate();
    }
}
