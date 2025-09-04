using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GlsTrackingApp.Security;
using GlsTrackingApp.Config;

namespace GlsTrackingApp.Security
{
    public class LoginDialog
    {
        public static bool ShowLoginDialog(out string masterPassword, bool isFirstTime = false)
        {
            string tempPassword = string.Empty; // Temporäre Variable für Lambda
            
            var dialog = new Window
            {
                Title = isFirstTime ? "Master-Passwort festlegen" : "GLS Tracking App - Anmeldung",
                Width = 450,
                Height = isFirstTime ? 350 : 280,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow,
                Topmost = true,
                Icon = null
            };

            var grid = new Grid { Margin = new Thickness(20) };
            
            // Row definitions
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Title
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Info
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2: Password Label
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 3: Password Box
            if (isFirstTime)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 4: Confirm Label
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 5: Confirm Box
            }
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Spacer
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Buttons

            // Title
            var title = new TextBlock
            {
                Text = isFirstTime ? "🔐 Master-Passwort festlegen" : "🔐 Anmeldung erforderlich",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            Grid.SetRow(title, 0);
            grid.Children.Add(title);

            // Info text
            var infoText = new TextBlock
            {
                Text = isFirstTime ? 
                    "Bitte legen Sie ein Master-Passwort fest.\nDieses schützt alle gespeicherten Daten der Anwendung." :
                    "Bitte geben Sie Ihr Master-Passwort ein.",
                FontSize = 12,
                Foreground = System.Windows.Media.Brushes.Gray,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(infoText, 1);
            grid.Children.Add(infoText);

            // Password label
            var passwordLabel = new TextBlock
            {
                Text = isFirstTime ? "Master-Passwort:" : "Passwort:",
                Margin = new Thickness(0, 0, 0, 5)
            };
            Grid.SetRow(passwordLabel, 2);
            grid.Children.Add(passwordLabel);

            // Password box
            var passwordBox = new PasswordBox
            {
                Height = 30,
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 15)
            };
            Grid.SetRow(passwordBox, 3);
            grid.Children.Add(passwordBox);

            PasswordBox? confirmPasswordBox = null;
            if (isFirstTime)
            {
                // Confirm password label
                var confirmLabel = new TextBlock
                {
                    Text = "Passwort bestätigen:",
                    Margin = new Thickness(0, 0, 0, 5)
                };
                Grid.SetRow(confirmLabel, 4);
                grid.Children.Add(confirmLabel);

                // Confirm password box
                confirmPasswordBox = new PasswordBox
                {
                    Height = 30,
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 15)
                };
                Grid.SetRow(confirmPasswordBox, 5);
                grid.Children.Add(confirmPasswordBox);
            }

            // Security info for first time
            if (isFirstTime)
            {
                var securityInfo = new TextBlock
                {
                    Text = "💡 Das Passwort sollte mindestens 6 Zeichen lang sein.\nVergessen Sie dieses Passwort nicht - es kann nicht wiederhergestellt werden!",
                    FontSize = 10,
                    Foreground = System.Windows.Media.Brushes.Orange,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 10),
                    TextWrapping = TextWrapping.Wrap
                };
                Grid.SetRow(securityInfo, isFirstTime ? 6 : 4);
                grid.Children.Add(securityInfo);
            }

            // Buttons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var cancelButton = new Button
            {
                Content = "Abbrechen",
                Width = 80,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0)
            };
            cancelButton.Click += (s, e) => 
            {
                dialog.DialogResult = false;
                dialog.Close();
            };

            var okButton = new Button
            {
                Content = isFirstTime ? "Erstellen" : "Anmelden",
                Width = 80,
                Height = 30,
                IsDefault = true
            };

            // OK Button event handler
            okButton.Click += (s, e) => ValidateAndClose();
            
            // Enter key handling
            passwordBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    if (isFirstTime && confirmPasswordBox != null)
                        confirmPasswordBox.Focus();
                    else
                        ValidateAndClose();
                }
            };

            if (confirmPasswordBox != null)
            {
                confirmPasswordBox.KeyDown += (s, e) =>
                {
                    if (e.Key == Key.Enter)
                        ValidateAndClose();
                };
            }

            void ValidateAndClose()
            {
                var password = passwordBox.Password;
                
                if (string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Bitte geben Sie ein Passwort ein.", "Eingabe erforderlich",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    passwordBox.Focus();
                    return;
                }

                if (isFirstTime)
                {
                    if (password.Length < 6)
                    {
                        MessageBox.Show("Das Passwort muss mindestens 6 Zeichen lang sein.", "Passwort zu kurz",
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        passwordBox.Focus();
                        return;
                    }

                    if (confirmPasswordBox?.Password != password)
                    {
                        MessageBox.Show("Die Passwörter stimmen nicht überein.", "Passwort-Bestätigung",
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        confirmPasswordBox?.Focus();
                        return;
                    }
                }
                else
                {
                    // Verify existing password
                    var config = AppConfig.Instance;
                    if (!EncryptionService.VerifyPassword(password, config.MasterPasswordHash))
                    {
                        MessageBox.Show("Falsches Passwort!", "Anmeldung fehlgeschlagen",
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                        passwordBox.Clear();
                        passwordBox.Focus();
                        return;
                    }
                }

                tempPassword = password; // Temporäre Variable setzen
                dialog.DialogResult = true;
                dialog.Close();
            }

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(okButton);

            Grid.SetRow(buttonPanel, isFirstTime ? 7 : 5);
            grid.Children.Add(buttonPanel);

            dialog.Content = grid;
            
            // Focus password box when dialog opens
            dialog.Loaded += (s, e) => passwordBox.Focus();

            var result = dialog.ShowDialog();
            masterPassword = tempPassword; // Temporäre Variable zuweisen
            return result == true;
        }
    }
}
