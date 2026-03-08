using System;

namespace GestionDocumental.Presentation;

public static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // Usamos nombres completos para evitar colisiones con el namespace 'Application'
        WinRT.ComWrappersSupport.InitializeComWrappers();
        
        global::Microsoft.UI.Xaml.Application.Start((p) =>
        {
            var dispatcherQueue = global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            var context = new global::Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(dispatcherQueue);
            global::System.Threading.SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }
}
