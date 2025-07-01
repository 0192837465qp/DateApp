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
            // These routes can be navigated to using Shell.Current.GoToAsync("//routename")
            Routing.RegisterRoute("login", typeof(Views.LoginPage));
            Routing.RegisterRoute("register", typeof(Views.RegisterPage));
            // Uncomment these when pages are created:
            // Routing.RegisterRoute("forgotpassword", typeof(Views.ForgotPasswordPage));
            // Routing.RegisterRoute("onboarding", typeof(Views.OnboardingPage));
            // Routing.RegisterRoute("profile/edit", typeof(Views.EditProfilePage));
            // Routing.RegisterRoute("settings", typeof(Views.SettingsPage));
            // Routing.RegisterRoute("chat", typeof(Views.ChatPage));
        }

        private async void CheckAuthenticationStatus()
        {
            // Check if user is authenticated
            bool isAuthenticated = Preferences.Get("is_authenticated", false);

            if (isAuthenticated)
            {
                // Check if token is still valid
                var authTimestamp = Preferences.Get("auth_timestamp", string.Empty);
                if (!string.IsNullOrEmpty(authTimestamp))
                {
                    if (DateTime.TryParse(authTimestamp, out DateTime loginTime))
                    {
                        // If logged in more than 30 days ago, require re-login
                        if ((DateTime.UtcNow - loginTime).TotalDays > 30)
                        {
                            Preferences.Set("is_authenticated", false);
                            await GoToAsync("//login");
                            return;
                        }
                    }
                }

                // User is authenticated, stay on login for now (no main page yet)
                await DisplayAlert("Welcome Back!", "You're already logged in. Main page coming soon!", "OK");
            }
            else
            {
                // User is not authenticated, already on login page
                // No navigation needed
            }
        }

        protected override bool OnBackButtonPressed()
        {
            // Handle back button navigation
            if (Current?.CurrentPage is Views.LoginPage)
            {
                // Don't go back from login page
                return true;
            }

            return base.OnBackButtonPressed();
        }
    }
}