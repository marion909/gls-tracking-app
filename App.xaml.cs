using System.Windows;
using System;

namespace GlsTrackingApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("App wird gestartet...");
            base.OnStartup(e);
            System.Diagnostics.Debug.WriteLine("App erfolgreich gestartet!");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fehler beim Starten der Anwendung: {ex.Message}\n\nStackTrace: {ex.StackTrace}", 
                "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

