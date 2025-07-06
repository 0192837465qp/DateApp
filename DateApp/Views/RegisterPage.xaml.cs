using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DateApp.Services;
using Microsoft.Maui.Storage;

namespace DateApp.Views
{
    public partial class RegisterPage : ContentPage
    {
        private readonly FirebaseService _firebaseService;
        private bool _isPasswordValid = false;
        private bool _doPasswordsMatch = false;

        public RegisterPage()
        {
            InitializeComponent();
            _firebaseService = new FirebaseService();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Fade in animation
            this.Opacity = 0;
            this.FadeTo(1, 500, Easing.CubicOut);
        }

        private void OnPasswordTextChanged(object sender, TextChangedEventArgs e)
        {
            var password = e.NewTextValue ?? string.Empty;
            var strength = CalculatePasswordStrength(password);

            // Update progress bar
            PasswordStrengthBar.Progress = strength.progress;
            PasswordStrengthBar.ProgressColor = strength.color;

            // Update label
            PasswordStrengthLabel.Text = strength.text;
            PasswordStrengthLabel.TextColor = strength.color;

            // Update icon
            PasswordStrengthIcon.Text = strength.icon;

            _isPasswordValid = strength.progress >= 0.5;

            // Check if passwords match
            if (!string.IsNullOrEmpty(ConfirmPasswordEntry.Text))
            {
                CheckPasswordMatch();
            }
        }

        private void OnConfirmPasswordTextChanged(object sender, TextChangedEventArgs e)
        {
            CheckPasswordMatch();
        }

        private void CheckPasswordMatch()
        {
            var password = PasswordEntry.Text ?? string.Empty;
            var confirmPassword = ConfirmPasswordEntry.Text ?? string.Empty;

            if (string.IsNullOrEmpty(confirmPassword))
            {
                PasswordMatchIcon.Text = "";
                _doPasswordsMatch = false;
            }
            else if (password == confirmPassword)
            {
                PasswordMatchIcon.Text = "✓";
                PasswordMatchIcon.TextColor = Color.FromRgb(0, 255, 0);
                _doPasswordsMatch = true;
            }
            else
            {
                PasswordMatchIcon.Text = "✗";
                PasswordMatchIcon.TextColor = Color.FromRgb(255, 0, 0);
                _doPasswordsMatch = false;
            }
        }

        private (double progress, Color color, string text, string icon) CalculatePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
                return (0, Color.FromRgb(128, 128, 128), "", "");

            int score = 0;

            // Length check
            if (password.Length >= 8) score++;
            if (password.Length >= 12) score++;

            // Character variety
            if (Regex.IsMatch(password, @"[a-z]")) score++;
            if (Regex.IsMatch(password, @"[A-Z]")) score++;
            if (Regex.IsMatch(password, @"[0-9]")) score++;
            if (Regex.IsMatch(password, @"[!@#$%^&*(),.?""':{}|<>]")) score++;

            return score switch
            {
                0 or 1 => (0.2, Color.FromRgb(255, 0, 0), "Weak", "😟"),
                2 or 3 => (0.4, Color.FromRgb(255, 165, 0), "Fair", "😐"),
                4 or 5 => (0.7, Color.FromRgb(255, 255, 0), "Good", "🙂"),
                _ => (1.0, Color.FromRgb(0, 255, 0), "Strong", "😊")
            };
        }

        private async void OnSignUpClicked(object sender, EventArgs e)
        {
            // Haptic feedback
            try { HapticFeedback.Perform(HapticFeedbackType.Click); } catch { }

            // Button animation
            await SignUpButton.ScaleTo(0.95, 50);
            await SignUpButton.ScaleTo(1, 50);

            // Validation
            if (!ValidateForm())
                return;

            // Show loading
            SignUpButton.IsVisible = false;
            LoadingIndicator.IsVisible = true;

            try
            {
                // Inițiază procesul de verificare email
                var emailVerificationService = new EmailVerificationService();
                var result = await emailVerificationService.SendVerificationCodeAsync(EmailEntry.Text, NameEntry.Text);

                if (result.success)
                {
                    await DisplayAlert("Verification Code Sent! 📧",
                        "We've sent a verification code to your email. Please check your inbox.",
                        "Continue");

                    // Navighează la pagina de verificare cu parametrii
                    await Shell.Current.GoToAsync($"emailverification?email={EmailEntry.Text}&username={NameEntry.Text}&password={PasswordEntry.Text}");
                }
                else
                {
                    await DisplayAlert("Email Send Failed", result.message, "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error",
                    "Something went wrong. Please try again.",
                    "OK");
                System.Diagnostics.Debug.WriteLine($"Registration error: {ex.Message}");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                SignUpButton.IsVisible = true;
            }
        }

        private bool ValidateForm()
        {
            // Name validation
            if (string.IsNullOrWhiteSpace(NameEntry.Text))
            {
                DisplayAlert("Required Field", "Please enter your full name.", "OK");
                return false;
            }

            // Email validation
            if (string.IsNullOrWhiteSpace(EmailEntry.Text))
            {
                DisplayAlert("Required Field", "Please enter your email address.", "OK");
                return false;
            }

            if (!IsValidEmail(EmailEntry.Text))
            {
                DisplayAlert("Invalid Email", "Please enter a valid email address.", "OK");
                return false;
            }

            // Password validation
            if (!_isPasswordValid)
            {
                DisplayAlert("Weak Password",
                    "Please create a stronger password with at least 8 characters.",
                    "OK");
                return false;
            }

            if (!_doPasswordsMatch)
            {
                DisplayAlert("Passwords Don't Match",
                    "Please make sure both passwords are the same.",
                    "OK");
                return false;
            }

            // Terms validation
            if (!TermsCheckBox.IsChecked)
            {
                DisplayAlert("Terms Required",
                    "Please agree to the Terms of Service and Privacy Policy.",
                    "OK");
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                return Regex.IsMatch(email,
                    @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
                    RegexOptions.IgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            try
            {
                // Haptic feedback
                HapticFeedback.Perform(HapticFeedbackType.Click);
            }
            catch { }

            // Animation
            await this.FadeTo(0.5, 200);

            // Navigate back to login page
            await Shell.Current.GoToAsync("//login");

            // Restore opacity
            await this.FadeTo(1, 200);
        }

        private async void OnSignInTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//login");
        }
    }
}