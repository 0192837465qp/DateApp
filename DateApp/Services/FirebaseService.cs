using Firebase.Auth;
using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.Maui.Storage;

namespace DateApp.Services
{
    public class FirebaseService
    {
        // IMPORTANT: Înlocuiește cu cheile tale din Firebase Console
        private const string FirebaseApiKey = "AIzaSyDgRtqV-xuRLtbr0PE6zeUz7RXnqHHbQno";

        // URL-ul corect pentru Firebase Realtime Database (regiunea Europe West1)
        private const string FirebaseDatabaseUrl = "https://db01-4b063-default-rtdb.europe-west1.firebasedatabase.app/";

        private readonly FirebaseAuthProvider _authProvider;
        private readonly FirebaseClient _firebaseClient;
        private FirebaseAuthLink _currentAuth;

        public FirebaseService()
        {
            _authProvider = new FirebaseAuthProvider(new FirebaseConfig(FirebaseApiKey));
            _firebaseClient = new FirebaseClient(FirebaseDatabaseUrl);
        }

        // Get current user
        public User CurrentUser => _currentAuth?.User;
        public string CurrentUserId => _currentAuth?.User?.LocalId;
        public bool IsAuthenticated => _currentAuth != null && !string.IsNullOrEmpty(_currentAuth.FirebaseToken);

        // Register new user
        public async Task<(bool success, string message, string userId)> RegisterUserAsync(string email, string password, string fullName)
        {
            try
            {
                // Create auth account
                var auth = await _authProvider.CreateUserWithEmailAndPasswordAsync(email, password);
                _currentAuth = auth;

                // Create user profile in database
                var userProfile = new UserProfile
                {
                    Id = auth.User.LocalId,
                    Email = email,
                    Name = fullName,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    ProfileCompleted = false
                };

                await _firebaseClient
                    .Child("users")
                    .Child(auth.User.LocalId)
                    .PutAsync(userProfile);

                return (true, "Registration successful!", auth.User.LocalId);
            }
            catch (FirebaseAuthException ex)
            {
                return ex.Reason switch
                {
                    AuthErrorReason.EmailExists => (false, "This email is already registered. Please sign in.", null),
                    AuthErrorReason.WeakPassword => (false, "Password should be at least 6 characters.", null),
                    AuthErrorReason.InvalidEmailAddress => (false, "Please enter a valid email address.", null),
                    _ => (false, "Registration failed. Please try again.", null)
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Registration error: {ex.Message}");
                return (false, $"Error: {ex.Message}", null);
            }
        }

        // Login user
        // Login user - Updated to return userId
        public async Task<(bool success, string message, string userId)> LoginAsync(string email, string password)
        {
            try
            {
                var auth = await _authProvider.SignInWithEmailAndPasswordAsync(email, password);
                _currentAuth = auth;

                // Update last login
                await _firebaseClient
                    .Child("users")
                    .Child(auth.User.LocalId)
                    .Child("lastLogin")
                    .PutAsync(DateTime.UtcNow);

                return (true, "Login successful!", auth.User.LocalId);
            }
            catch (FirebaseAuthException ex)
            {
                var message = ex.Reason switch
                {
                    AuthErrorReason.WrongPassword => "Incorrect password. Please try again.",
                    AuthErrorReason.UnknownEmailAddress => "No account found with this email.",
                    AuthErrorReason.UserDisabled => "This account has been disabled.",
                    _ => "Login failed. Please check your credentials."
                };
                return (false, message, null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
                return (false, $"Connection error: {ex.Message}", null);
            }
        }

        // Logout
        public void Logout()
        {
            _currentAuth = null;
        }

        // Get user profile
        public async Task<UserProfile> GetUserProfileAsync(string userId)
        {
            try
            {
                var profile = await _firebaseClient
                    .Child("users")
                    .Child(userId)
                    .OnceSingleAsync<UserProfile>();

                return profile;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get profile error: {ex.Message}");
                return null;
            }
        }

        // Update user profile
        public async Task<bool> UpdateUserProfileAsync(UserProfile profile)
        {
            try
            {
                await _firebaseClient
                    .Child("users")
                    .Child(profile.Id)
                    .PutAsync(profile);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Update profile error: {ex.Message}");
                return false;
            }
        }

        // Get potential matches (simple version)
        public async Task<List<UserProfile>> GetPotentialMatchesAsync()
        {
            try
            {
                var allUsers = await _firebaseClient
                    .Child("users")
                    .OnceAsync<UserProfile>();

                // Filter out current user and already matched users
                var potentialMatches = allUsers
                    .Where(u => u.Object.Id != CurrentUserId && u.Object.IsActive)
                    .Select(u => u.Object)
                    .Take(10) // Limit to 10 for now
                    .ToList();

                return potentialMatches;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get matches error: {ex.Message}");
                return new List<UserProfile>();
            }
        }

        // Create a match
        public async Task<bool> CreateMatchAsync(string otherUserId, bool isLike)
        {
            try
            {
                var match = new Match
                {
                    User1Id = CurrentUserId,
                    User2Id = otherUserId,
                    User1Liked = isLike,
                    Timestamp = DateTime.UtcNow
                };

                await _firebaseClient
                    .Child("matches")
                    .PostAsync(match);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Create match error: {ex.Message}");
                return false;
            }
        }

        // Check if users matched
        public async Task<bool> CheckMutualMatchAsync(string otherUserId)
        {
            try
            {
                var matches = await _firebaseClient
                    .Child("matches")
                    .OnceAsync<Match>();

                // Check if both users liked each other
                var user1Liked = matches.Any(m =>
                    m.Object.User1Id == CurrentUserId &&
                    m.Object.User2Id == otherUserId &&
                    m.Object.User1Liked);

                var user2Liked = matches.Any(m =>
                    m.Object.User1Id == otherUserId &&
                    m.Object.User2Id == CurrentUserId &&
                    m.Object.User1Liked);

                return user1Liked && user2Liked;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Check match error: {ex.Message}");
                return false;
            }
        }

        // Send password reset email
        public async Task<(bool success, string message)> ResetPasswordAsync(string email)
        {
            try
            {
                await _authProvider.SendPasswordResetEmailAsync(email);
                return (true, "Password reset email sent. Please check your inbox.");
            }
            catch (FirebaseAuthException ex)
            {
                return ex.Reason switch
                {
                    AuthErrorReason.UnknownEmailAddress => (false, "No account found with this email."),
                    _ => (false, "Failed to send reset email. Please try again.")
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Reset password error: {ex.Message}");
                return (false, "Connection error. Please check your internet.");
            }
        }
    }

    // Data Models
    public class UserProfile
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string Bio { get; set; }
        public List<string> Photos { get; set; } = new List<string>();
        public Location Location { get; set; }
        public UserPreferences Preferences { get; set; } // Renamed from Preferences to UserPreferences
        public DateTime CreatedAt { get; set; }
        public DateTime LastLogin { get; set; }
        public bool IsActive { get; set; }
        public bool ProfileCompleted { get; set; }
    }

    public class Location
    {
        public string City { get; set; }
        public string Country { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class UserPreferences // Renamed from Preferences to UserPreferences
    {
        public int AgeMin { get; set; } = 18;
        public int AgeMax { get; set; } = 50;
        public int MaxDistance { get; set; } = 50;
        public string InterestedIn { get; set; } = "Everyone";
    }

    public class Match
    {
        public string User1Id { get; set; }
        public string User2Id { get; set; }
        public bool User1Liked { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsMutual { get; set; }
    }
}