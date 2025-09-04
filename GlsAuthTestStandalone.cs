using GlsTrackingApp.Services;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("                    GLS AUTHENTICATION TEST - STANDALONE");
        Console.WriteLine("================================================================================");
        Console.WriteLine();
        
        var authService = new GlsAuthenticationService();
        
        try
        {
            Console.WriteLine("🔐 Starte GLS Login-Test...");
            Console.WriteLine("👤 Benutzer: 0404000500-920001104");
            Console.WriteLine("🔑 Passwort: ***********");
            Console.WriteLine();
            
            bool loginSuccess = await authService.TestLoginAsync(
                username: "0404000500-920001104",
                password: "920001104"
            );
            
            Console.WriteLine();
            Console.WriteLine("================================================================================");
            
            if (loginSuccess)
            {
                Console.WriteLine("🎉 LOGIN ERFOLGREICH!");
                Console.WriteLine("✅ Die Anmeldedaten sind korrekt und funktionieren.");
                Console.WriteLine("🔓 Zugang zum GLS-Portal möglich.");
            }
            else
            {
                Console.WriteLine("❌ LOGIN FEHLGESCHLAGEN!");
                Console.WriteLine("🔒 Die Anmeldedaten funktionieren nicht oder es gab ein technisches Problem.");
            }
            
            Console.WriteLine("================================================================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 FEHLER BEIM TEST: {ex.Message}");
            Console.WriteLine($"🔧 Stack Trace: {ex.StackTrace}");
        }
        finally
        {
            authService.Dispose();
        }
        
        Console.WriteLine();
        Console.WriteLine("Drücken Sie eine beliebige Taste, um zu beenden...");
        Console.ReadKey();
    }
}
