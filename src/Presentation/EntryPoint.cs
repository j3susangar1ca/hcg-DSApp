using Microsoft.UI.Xaml;
using System;

namespace GestionDocumental.Presentation;

public static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();
            global::Microsoft.UI.Xaml.Application.Start((p) =>
            {
                new App();
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine("FATAL ERROR:");
            Console.WriteLine(ex.ToString());
            Console.ReadLine(); // Pausa para leer el error
        }
    }
}
