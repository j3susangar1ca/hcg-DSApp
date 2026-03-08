using Microsoft.UI.Xaml;
using Microsoft.UI.Dispatching;
using System;

namespace GestionDocumental.Presentation;

public partial class App : Microsoft.UI.Xaml.Application
{
    private Window? m_window;

    public App()
    {
        // Esto asegura que el contexto de sincronización sea el de WinUI
        this.InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        m_window = new MainWindow();
        m_window.Activate();
    }
}
