using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using GestionDocumental.Application.Interfaces;
using GestionDocumental.Infrastructure.Services;
using GestionDocumental.Infrastructure.Data;
using GestionDocumental.Presentation.ViewModels;

namespace GestionDocumental.Presentation;

public partial class App : global::Microsoft.UI.Xaml.Application
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
        var basePath = AppContext.BaseDirectory;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
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

    protected override void OnLaunched(global::Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        m_window = new MainWindow();
        m_window.Activate();
    }
}