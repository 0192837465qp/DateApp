using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml; // Add this namespace
using System;
using System.Threading.Tasks;
using DateApp.Services;

namespace DateApp.Views
{
    public partial class MainAppPage : ContentPage
    {
        private readonly FirebaseService _firebaseService;

        public MainAppPage()
        {
            InitializeComponent(); // Ensure this method is generated by XAML
            _firebaseService = new FirebaseService();
            LoadUserInfo();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Entrance animation
            this.Opacity = 0;
            this.FadeTo(1, 600, Easing.CubicOut);
        }

        private void LoadUserInfo()
        {
            var userName = Preferences.Get("user_name", "User");
            WelcomeLabel.Text = $"Welcome back, {userName}!";
        }

        private async void OnViewProfileClicked(object sender, EventArgs e)
        {
            try
            {
                // Haptic feedback
                HapticFeedback.Perform(HapticFeedbackType.Click);
            }
            catch { }

            // Button animation
            var button = sender as Button;
            await button.ScaleTo(0.95, 50);
            await button.ScaleTo(1, 50);

            await DisplayAlert("Profile",
                "Profile viewing feature coming soon! You'll be able to view and edit your profile here.",
                "OK");
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            try
            {
                // Haptic feedback
                HapticFeedback.Perform(HapticFeedbackType.Click);
            }
            catch { }

            // Button animation
            var button = sender as Button;
            await button.ScaleTo(0.95, 50);
            await button.ScaleTo(1, 50);

            await DisplayAlert("Settings",
                "Settings page coming soon! You'll be able to manage your account preferences here.",
                "OK");
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            var result = await DisplayAlert("Logout",
                "Are you sure you want to logout?",
                "Logout", "Cancel");

            if (result)
            {
                try
                {
                    // Clear all preferences
                    Preferences.Clear();
                    SecureStorage.RemoveAll();

                    // Logout from Firebase
                    _firebaseService.Logout();

                    await DisplayAlert("Logged Out", "You have been successfully logged out.", "OK");

                    // Navigate back to login
                    await Shell.Current.GoToAsync("//login");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Logout error: {ex.Message}");
                    await DisplayAlert("Error", "Failed to logout. Please try again.", "OK");
                }
            }
        }

        protected override bool OnBackButtonPressed()
        {
            // Prevent back navigation from main app
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