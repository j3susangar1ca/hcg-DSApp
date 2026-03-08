using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using GestionDocumental.Application.Interfaces;
using GestionDocumental.Infrastructure.Services;
using GestionDocumental.Infrastructure.Data;
using GestionDocumental.Presentation.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;

namespace GestionDocumental.Presentation;

public static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        Microsoft.UI.Xaml.Application.Start((p) =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            System.Threading.SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }
}
