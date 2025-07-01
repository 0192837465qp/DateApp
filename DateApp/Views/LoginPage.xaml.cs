using Microsoft.Maui.Controls;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DateApp.Services;

namespace DateApp.Views
{
    public partial class LoginPage : ContentPage
    {
        private readonly FirebaseService _firebaseService;
        private bool _isPasswordVisible = false;

        public LoginPage()
        {
            InitializeComponent();
            _firebaseService = new FirebaseService();
            CheckRememberedUser();
            AnimatePageLoad();
        }

        private async void AnimatePageLoad()
        {
            this.Opacity = 0;
            await this.FadeTo(1, 800, Easing.CubicOut);
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            try
            {
                HapticFeedback.Perform(HapticFeedbackType.Click);
            }
            catch { }

            await LoginButton.ScaleTo(0.98, 50, Easing.CubicOut);
            await LoginButton.ScaleTo(1, 50, Easing.CubicIn);

            if (string.IsNullOrWhiteSpace(EmailEntry.Text) ||
                string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                await ShowErrorAnimation();
                await DisplayAlert("Required Fields",
                    "Please enter your email and password to continue.",
                    "OK");
                return;
            }

            if (!IsValidEmail(EmailEntry.Text))
            {
                await ShowErrorAnimation();
                await DisplayAlert("Invalid Email",
                    "Please enter a valid email address.",
                    "OK");
                return;
            }

            await ShowLoadingState();

            try
            {
                bool loginSuccess = await AuthenticateUser(EmailEntry.Text, PasswordEntry.Text);

                if (loginSuccess)
                {
                    if (RememberMeCheckBox.IsChecked)
                    {
                        await SecureStorage.SetAsync("user_email", EmailEntry.Text);
                        Preferences.Set("remember_me", true);
                    }
                    else
                    {
                        SecureStorage.Remove("user_email");
                        Preferences.Set("remember_me", false);
                    }

                    Preferences.Set("is_authenticated", true);
                    Preferences.Set("auth_timestamp", DateTime.UtcNow.ToString());

                    await LoginButton.ScaleTo(1.05, 100);
                    await LoginButton.ScaleTo(1, 100);

                    await DisplayAlert("Welcome Back!", "Login successful! Main page coming soon.", "OK");
                }
                else
                {
                    await HideLoadingState();
                    await ShowErrorAnimation();

                    await DisplayAlert("Authentication Failed",
                        "The email or password you entered is incorrect. Please try again.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await HideLoadingState();
                await DisplayAlert("Connection Error",
                    "Unable to connect to our servers. Please check your internet connection and try again.",
                    "OK");

                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
            }
        }

        private async Task ShowLoadingState()
        {
            LoginButton.IsEnabled = false;
            LoadingContainer.IsVisible = true;
            LoadingContainer.Opacity = 0;
            await LoadingContainer.FadeTo(1, 200);
        }

        private async Task HideLoadingState()
        {
            await LoadingContainer.FadeTo(0, 200);
            LoadingContainer.IsVisible = false;
            LoginButton.IsEnabled = true;
        }

        private async Task ShowErrorAnimation()
        {
            await this.TranslateTo(-10, 0, 50);
            await this.TranslateTo(10, 0, 50);
            await this.TranslateTo(-10, 0, 50);
            await this.TranslateTo(10, 0, 50);
            await this.TranslateTo(0, 0, 50);
        }

        private void CheckRememberedUser()
        {
            if (Preferences.Get("remember_me", false))
            {
                Task.Run(async () =>
                {
                    try
                    {
                        var email = await SecureStorage.GetAsync("user_email");
                        if (!string.IsNullOrEmpty(email))
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                EmailEntry.Text = email;
                                RememberMeCheckBox.IsChecked = true;
                            });
                        }
                    }
                    catch { }
                });
            }
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                return Regex.IsMatch(email,
                    @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        private async Task<bool> AuthenticateUser(string email, string password)
        {
            var result = await _firebaseService.LoginAsync(email, password);

            if (result.success)
            {
                // Save user session
                Preferences.Set("user_id", _firebaseService.CurrentUserId);
                Preferences.Set("user_email", email);
            }

            return result.success;
        }

        private void OnShowPasswordClicked(object sender, EventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;
            PasswordEntry.IsPassword = !_isPasswordVisible;
        }

        private async void OnForgotPasswordTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Password Reset",
                "Password reset feature coming soon!",
                "OK");
        }

        private async void OnSignUpTapped(object sender, EventArgs e)
        {
            await this.FadeTo(0.5, 200);
            await Shell.Current.GoToAsync("//register");
            await this.FadeTo(1, 200);
        }

        private async void OnGoogleLoginClicked(object sender, EventArgs e)
        {
            try
            {
                HapticFeedback.Perform(HapticFeedbackType.Click);
            }
            catch { }

            await DisplayAlert("Coming Soon",
                "Google sign-in will be available in the next update.",
                "OK");
        }

        private async void OnFacebookLoginClicked(object sender, EventArgs e)
        {
            try
            {
                HapticFeedback.Perform(HapticFeedbackType.Click);
            }
            catch { }

            await DisplayAlert("Coming Soon",
                "Facebook sign-in will be available in the next update.",
                "OK");
        }

        protected override bool OnBackButtonPressed()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var result = await DisplayAlert("Exit HeartSync",
                    "Are you sure you want to exit?",
                    "Exit", "Cancel");

                if (result)
                {
                    Application.Current.Quit();
                }
            });

            return true;
        }
    }
}