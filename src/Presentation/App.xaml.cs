using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using GestionDocumental.Application.Interfaces;
using GestionDocumental.Infrastructure.Services;
using GestionDocumental.Infrastructure.Data;
using GestionDocumental.Presentation.ViewModels;

namespace GestionDocumental.Presentation;

// Usamos el nombre completo para evitar colisión con el namespace 'Application'
public partial class App : global::Microsoft.UI.Xaml.Application
{
    public IServiceProvider Services { get; }
    private Window m_window;

    public App()
    {
        Services = ConfigureServices();
        this.InitializeComponent();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddDbContext<DocumentoDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddHttpClient<IDocumentAnalyzerService, DocumentAnalyzerService>();
        services.Configure<GeminiOptions>(configuration.GetSection("Gemini"));
        services.AddSingleton<IOcrProcessor, OcrProcessor>();
        services.AddSingleton<ICryptoSealer, CryptoSealer>();
        services.AddSingleton<INetworkStorageManager, NetworkStorageManager>();
        services.AddTransient<DocumentoViewModel>();

        return services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        m_window = new MainWindow();
        m_window.Activate();
    }
}