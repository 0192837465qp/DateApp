using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using DateApp.Services;
using System.Timers;
using Microsoft.Maui.Storage;

namespace DateApp.Views
{
    [QueryProperty(nameof(Email), "email")]
    [QueryProperty(nameof(UserName), "username")]
    [QueryProperty(nameof(Password), "password")]
    public partial class EmailVerificationPage : ContentPage
    {
        private readonly EmailVerificationService _emailVerificationService;
        private readonly FirebaseService _firebaseService;
        private Entry[] _codeEntries;
        private System.Timers.Timer _resendTimer;
        private int _resendCountdown = 60;

        public string Email { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public EmailVerificationPage()
        {
            InitializeComponent();
            _emailVerificationService = new EmailVerificationService();
            _firebaseService = new FirebaseService();

            InitializeCodeEntries();
            SetupResendTimer();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (!string.IsNullOrEmpty(Email))
            {
                UserEmailLabel.Text = Email;
                // Focus pe primul câmp
                Code1Entry.Focus();
            }
        }

        private void InitializeCodeEntries()
        {
            _codeEntries = new Entry[]
            {
                Code1Entry, Code2Entry, Code3Entry,
                Code4Entry, Code5Entry, Code6Entry
            };
        }

        private void SetupResendTimer()
        {
            _resendTimer = new System.Timers.Timer(1000); // 1 secundă
            _resendTimer.Elapsed += OnResendTimerElapsed;
        }

        private void OnResendTimerElapsed(object sender, ElapsedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _resendCountdown--;

                if (_resendCountdown <= 0)
                {
                    _resendTimer.Stop();
                    ResendButton.IsEnabled = true;
                    ResendTimerLabel.IsVisible = false;
                    ResendButton.Text = "Resend Code";
                }
                else
                {
                    ResendTimerLabel.Text = $"Resend in {_resendCountdown}s";
                    ResendButton.Text = $"Resend ({_resendCountdown}s)";
                }
            });
        }

        private void OnCodeEntryTextChanged(object sender, TextChangedEventArgs e)
        {
            var entry = sender as Entry;

            if (entry == null) return;

            // Permite doar cifre
            if (!string.IsNullOrEmpty(e.NewTextValue) && !char.IsDigit(e.NewTextValue[0]))
            {
                entry.Text = e.OldTextValue;
                return;
            }

            // Auto-focus la următorul câmp
            if (!string.IsNullOrEmpty(e.NewTextValue))
            {
                var currentIndex = Array.IndexOf(_codeEntries, entry);
                if (currentIndex < _codeEntries.Length - 1)
                {
                    _codeEntries[currentIndex + 1].Focus();
                }
            }

            // Verifică dacă toate câmpurile sunt completate
            CheckIfCodeComplete();

            // Auto-verificare când toate câmpurile sunt completate
            if (IsCodeComplete())
            {
                MainThread.BeginInvokeOnMainThread(async () => await VerifyCodeAsync());
            }
        }

        private void OnCodeEntryFocused(object sender, FocusEventArgs e)
        {
            var entry = sender as Entry;
            if (entry != null && !string.IsNullOrEmpty(entry.Text))
            {
                // Selectează textul când se focus-ează
                entry.CursorPosition = 0;
                entry.SelectionLength = entry.Text.Length;
            }
        }

        private void CheckIfCodeComplete()
        {
            VerifyButton.IsEnabled = IsCodeComplete();
        }

        private bool IsCodeComplete()
        {
            foreach (var entry in _codeEntries)
            {
                if (string.IsNullOrEmpty(entry.Text))
                    return false;
            }
            return true;
        }

        private string GetEnteredCode()
        {
            var code = "";
            foreach (var entry in _codeEntries)
            {
                code += entry.Text ?? "";
            }
            return code;
        }

        private void ClearCodeEntries()
        {
            foreach (var entry in _codeEntries)
            {
                entry.Text = "";
            }
            Code1Entry.Focus();
        }

        private async void OnVerifyClicked(object sender, EventArgs e)
        {
            await VerifyCodeAsync();
        }

        private async Task VerifyCodeAsync()
        {
            if (!IsCodeComplete())
            {
                await DisplayAlert("Incomplete Code", "Please enter all 6 digits.", "OK");
                return;
            }

            try
            {
                // Show loading
                LoadingIndicator.IsVisible = true;
                VerifyButton.IsEnabled = false;

                var enteredCode = GetEnteredCode();
                var verificationResult = await _emailVerificationService.VerifyCodeAsync(Email, enteredCode);

                if (verificationResult.success)
                {
                    // Codul este corect, creează contul Firebase
                    await CreateFirebaseAccountAsync();
                }
                else
                {
                    await DisplayAlert("Verification Failed", verificationResult.message, "OK");
                    ClearCodeEntries();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Something went wrong. Please try again.", "OK");
                System.Diagnostics.Debug.WriteLine($"Verify error: {ex.Message}");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                VerifyButton.IsEnabled = IsCodeComplete();
            }
        }

        private async Task CreateFirebaseAccountAsync()
        {
            try
            {
                // Creează contul Firebase
                var result = await _firebaseService.RegisterUserAsync(Email, Password, UserName);

                if (result.success)
                {
                    // Salvează datele de sesiune
                    Preferences.Set("user_id", result.userId);
                    Preferences.Set("user_email", Email);
                    Preferences.Set("user_name", UserName);
                    Preferences.Set("is_authenticated", true);
                    Preferences.Set("email_verified", true);
                    Preferences.Set("auth_timestamp", DateTime.UtcNow.ToString());

                    await DisplayAlert("Success! 🎉",
                        $"Welcome to HeartSync, {UserName}! Your account has been created successfully.",
                        "Let's Start!");

                    // Navighează la pagina principală sau onboarding
                    await Shell.Current.GoToAsync("//login");
                }
                else
                {
                    await DisplayAlert("Account Creation Failed", result.message, "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to create account. Please try again.", "OK");
                System.Diagnostics.Debug.WriteLine($"Firebase registration error: {ex.Message}");
            }
        }

        private async void OnResendClicked(object sender, EventArgs e)
        {
            try
            {
                ResendButton.IsEnabled = false;

                var result = await _emailVerificationService.ResendVerificationCodeAsync(Email, UserName);

                if (result.success)
                {
                    await DisplayAlert("Code Sent", "New verification code sent to your email!", "OK");

                    // Începe countdown-ul
                    _resendCountdown = 60;
                    ResendTimerLabel.IsVisible = true;
                    _resendTimer.Start();

                    ClearCodeEntries();
                }
                else
                {
                    await DisplayAlert("Resend Failed", result.message, "OK");
                    ResendButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to resend code. Please try again.", "OK");
                ResendButton.IsEnabled = true;
                System.Diagnostics.Debug.WriteLine($"Resend error: {ex.Message}");
            }
        }

        private async void OnBackToRegistrationClicked(object sender, EventArgs e)
        {
            var result = await DisplayAlert("Go Back?",
                "Are you sure you want to go back? You'll need to enter your details again.",
                "Yes", "Cancel");

            if (result)
            {
                // Curăță datele de verificare
                _emailVerificationService.ClearVerificationData(Email);

                await Shell.Current.GoToAsync("//register");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _resendTimer?.Stop();
        }

        protected override bool OnBackButtonPressed()
        {
            // Previne navigarea înapoi accidentală
            MainThread.BeginInvokeOnMainThread(() =>
            {
                OnBackToRegistrationClicked(this, EventArgs.Empty);
            });

            return true;
        }
    }
}