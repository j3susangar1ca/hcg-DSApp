using Microsoft.UI.Xaml;

namespace GestionDocumental.Presentation;

public partial class App : Application
{
    private Window? m_window;   // ← nullable, se asigna en OnLaunched

    public App()
    {
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        m_window = new MainWindow();
        m_window.Activate();
    }
}
