using GlsTrackingApp.Services;
using System;
using System.Threading.Tasks;

namespace GlsTrackingApp
{
    public class GlsAuthenticationTest
    {
        public static async Task RunAuthenticationTest()
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine("                        GLS AUTHENTICATION TEST");
            Console.WriteLine("================================================================================");
            Console.WriteLine();
            
            var authService = new GlsAuthenticationService();
            
            try
            {
                Console.WriteLine("🔐 Starte GLS Login-Test...");
                Console.WriteLine("👤 Benutzer: 0404000500-920001104");
                Console.WriteLine("🔑 Passwort: [VERBORGEN]");
                Console.WriteLine();
                
                Console.WriteLine("🌐 Öffne Browser und navigiere zur GLS Login-Seite...");
                
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
                    Console.WriteLine();
                    Console.WriteLine("📋 NÄCHSTE SCHRITTE:");
                    Console.WriteLine("   • Integration in die Hauptanwendung möglich");
                    Console.WriteLine("   • Erweiterte Tracking-Funktionen verfügbar");
                    Console.WriteLine("   • API-Zugang zum GLS-System");
                }
                else
                {
                    Console.WriteLine("❌ LOGIN FEHLGESCHLAGEN!");
                    Console.WriteLine("🔒 Die Anmeldedaten funktionieren nicht oder es gab ein technisches Problem.");
                    Console.WriteLine();
                    Console.WriteLine("🔍 MÖGLICHE URSACHEN:");
                    Console.WriteLine("   • Ungültige Anmeldedaten");
                    Console.WriteLine("   • Account gesperrt oder abgelaufen");
                    Console.WriteLine("   • GLS-System temporär nicht verfügbar");
                    Console.WriteLine("   • Zusätzliche Sicherheitsmaßnahmen erforderlich");
                }
                
                Console.WriteLine("================================================================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 FEHLER BEIM TEST: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("🔧 DEBUGGING-INFORMATION:");
                Console.WriteLine($"   • Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"   • Stack Trace: {ex.StackTrace}");
            }
            finally
            {
                authService.Dispose();
            }
            
            Console.WriteLine();
            Console.WriteLine("Drücken Sie eine beliebige Taste, um fortzufahren...");
            Console.ReadKey();
        }
    }
}
