using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using GestionDocumental.Application.Interfaces;
using GestionDocumental.Infrastructure.Services;
using GestionDocumental.Infrastructure.Data;
using GestionDocumental.Presentation.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace GestionDocumental.Presentation;

public partial class App : Application
{
    public IServiceProvider Services { get; }
    private Window? m_window;

    public App()
    {
        Services = ConfigureServices();
        this.InitializeComponent();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Configuración
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // Base de Datos
        services.AddDbContext<DocumentoDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Servicios de Infraestructura
        services.AddHttpClient<IDocumentAnalyzerService, DocumentAnalyzerService>();
        services.Configure<GeminiOptions>(configuration.GetSection("Gemini"));
        
        services.AddSingleton<IOcrProcessor, OcrProcessor>();
        services.AddSingleton<ICryptoSealer, CryptoSealer>();
        services.AddSingleton<INetworkStorageManager, NetworkStorageManager>();

        // ViewModels
        services.AddTransient<DocumentoViewModel>();

        return services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        m_window = new MainWindow();
        m_window.Activate();
    }
}
