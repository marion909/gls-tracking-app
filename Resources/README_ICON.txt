ICON SETUP INSTRUCTIONS
=======================

To complete the icon setup for your GLS Tracking App:

1. Convert the provided tracking icon image to ICO format:
   - Use an online converter like https://convertio.co/png-ico/ 
   - Or use a tool like GIMP, Paint.NET, or Photoshop
   - Recommended sizes: 16x16, 32x32, 48x48, 256x256

2. Save the converted icon as: c:\Projekte\gls-tracking-app\Resources\icon.ico

3. The project is already configured to use this icon:
   - Application icon (for .exe file): Set in GlsTrackingApp.csproj
   - Window icon (in title bar): Set in SimpleMainWindow.xaml

4. After placing the icon file, rebuild the project:
   dotnet build

The icon will then appear:
- In the window title bar
- In the taskbar when the app is running
- As the executable file icon in Windows Explorer
- In the Start Menu (if installed)

Current status:
✅ Project configuration updated
✅ XAML window configuration updated
❌ Icon file needs to be added manually

File expected at: Resources\icon.ico
