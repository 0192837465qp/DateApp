using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace DateApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Register routes for navigation
            RegisterRoutes();

            // Check if user is already logged in
            CheckAuthenticationStatus();
        }

        private void RegisterRoutes()
        {
            // Main navigation routes
            Routing.RegisterRoute("login", typeof(Views.LoginPage));
            Routing.RegisterRoute("register", typeof(Views.RegisterPage));
            Routing.RegisterRoute("emailverification", typeof(Views.EmailVerificationPage));
            Routing.RegisterRoute("profilesetup", typeof(Views.ProfileSetupPage));
            Routing.RegisterRoute("swipe", typeof(Views.SwipePage));

            // Future routes (uncomment when pages are created)
            // Routing.RegisterRoute("forgotpassword", typeof(Views.ForgotPasswordPage));
            // Routing.RegisterRoute("chat", typeof(Views.ChatPage));
            // Routing.RegisterRoute("profile", typeof(Views.ProfilePage));
            // Routing.RegisterRoute("settings", typeof(Views.SettingsPage));
            // Routing.RegisterRoute("matches", typeof(Views.MatchesPage));
        }

        private async void CheckAuthenticationStatus()
        {
            try
            {
                // Check if user is authenticated
                bool isAuthenticated = Preferences.Get("is_authenticated", false);

                if (!isAuthenticated)
                {
                    // User not authenticated - go to login page
                    await GoToAsync("//login");
                    return;
                }

                // User is authenticated - check token validity
                var authTimestamp = Preferences.Get("auth_timestamp", string.Empty);
                if (!string.IsNullOrEmpty(authTimestamp))
                {
                    if (DateTime.TryParse(authTimestamp, out DateTime loginTime))
                    {
                        // If logged in more than 30 days ago, require re-login
                        if ((DateTime.UtcNow - loginTime).TotalDays > 30)
                        {
                            await LogoutUser();
                            return;
                        }
                    }
                }

                // Get user ID to verify profile in Firebase
                var userId = Preferences.Get("user_id", "");
                if (string.IsNullOrEmpty(userId))
                {
                    // No user ID - something went wrong, logout
                    await LogoutUser();
                    return;
                }

                // Check actual profile completion status in Firebase
                await VerifyProfileCompletionAsync(userId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckAuthenticationStatus error: {ex.Message}");
                // On error, go to login page
                await GoToAsync("//login");
            }
        }

        private async Task VerifyProfileCompletionAsync(string userId)
        {
            try
            {
                // Create FirebaseService instance to check profile
                var firebaseService = new DateApp.Services.FirebaseService();
                var userProfile = await firebaseService.GetUserProfileAsync(userId);

                if (userProfile == null)
                {
                    // Profile doesn't exist - go to profile setup
                    var userEmail = Preferences.Get("user_email", "");
                    var userName = Preferences.Get("user_name", "");
                    await GoToAsync($"profilesetup?userid={userId}&email={userEmail}&username={userName}");
                    return;
                }

                // Check if profile is actually completed
                bool isProfileComplete = IsProfileReallyComplete(userProfile);

                if (!isProfileComplete)
                {
                    // Profile incomplete - go to profile setup
                    var userEmail = Preferences.Get("user_email", "");
                    var userName = Preferences.Get("user_name", "");
                    await GoToAsync($"profilesetup?userid={userId}&email={userEmail}&username={userName}");
                    return;
                }

                // Profile is complete - update local preference and go to swipe
                Preferences.Set("profile_completed", true);
                await GoToAsync("//swipe");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"VerifyProfileCompletionAsync error: {ex.Message}");

                // Fallback to local preference check
                bool profileCompleted = Preferences.Get("profile_completed", false);
                if (profileCompleted)
                {
                    await GoToAsync("//swipe");
                }
                else
                {
                    var userEmail = Preferences.Get("user_email", "");
                    var userName = Preferences.Get("user_name", "");
                    await GoToAsync($"profilesetup?userid={userId}&email={userEmail}&username={userName}");
                }
            }
        }

        private bool IsProfileReallyComplete(DateApp.Services.UserProfile profile)
        {
            // Check all required fields for a complete profile
            if (string.IsNullOrWhiteSpace(profile.Name)) return false;
            if (profile.Age <= 0) return false;
            if (string.IsNullOrWhiteSpace(profile.Bio)) return false;
            if (profile.Photos == null || profile.Photos.Count < 3) return false;

            // Check if profile is marked as completed in Firebase
            return profile.ProfileCompleted;
        }

        private async Task LogoutUser()
        {
            try
            {
                // Clear all user preferences
                Preferences.Set("is_authenticated", false);
                Preferences.Set("profile_completed", false);
                Preferences.Remove("user_id");
                Preferences.Remove("user_email");
                Preferences.Remove("user_name");
                Preferences.Remove("auth_timestamp");

                // Clear secure storage
                SecureStorage.RemoveAll();

                // Navigate to login
                await GoToAsync("//login");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logout error: {ex.Message}");
                await GoToAsync("//login");
            }
        }

        protected override bool OnBackButtonPressed()
        {
            try
            {
                var currentPage = Current?.CurrentPage;

                // Handle back button based on current page
                if (currentPage is Views.LoginPage)
                {
                    // Don't go back from login page - show exit dialog instead
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        var result = await DisplayAlert("Exit HeartSync",
                            "Are you sure you want to exit the app?",
                            "Exit", "Cancel");

                        if (result)
                        {
                            Application.Current?.Quit();
                        }
                    });
                    return true;
                }

                if (currentPage is Views.RegisterPage)
                {
                    // Allow going back to login from register
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await GoToAsync("//login");
                    });
                    return true;
                }

                if (currentPage is Views.EmailVerificationPage)
                {
                    // Ask if user wants to go back to registration
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        var result = await DisplayAlert("Go Back?",
                            "Are you sure you want to go back to registration? You'll need to enter your details again.",
                            "Yes", "Cancel");

                        if (result)
                        {
                            await GoToAsync("//register");
                        }
                    });
                    return true;
                }

                if (currentPage is Views.ProfileSetupPage)
                {
                    // Don't allow going back from profile setup - user must complete or skip
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await DisplayAlert("Complete Profile",
                            "Please complete your profile setup or skip for now to continue using HeartSync.",
                            "OK");
                    });
                    return true;
                }

                if (currentPage is Views.SwipePage)
                {
                    // Main page - show exit dialog
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        var result = await DisplayAlert("Exit HeartSync",
                            "Are you sure you want to exit?",
                            "Exit", "Cancel");

                        if (result)
                        {
                            Application.Current?.Quit();
                        }
                    });
                    return true;
                }

                // Default behavior for other pages
                return base.OnBackButtonPressed();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnBackButtonPressed error: {ex.Message}");
                return base.OnBackButtonPressed();
            }
        }

        // Helper method for manual logout (can be called from settings page)
        public async Task PerformLogoutAsync()
        {
            await LogoutUser();
        }

        // Helper method to check if user is fully authenticated and ready
        public bool IsUserFullyAuthenticated()
        {
            var isAuthenticated = Preferences.Get("is_authenticated", false);
            var profileCompleted = Preferences.Get("profile_completed", false);
            var userId = Preferences.Get("user_id", "");

            return isAuthenticated && profileCompleted && !string.IsNullOrEmpty(userId);
        }

        // Helper method to navigate to specific page programmatically
        public async Task NavigateToPageAsync(string route)
        {
            try
            {
                await GoToAsync($"//{route}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NavigateToPageAsync error: {ex.Message}");
            }
        }
    }
}

//using Microsoft.Maui.Controls;
//using System;
//using System.Threading.Tasks;

//namespace DateApp
//{
//    public partial class AppShell : Shell
//    {
//        public AppShell()
//        {
//            InitializeComponent();

//            // Register routes for navigation
//            RegisterRoutes();

//            // Check if user is already logged in
//            CheckAuthenticationStatus();
//        }

//        private void RegisterRoutes()
//        {
//            // These routes can be navigated to using Shell.Current.GoToAsync("//routename")
//            Routing.RegisterRoute("login", typeof(Views.LoginPage));
//            Routing.RegisterRoute("register", typeof(Views.RegisterPage));
//            Routing.RegisterRoute("emailverification", typeof(Views.EmailVerificationPage));
//            Routing.RegisterRoute("profilesetup", typeof(Views.ProfileSetupPage));
//            Routing.RegisterRoute("mainapp", typeof(Views.MainAppPage));

//            // Uncomment these when pages are created:
//            // Routing.RegisterRoute("forgotpassword", typeof(Views.ForgotPasswordPage));
//            // Routing.RegisterRoute("onboarding", typeof(Views.OnboardingPage));
//            // Routing.RegisterRoute("profile/edit", typeof(Views.EditProfilePage));
//            // Routing.RegisterRoute("settings", typeof(Views.SettingsPage));
//            // Routing.RegisterRoute("chat", typeof(Views.ChatPage));
//        }

//        private async void CheckAuthenticationStatus()
//        {
//            // Check if user is authenticated
//            bool isAuthenticated = Preferences.Get("is_authenticated", false);

//            if (isAuthenticated)
//            {
//                // Check if token is still valid
//                var authTimestamp = Preferences.Get("auth_timestamp", string.Empty);
//                if (!string.IsNullOrEmpty(authTimestamp))
//                {
//                    if (DateTime.TryParse(authTimestamp, out DateTime loginTime))
//                    {
//                        // If logged in more than 30 days ago, require re-login
//                        if ((DateTime.UtcNow - loginTime).TotalDays > 30)
//                        {
//                            Preferences.Set("is_authenticated", false);
//                            await GoToAsync("//login");
//                            return;
//                        }
//                    }
//                }

//                // Check if profile is completed
//                bool profileCompleted = Preferences.Get("profile_completed", false);
//                var userId = Preferences.Get("user_id", "");
//                var userEmail = Preferences.Get("user_email", "");
//                var userName = Preferences.Get("user_name", "");

//                if (!profileCompleted && !string.IsNullOrEmpty(userId))
//                {
//                    // User is authenticated but profile not completed - go to profile setup
//                    await GoToAsync($"profilesetup?userid={userId}&email={userEmail}&username={userName}");
//                }
//                else if (profileCompleted)
//                {
//                    // User is authenticated and profile completed - go to main app
//                    await GoToAsync("//mainapp");
//                }
//                else
//                {
//                    // Something is wrong, go to login
//                    await GoToAsync("//login");
//                }
//            }
//            else
//            {
//                // User is not authenticated, already on login page
//                // No navigation needed
//            }
//        }

//        protected override bool OnBackButtonPressed()
//        {
//            // Handle back button navigation
//            if (Current?.CurrentPage is Views.LoginPage)
//            {
//                // Don't go back from login page
//                return true;
//            }

//            if (Current?.CurrentPage is Views.ProfileSetupPage)
//            {
//                // Don't go back from profile setup page - user must complete or skip
//                return true;
//            }

//            return base.OnBackButtonPressed();
//        }
//    }
//}