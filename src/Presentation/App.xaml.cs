using Microsoft.UI.Xaml;
using Microsoft.UI.Dispatching;

namespace GestionDocumental.Presentation;

public partial class App : Microsoft.UI.Xaml.Application
{
    private Window m_window;

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
