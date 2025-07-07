using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DateApp.Services;

namespace DateApp.Views
{
    [QueryProperty(nameof(UserId), "userid")]
    [QueryProperty(nameof(UserEmail), "email")]
    [QueryProperty(nameof(UserName), "username")]
    public partial class ProfileSetupPage : ContentPage
    {
        private readonly FirebaseService _firebaseService;
        private List<string> _photoSources = new List<string>(6);
        private Image[] _photoImages;
        private VerticalStackLayout[] _photoPlaceholders;
        private Frame[] _photoSlots;
        private int _photoCount = 0;

        public string UserId { get; set; }
        public string UserEmail { get; set; }
        public string UserName { get; set; }

        public ProfileSetupPage()
        {
            InitializeComponent();
            _firebaseService = new FirebaseService();
            InitializePhotoArrays();
            SetupInitialData();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Entrance animation
            this.Opacity = 0;
            this.FadeTo(1, 600, Easing.CubicOut);
        }

        private void InitializePhotoArrays()
        {
            // Initialize photo tracking arrays
            for (int i = 0; i < 6; i++)
            {
                _photoSources.Add(null);
            }

            _photoImages = new Image[]
            {
                Photo1, Photo2, Photo3, Photo4, Photo5, Photo6
            };

            _photoPlaceholders = new VerticalStackLayout[]
            {
                PhotoPlaceholder1, PhotoPlaceholder2, PhotoPlaceholder3,
                PhotoPlaceholder4, PhotoPlaceholder5, PhotoPlaceholder6
            };

            _photoSlots = new Frame[]
            {
                PhotoSlot1, PhotoSlot2, PhotoSlot3,
                PhotoSlot4, PhotoSlot5, PhotoSlot6
            };
        }

        private void SetupInitialData()
        {
            // Pre-fill display name if we have user data
            if (!string.IsNullOrEmpty(UserName))
            {
                DisplayNameEntry.Text = UserName;
            }

            UpdateProgress();
        }

        private void OnDisplayNameChanged(object sender, TextChangedEventArgs e)
        {
            var displayName = e.NewTextValue?.Trim() ?? "";

            if (string.IsNullOrEmpty(displayName))
            {
                DisplayNameError.Text = "Display name is required";
                DisplayNameError.IsVisible = true;
            }
            else if (displayName.Length < 2)
            {
                DisplayNameError.Text = "Display name must be at least 2 characters";
                DisplayNameError.IsVisible = true;
            }
            else if (displayName.Length > 50)
            {
                DisplayNameError.Text = "Display name must be less than 50 characters";
                DisplayNameError.IsVisible = true;
            }
            else
            {
                DisplayNameError.IsVisible = false;
            }

            UpdateProgress();
        }

        private async void OnAddPhotoClicked(object sender, EventArgs e)
        {
            try
            {
                int photoIndex = 0;

                // Get the photo index from TapGestureRecognizer
                if (sender is Frame frame && e is TappedEventArgs tappedArgs)
                {
                    if (tappedArgs.Parameter is int index)
                    {
                        photoIndex = index;
                    }
                }
                // Fallback for Button CommandParameter
                else if (sender is Button button && button.CommandParameter is int buttonIndex)
                {
                    photoIndex = buttonIndex;
                }

                // Check if photo already exists - if yes, show option to remove
                if (!string.IsNullOrEmpty(_photoSources[photoIndex]))
                {
                    var result = await DisplayActionSheet("Photo Options", "Cancel", null, "Replace Photo", "Remove Photo");

                    if (result == "Remove Photo")
                    {
                        await RemovePhoto(photoIndex);
                        return;
                    }
                    else if (result != "Replace Photo")
                    {
                        return;
                    }
                }

                await AddPhotoAsync(photoIndex);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to add photo. Please try again.", "OK");
                System.Diagnostics.Debug.WriteLine($"Add photo error: {ex.Message}");
            }
        }

        private async Task AddPhotoAsync(int photoIndex)
        {
            try
            {
                // Show action sheet for photo source
                var result = await DisplayActionSheet(
                    "Select Photo Source",
                    "Cancel",
                    null,
                    "Camera",
                    "Photo Library");

                if (result == "Cancel" || string.IsNullOrEmpty(result))
                    return;

                FileResult photo = null;

                if (result == "Camera")
                {
                    // Check camera permission
                    var cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
                    if (cameraStatus != PermissionStatus.Granted)
                    {
                        cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
                    }

                    if (cameraStatus != PermissionStatus.Granted)
                    {
                        await DisplayAlert("Permission Denied", "Camera permission is required to take photos.", "OK");
                        return;
                    }

                    photo = await MediaPicker.Default.CapturePhotoAsync();
                }
                else if (result == "Photo Library")
                {
                    // Check photo library permission
                    var photoStatus = await Permissions.CheckStatusAsync<Permissions.Photos>();
                    if (photoStatus != PermissionStatus.Granted)
                    {
                        photoStatus = await Permissions.RequestAsync<Permissions.Photos>();
                    }

                    if (photoStatus != PermissionStatus.Granted)
                    {
                        await DisplayAlert("Permission Denied", "Photo library permission is required to select photos.", "OK");
                        return;
                    }

                    photo = await MediaPicker.Default.PickPhotoAsync();
                }

                if (photo != null)
                {
                    // Load and display the photo
                    var stream = await photo.OpenReadAsync();
                    _photoImages[photoIndex].Source = ImageSource.FromStream(() => stream);
                    _photoImages[photoIndex].IsVisible = true;
                    _photoPlaceholders[photoIndex].IsVisible = false;

                    // Store photo source
                    _photoSources[photoIndex] = photo.FullPath;

                    // Update photo count
                    _photoCount = 0;
                    foreach (var source in _photoSources)
                    {
                        if (!string.IsNullOrEmpty(source))
                            _photoCount++;
                    }

                    UpdatePhotoUI();
                    UpdateProgress();

                    // Show success feedback
                    try { HapticFeedback.Perform(HapticFeedbackType.Click); } catch { }
                }
            }
            catch (FeatureNotSupportedException)
            {
                await DisplayAlert("Not Supported", "This feature is not supported on this device.", "OK");
            }
            catch (PermissionException)
            {
                await DisplayAlert("Permission Error", "Permission was denied to access photos.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load photo: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"Photo error: {ex}");
            }
        }

        private async Task RemovePhoto(int photoIndex)
        {
            try
            {
                // Hide photo and show placeholder
                _photoImages[photoIndex].IsVisible = false;
                _photoPlaceholders[photoIndex].IsVisible = true;
                _photoSources[photoIndex] = null;

                // Update count
                _photoCount--;

                UpdatePhotoUI();
                UpdateProgress();

                // Add scale animation
                await _photoSlots[photoIndex].ScaleTo(0.9, 100);
                await _photoSlots[photoIndex].ScaleTo(1, 100);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Remove photo error: {ex.Message}");
            }
        }

        private void UpdatePhotoUI()
        {
            PhotoCountLabel.Text = $"({_photoCount}/6)";

            if (_photoCount >= 3)
            {
                PhotoError.IsVisible = false;
            }
            else
            {
                PhotoError.Text = $"Add {3 - _photoCount} more photos to continue";
                PhotoError.IsVisible = true;
            }
        }

        private void OnBioChanged(object sender, TextChangedEventArgs e)
        {
            var bio = e.NewTextValue ?? "";
            BioCharCountLabel.Text = $"({bio.Length}/500)";

            if (string.IsNullOrWhiteSpace(bio))
            {
                BioError.Text = "Please write something about yourself";
                BioError.IsVisible = true;
            }
            else if (bio.Length > 500)
            {
                BioError.Text = "Bio is too long";
                BioError.IsVisible = true;
            }
            else
            {
                BioError.IsVisible = false;
            }

            UpdateProgress();
        }

        private void OnAgeChanged(object sender, TextChangedEventArgs e)
        {
            var ageText = e.NewTextValue ?? "";

            if (string.IsNullOrEmpty(ageText))
            {
                AgeError.Text = "Age is required";
                AgeError.IsVisible = true;
            }
            else if (!int.TryParse(ageText, out int age))
            {
                AgeError.Text = "Please enter a valid number";
                AgeError.IsVisible = true;
            }
            else if (age < 18)
            {
                AgeError.Text = "You must be at least 18 years old";
                AgeError.IsVisible = true;
            }
            else if (age > 100)
            {
                AgeError.Text = "Please enter a valid age";
                AgeError.IsVisible = true;
            }
            else
            {
                AgeError.IsVisible = false;
            }

            UpdateProgress();
        }

        private void UpdateProgress()
        {
            double progress = 0;
            int completedItems = 0;
            int totalItems = 4; // Display name, photos, bio, age

            // Check display name
            if (!string.IsNullOrWhiteSpace(DisplayNameEntry.Text) &&
                DisplayNameEntry.Text.Length >= 2 &&
                !DisplayNameError.IsVisible)
            {
                completedItems++;
            }

            // Check photos (minimum 3)
            if (_photoCount >= 3)
            {
                completedItems++;
            }

            // Check bio
            if (!string.IsNullOrWhiteSpace(BioEditor.Text) && !BioError.IsVisible)
            {
                completedItems++;
            }

            // Check age
            if (!string.IsNullOrWhiteSpace(AgeEntry.Text) && !AgeError.IsVisible)
            {
                completedItems++;
            }

            progress = (double)completedItems / totalItems;
            ProfileProgressBar.Progress = progress;
            ProgressLabel.Text = $"{(int)(progress * 100)}% Complete";

            // Enable complete button if all requirements are met
            CompleteProfileButton.IsEnabled = completedItems == totalItems;
        }

        private async void OnCompleteProfileClicked(object sender, EventArgs e)
        {
            try
            {
                // Haptic feedback
                try { HapticFeedback.Perform(HapticFeedbackType.Click); } catch { }

                // Button animation
                await CompleteProfileButton.ScaleTo(0.95, 50);
                await CompleteProfileButton.ScaleTo(1, 50);

                // Show loading
                CompleteProfileButton.IsVisible = false;
                LoadingIndicator.IsVisible = true;

                // Save profile
                bool success = await SaveProfileAsync();

                if (success)
                {
                    await DisplayAlert("Profile Complete! 🎉",
                        "Your profile has been created successfully. Welcome to HeartSync!",
                        "Start Exploring");

                    // Navigate to main app
                    await Shell.Current.GoToAsync("//swipe");
                }
                else
                {
                    await DisplayAlert("Error",
                        "Failed to save your profile. Please try again.",
                        "OK");
                    CompleteProfileButton.IsVisible = true;
                    LoadingIndicator.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error",
                    "Something went wrong. Please try again.",
                    "OK");
                System.Diagnostics.Debug.WriteLine($"Complete profile error: {ex.Message}");
                CompleteProfileButton.IsVisible = true;
                LoadingIndicator.IsVisible = false;
            }
        }

        private async Task<bool> SaveProfileAsync()
        {
            try
            {
                // Validate required fields first
                if (string.IsNullOrWhiteSpace(DisplayNameEntry.Text))
                {
                    await DisplayAlert("Error", "Please enter your display name.", "OK");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(AgeEntry.Text) || !int.TryParse(AgeEntry.Text, out int age) || age < 18 || age > 100)
                {
                    await DisplayAlert("Error", "Please enter a valid age between 18 and 100.", "OK");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(BioEditor.Text))
                {
                    await DisplayAlert("Error", "Please write something about yourself.", "OK");
                    return false;
                }

                if (_photoCount < 3)
                {
                    await DisplayAlert("Error", "Please add at least 3 photos.", "OK");
                    return false;
                }

                // Get current user profile or create new one
                var userProfile = await _firebaseService.GetUserProfileAsync(UserId) ?? new UserProfile
                {
                    Id = UserId,
                    Email = UserEmail,
                    CreatedAt = DateTime.UtcNow
                };

                // Update profile data
                userProfile.Name = DisplayNameEntry.Text.Trim();
                userProfile.Bio = BioEditor.Text.Trim();
                userProfile.Age = age; // Use the parsed age
                userProfile.ProfileCompleted = true;
                userProfile.IsActive = true;
                userProfile.LastLogin = DateTime.UtcNow;

                // Upload photos to Firebase Storage and get URLs
                userProfile.Photos = new List<string>();

                for (int i = 0; i < _photoSources.Count; i++)
                {
                    if (!string.IsNullOrEmpty(_photoSources[i]))
                    {
                        try
                        {
                            // Show progress for photo upload
                            LoadingIndicator.IsVisible = true;

                            // Upload photo to Firebase Storage
                            string photoUrl = await UploadPhotoToFirebaseAsync(_photoSources[i], i);

                            if (!string.IsNullOrEmpty(photoUrl))
                            {
                                userProfile.Photos.Add(photoUrl);
                            }
                            else
                            {
                                // Fallback: save local path (not ideal, but better than losing data)
                                userProfile.Photos.Add(_photoSources[i]);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Photo upload error: {ex.Message}");
                            // Fallback: save local path
                            userProfile.Photos.Add(_photoSources[i]);
                        }
                    }
                }

                // Save to Firebase Realtime Database
                bool success = await _firebaseService.UpdateUserProfileAsync(userProfile);

                if (success)
                {
                    // Update local preferences
                    Preferences.Set("profile_completed", true);
                    Preferences.Set("user_name", userProfile.Name);
                    Preferences.Set("user_age", userProfile.Age);

                    // Save profile data locally for offline access
                    var profileJson = System.Text.Json.JsonSerializer.Serialize(userProfile);
                    await SecureStorage.SetAsync($"user_profile_{UserId}", profileJson);
                }

                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save profile error: {ex.Message}");
                await DisplayAlert("Error", $"Failed to save profile: {ex.Message}", "OK");
                return false;
            }
        }

        private async Task<string> UploadPhotoToFirebaseAsync(string localPath, int photoIndex)
        {
            try
            {
                if (string.IsNullOrEmpty(localPath) || !File.Exists(localPath))
                {
                    return null;
                }

                // Upload to Firebase Storage using the service
                string downloadUrl = await _firebaseService.UploadPhotoAsync(localPath, UserId, photoIndex);

                if (!string.IsNullOrEmpty(downloadUrl))
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Photo {photoIndex} uploaded successfully: {downloadUrl}");
                    return downloadUrl;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Failed to upload photo {photoIndex}");
                    return localPath; // Fallback to local path
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Photo upload error: {ex.Message}");
                return localPath; // Fallback to local path
            }
        }

        private async void OnSkipTapped(object sender, EventArgs e)
        {
            var result = await DisplayAlert("Skip Profile Setup?",
                "You can complete your profile later, but having photos and a bio will help you get more matches.",
                "Skip for Now", "Continue Setup");

            if (result)
            {
                // Mark profile as incomplete but allow access to app
                Preferences.Set("profile_completed", false);
                await Shell.Current.GoToAsync("//swipe");
            }
        }

        protected override bool OnBackButtonPressed()
        {
            // Prevent back navigation - user must complete or skip profile
            MainThread.BeginInvokeOnMainThread(() =>
            {
                OnSkipTapped(this, EventArgs.Empty);
            });

            return true;
        }
    }
}