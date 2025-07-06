using System;
using System.Threading.Tasks;
using Firebase.Auth;
using Microsoft.Maui.Storage; // Replace Essentials with Storage
using static Microsoft.Maui.Authentication.WebAuthenticator;
using static Microsoft.Maui.Authentication.WebAuthenticator;

namespace DateApp.Services
{
    public class FacebookLoginService
    {
        // Facebook App ID - înlocuiește cu ID-ul tău real de la Facebook Developers
        private const string FacebookAppId = "YOUR_FACEBOOK_APP_ID";
        private const string FacebookAppSecret = "YOUR_FACEBOOK_APP_SECRET";

        private readonly FirebaseAuthProvider _authProvider;
        private readonly FirebaseService _firebaseService;

        public FacebookLoginService(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
            _authProvider = new FirebaseAuthProvider(new FirebaseConfig("AIzaSyDgRtqV-xuRLtbr0PE6zeUz7RXnqHHbQno"));
        }

        public async Task<(bool success, string message, string userId)> LoginWithFacebookAsync()
        {
            try
            {
                // 1. Obține Facebook Access Token prin Web Authenticator
                var facebookToken = await GetFacebookAccessTokenAsync();

                if (string.IsNullOrEmpty(facebookToken))
                {
                    return (false, "Facebook login was cancelled.", null);
                }

                // 2. Obține informații despre utilizator de la Facebook
                var userInfo = await GetFacebookUserInfoAsync(facebookToken);

                if (userInfo == null)
                {
                    return (false, "Failed to get user information from Facebook.", null);
                }

                // 3. Autentifică cu Firebase folosind Facebook token
                var firebaseAuth = await _authProvider.SignInWithOAuthAsync(FirebaseAuthType.Facebook, facebookToken);

                // 4. Verifică dacă utilizatorul există în baza de date
                var existingProfile = await _firebaseService.GetUserProfileAsync(firebaseAuth.User.LocalId);

                if (existingProfile == null)
                {
                    // 5. Creează profil nou pentru utilizatori noi
                    var newProfile = new UserProfile
                    {
                        Id = firebaseAuth.User.LocalId,
                        Email = userInfo.Email,
                        Name = userInfo.Name,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true,
                        ProfileCompleted = false,
                        Photos = !string.IsNullOrEmpty(userInfo.ProfilePictureUrl)
                            ? new List<string> { userInfo.ProfilePictureUrl }
                            : new List<string>()
                    };

                    var profileSaved = await _firebaseService.UpdateUserProfileAsync(newProfile);

                    if (!profileSaved)
                    {
                        return (false, "Failed to create user profile.", null);
                    }

                    // Salvează datele local
                    Preferences.Set("user_id", firebaseAuth.User.LocalId);
                    Preferences.Set("user_email", userInfo.Email);
                    Preferences.Set("user_name", userInfo.Name);
                    Preferences.Set("login_method", "facebook");

                    return (true, "Account created successfully with Facebook!", firebaseAuth.User.LocalId);
                }
                else
                {
                    // 6. Login pentru utilizatori existenți
                    // Actualizează ultima dată de login
                    existingProfile.LastLogin = DateTime.UtcNow;
                    await _firebaseService.UpdateUserProfileAsync(existingProfile);

                    // Salvează datele local
                    Preferences.Set("user_id", firebaseAuth.User.LocalId);
                    Preferences.Set("user_email", existingProfile.Email);
                    Preferences.Set("user_name", existingProfile.Name);
                    Preferences.Set("login_method", "facebook");

                    return (true, "Login successful!", firebaseAuth.User.LocalId);
                }
            }
            catch (TaskCanceledException)
            {
                return (false, "Facebook login was cancelled.", null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Facebook login error: {ex.Message}");
                return (false, "Failed to login with Facebook. Please try again.", null);
            }
        }

        private async Task<string> GetFacebookAccessTokenAsync()
        {
            try
            {
                // URL-uri pentru Facebook OAuth
                var authUrl = $"https://www.facebook.com/v18.0/dialog/oauth?" +
                             $"client_id={FacebookAppId}&" +
                             $"redirect_uri=https://www.facebook.com/connect/login_success.html&" +
                             $"scope=email,public_profile&" +
                             $"response_type=token";

                var callbackUrl = "https://www.facebook.com/connect/login_success.html";

                // Folosește Web Authenticator pentru login
                var authResult = await WebAuthenticator.AuthenticateAsync(
                    new WebAuthenticatorOptions
                    {
                        Url = new Uri(authUrl),
                        CallbackUrl = new Uri(callbackUrl),
                        PrefersEphemeralWebBrowserSession = true
                    });

                // Extrage access token din URL fragment
                if (authResult.Properties.TryGetValue("access_token", out var accessToken))
                {
                    return accessToken;
                }

                return null;
            }
            catch (TaskCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Facebook token error: {ex.Message}");
                return null;
            }
        }

        private async Task<FacebookUserInfo> GetFacebookUserInfoAsync(string accessToken)
        {
            try
            {
                using var httpClient = new HttpClient();

                var userInfoUrl = $"https://graph.facebook.com/me?fields=id,name,email,picture&access_token={accessToken}";
                var response = await httpClient.GetStringAsync(userInfoUrl);

                var userInfo = System.Text.Json.JsonSerializer.Deserialize<FacebookUserInfo>(response);
                return userInfo;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Facebook user info error: {ex.Message}");
                return null;
            }
        }
    }

    // Model pentru informațiile utilizatorului de la Facebook
    public class FacebookUserInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public FacebookPicture Picture { get; set; }

        public string ProfilePictureUrl => Picture?.Data?.Url;
    }

    public class FacebookPicture
    {
        public FacebookPictureData Data { get; set; }
    }

    public class FacebookPictureData
    {
        public string Url { get; set; }
    }
}