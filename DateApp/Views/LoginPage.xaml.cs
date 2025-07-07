using Microsoft.Maui.Controls;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DateApp.Services;
using System.Text.Json;
using System.Net.Http;

namespace DateApp.Views
{
    public partial class LoginPage : ContentPage
    {
        private readonly FirebaseService _firebaseService;
        private readonly HttpClient _httpClient;
        private bool _isPasswordVisible = false;

        // Facebook App ID - înlocuiește cu ID-ul tău real
        private const string FacebookAppId = "1467546754257674";

        public LoginPage()
        {
            InitializeComponent();
            _firebaseService = new FirebaseService();
            _httpClient = new HttpClient();
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
                var result = await AuthenticateUser(EmailEntry.Text, PasswordEntry.Text);

                if (result.success)
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
                    Preferences.Set("login_method", "email");

                    await LoginButton.ScaleTo(1.05, 100);
                    await LoginButton.ScaleTo(1, 100);

                    // Check if user needs to complete profile
                    await CheckProfileCompletionAndNavigate(result.userId);
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

        private async void OnFacebookLoginClicked(object sender, EventArgs e)
        {
            try
            {
                HapticFeedback.Perform(HapticFeedbackType.Click);
            }
            catch { }

            // Animație buton
            var button = sender as Button;
            await button.ScaleTo(0.95, 50);
            await button.ScaleTo(1, 50);

            // Schimbă textul butonului pentru loading
            var originalText = button.Text;
            button.Text = "Connecting...";
            button.IsEnabled = false;

            try
            {
                var result = await TestFacebookLoginAsync();

                if (result.success)
                {
                    // Setează autentificarea
                    Preferences.Set("is_authenticated", true);
                    Preferences.Set("auth_timestamp", DateTime.UtcNow.ToString());
                    Preferences.Set("login_method", "facebook");

                    // Check if this is a new user or existing user
                    var userId = Preferences.Get("user_id", "");
                    await CheckProfileCompletionAndNavigate(userId);
                }
                else
                {
                    await DisplayAlert("Facebook Login Failed",
                        result.message,
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error",
                    "Something went wrong with Facebook login. Please try again.",
                    "OK");

                System.Diagnostics.Debug.WriteLine($"Facebook login error: {ex.Message}");
            }
            finally
            {
                // Restabilește butonul
                button.Text = originalText;
                button.IsEnabled = true;
            }
        }

        private async Task CheckProfileCompletionAndNavigate(string userId)
        {
            try
            {
                // Check if user profile is completed
                var userProfile = await _firebaseService.GetUserProfileAsync(userId);

                if (userProfile != null && userProfile.ProfileCompleted)
                {
                    // Profile is complete, go to main app
                    await DisplayAlert("Welcome Back!", "Login successful!", "OK");
                    await Shell.Current.GoToAsync("//mainapp");
                }
                else
                {
                    // Profile needs to be completed
                    var userEmail = Preferences.Get("user_email", "");
                    var userName = Preferences.Get("user_name", "");

                    await DisplayAlert("Complete Your Profile",
                        "Please complete your profile to start using HeartSync!",
                        "Continue");

                    await Shell.Current.GoToAsync($"profilesetup?userid={userId}&email={userEmail}&username={userName}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Profile check error: {ex.Message}");
                // If there's an error checking profile, assume it needs completion
                var userEmail = Preferences.Get("user_email", "");
                var userName = Preferences.Get("user_name", "");
                await Shell.Current.GoToAsync($"profilesetup?userid={userId}&email={userEmail}&username={userName}");
            }
        }

        private async Task<(bool success, string message)> TestFacebookLoginAsync()
        {
            try
            {
                // Facebook App ID
                const string facebookAppId = "1467546754257674";

                // URL pentru Facebook OAuth - ACELAȘI callback în ambele locuri
                var callbackUrl = "https://localhost/auth/callback";

                var authUrl = $"https://www.facebook.com/v18.0/dialog/oauth?" +
                             $"client_id={facebookAppId}&" +
                             $"redirect_uri={callbackUrl}&" +
                             $"scope=email,public_profile&" +
                             $"response_type=token";

                System.Diagnostics.Debug.WriteLine($"=== FACEBOOK LOGIN DEBUG ===");
                System.Diagnostics.Debug.WriteLine($"App ID: {facebookAppId}");
                System.Diagnostics.Debug.WriteLine($"Auth URL: {authUrl}");
                System.Diagnostics.Debug.WriteLine($"Callback URL: {callbackUrl}");
                System.Diagnostics.Debug.WriteLine($"============================");

                // Test cu WebAuthenticator - folosind calea completă
                var authResult = await Microsoft.Maui.Authentication.WebAuthenticator.AuthenticateAsync(
                    new Microsoft.Maui.Authentication.WebAuthenticatorOptions
                    {
                        Url = new Uri(authUrl),
                        CallbackUrl = new Uri(callbackUrl),
                        PrefersEphemeralWebBrowserSession = true
                    });

                System.Diagnostics.Debug.WriteLine($"=== FACEBOOK RESULT ===");

                // Verifică rezultatul
                if (authResult?.Properties?.Count > 0)
                {
                    var debugInfo = "";
                    foreach (var prop in authResult.Properties)
                    {
                        debugInfo += $"{prop.Key}: {prop.Value}\n";
                        System.Diagnostics.Debug.WriteLine($"Property: {prop.Key} = {prop.Value}");
                    }

                    // Caută access token în URL
                    if (authResult.Properties.TryGetValue("access_token", out var accessToken))
                    {
                        System.Diagnostics.Debug.WriteLine($"SUCCESS: Found access token!");

                        // Simulate Facebook user creation/login
                        await SimulateFacebookUserLogin(accessToken);

                        return (true, $"Facebook login successful!");
                    }

                    // Caută în parametrul URL
                    if (authResult.Properties.TryGetValue("url", out var url))
                    {
                        System.Diagnostics.Debug.WriteLine($"URL received: {url}");

                        // Încearcă să extragi token din URL
                        if (url.Contains("access_token="))
                        {
                            var tokenStart = url.IndexOf("access_token=") + "access_token=".Length;
                            var tokenEnd = url.IndexOf("&", tokenStart);
                            if (tokenEnd == -1) tokenEnd = url.Length;

                            var extractedToken = url.Substring(tokenStart, tokenEnd - tokenStart);
                            System.Diagnostics.Debug.WriteLine($"Extracted token: {extractedToken}");

                            // Simulate Facebook user creation/login
                            await SimulateFacebookUserLogin(extractedToken);

                            return (true, $"Facebook login successful!");
                        }
                    }

                    return (false, $"Facebook returned data but no access token found. Properties: {debugInfo}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"No properties returned from Facebook");
                    return (false, "No authentication result received from Facebook.");
                }
            }
            catch (TaskCanceledException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Facebook login cancelled: {ex.Message}");
                return (false, "Facebook login was cancelled by user.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Facebook login error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return (false, $"Facebook login error: {ex.Message}");
            }
        }

        private async Task SimulateFacebookUserLogin(string accessToken)
        {
            try
            {
                // In a real app, you would:
                // 1. Use the access token to get user info from Facebook Graph API
                // 2. Check if user exists in your database
                // 3. Create or update user profile

                // For demo purposes, we'll simulate this
                var userId = "fb_user_" + Guid.NewGuid().ToString("N")[..8];
                var email = "facebook.user@example.com"; // In real app, get from Facebook API
                var name = "Facebook User"; // In real app, get from Facebook API

                // Save user data
                Preferences.Set("user_id", userId);
                Preferences.Set("user_email", email);
                Preferences.Set("user_name", name);

                // Check if user profile exists and is complete
                var userProfile = await _firebaseService.GetUserProfileAsync(userId);
                if (userProfile == null)
                {
                    // Create new user profile in Firebase
                    var newProfile = new UserProfile
                    {
                        Id = userId,
                        Email = email,
                        Name = name,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true,
                        ProfileCompleted = false // User needs to complete profile
                    };

                    await _firebaseService.UpdateUserProfileAsync(newProfile);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Simulate Facebook user error: {ex.Message}");
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

        private async Task<(bool success, string userId)> AuthenticateUser(string email, string password)
        {
            var result = await _firebaseService.LoginAsync(email, password);

            if (result.success)
            {
                // Save user session
                Preferences.Set("user_id", _firebaseService.CurrentUserId);
                Preferences.Set("user_email", email);

                return (true, _firebaseService.CurrentUserId);
            }

            return (false, null);
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