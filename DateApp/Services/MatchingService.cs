using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DateApp.Services;

namespace DateApp.Services
{
    public class MatchingService
    {
        private readonly FirebaseService _firebaseService;

        public MatchingService(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        public async Task<List<UserProfile>> GetPotentialMatchesAsync(UserProfile currentUser)
        {
            try
            {
                if (currentUser == null)
                {
                    System.Diagnostics.Debug.WriteLine("Current user is null");
                    return new List<UserProfile>();
                }

                // Get all potential matches from Firebase
                var allMatches = await _firebaseService.GetPotentialMatchesAsync();

                if (allMatches == null || allMatches.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No potential matches found in database");
                    // Return demo data for development
                    return GetDemoMatches();
                }

                // Filter and sort matches based on preferences and location
                var filteredMatches = FilterMatchesByPreferences(allMatches, currentUser);
                var sortedMatches = SortMatchesByDistance(filteredMatches, currentUser);

                System.Diagnostics.Debug.WriteLine($"Found {sortedMatches.Count} potential matches");
                return sortedMatches;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting potential matches: {ex.Message}");
                // Return demo data as fallback
                return GetDemoMatches();
            }
        }

        private List<UserProfile> FilterMatchesByPreferences(List<UserProfile> allMatches, UserProfile currentUser)
        {
            var filtered = allMatches.Where(match =>
            {
                // Don't show current user
                if (match.Id == currentUser.Id)
                    return false;

                // Only show active users with completed profiles
                if (!match.IsActive || !match.ProfileCompleted)
                    return false;

                // Filter by age preferences
                if (currentUser.Preferences != null)
                {
                    if (match.Age < currentUser.Preferences.AgeMin || match.Age > currentUser.Preferences.AgeMax)
                        return false;
                }

                // Must have at least one photo
                if (match.Photos == null || match.Photos.Count == 0)
                    return false;

                return true;
            }).ToList();

            return filtered;
        }

        private List<UserProfile> SortMatchesByDistance(List<UserProfile> matches, UserProfile currentUser)
        {
            // Sort by distance (closest first)
            // Pentru demo, folosim o sortare random
            var random = new Random();
            return matches.OrderBy(x => random.Next()).ToList();
        }

        private double CalculateDistance(UserProfile user1, UserProfile user2)
        {
            // Placeholder pentru calculul real al distanței
            // În aplicația reală ai folosi formula Haversine pentru coordonate GPS

            if (user1.Location == null || user2.Location == null)
                return double.MaxValue;

            // Formula simplificată pentru demo
            var lat1 = user1.Location.Latitude;
            var lon1 = user1.Location.Longitude;
            var lat2 = user2.Location.Latitude;
            var lon2 = user2.Location.Longitude;

            // Distanța euclidiană simplificată (nu e accurată pentru Earth)
            var distance = Math.Sqrt(Math.Pow(lat2 - lat1, 2) + Math.Pow(lon2 - lon1, 2)) * 111; // aprox km per degree

            return distance;
        }

        private List<UserProfile> GetDemoMatches()
        {
            // Date demo pentru dezvoltare
            var demoUsers = new List<UserProfile>
            {
                new UserProfile
                {
                    Id = "demo_user_1",
                    Name = "Emma",
                    Age = 25,
                    Bio = "Love hiking, photography, and good coffee ☕ Looking for someone to explore the city with!",
                    Photos = new List<string>
                    {
                        "https://images.unsplash.com/photo-1494790108755-2616b612b47c?w=400&h=600&fit=crop&crop=face",
                        "https://images.unsplash.com/photo-1517841905240-472988babdf9?w=400&h=600&fit=crop&crop=face",
                        "https://images.unsplash.com/photo-1524504388940-b1c1722653e1?w=400&h=600&fit=crop&crop=face"
                    },
                    IsActive = true,
                    ProfileCompleted = true,
                    Location = new Location { City = "Bucharest", Country = "Romania" }
                },
                new UserProfile
                {
                    Id = "demo_user_2",
                    Name = "Sofia",
                    Age = 23,
                    Bio = "Yoga instructor 🧘‍♀️ Vegan foodie 🌱 Beach lover 🏖️ Let's find our zen together!",
                    Photos = new List<string>
                    {
                        "https://images.unsplash.com/photo-1531746020798-e6953c6e8e04?w=400&h=600&fit=crop&crop=face",
                        "https://images.unsplash.com/photo-1488426862026-3ee34a7d66df?w=400&h=600&fit=crop&crop=face",
                        "https://images.unsplash.com/photo-1529626455594-4ff0802cfb7e?w=400&h=600&fit=crop&crop=face"
                    },
                    IsActive = true,
                    ProfileCompleted = true,
                    Location = new Location { City = "Bucharest", Country = "Romania" }
                },
                new UserProfile
                {
                    Id = "demo_user_3",
                    Name = "Alexandra",
                    Age = 27,
                    Bio = "Artist and book lover 📚🎨 Weekend warrior seeking adventure and deep conversations.",
                    Photos = new List<string>
                    {
                        "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=400&h=600&fit=crop&crop=face",
                        "https://images.unsplash.com/photo-1504593811423-6dd665756598?w=400&h=600&fit=crop&crop=face"
                    },
                    IsActive = true,
                    ProfileCompleted = true,
                    Location = new Location { City = "Bucharest", Country = "Romania" }
                },
                new UserProfile
                {
                    Id = "demo_user_4",
                    Name = "Maria",
                    Age = 24,
                    Bio = "Medical student 👩‍⚕️ Dog lover 🐕 Netflix binger 📺 Looking for my study buddy and adventure partner!",
                    Photos = new List<string>
                    {
                        "https://images.unsplash.com/photo-1508214751196-bcfd4ca60f91?w=400&h=600&fit=crop&crop=face",
                        "https://images.unsplash.com/photo-1521577352947-9bb58764b69a?w=400&h=600&fit=crop&crop=face",
                        "https://images.unsplash.com/photo-1521038199265-bc482db0f923?w=400&h=600&fit=crop&crop=face"
                    },
                    IsActive = true,
                    ProfileCompleted = true,
                    Location = new Location { City = "Bucharest", Country = "Romania" }
                },
                new UserProfile
                {
                    Id = "demo_user_5",
                    Name = "Ana",
                    Age = 26,
                    Bio = "Travel enthusiast ✈️ Foodie 🍝 Sunset chaser 🌅 Let's explore the world together!",
                    Photos = new List<string>
                    {
                        "https://images.unsplash.com/photo-1506277886164-e25aa3f4ef7f?w=400&h=600&fit=crop&crop=face",
                        "https://images.unsplash.com/photo-1502767089025-6572583495b9?w=400&h=600&fit=crop&crop=face"
                    },
                    IsActive = true,
                    ProfileCompleted = true,
                    Location = new Location { City = "Bucharest", Country = "Romania" }
                },
                new UserProfile
                {
                    Id = "demo_user_6",
                    Name = "Cristina",
                    Age = 28,
                    Bio = "Fitness trainer 💪 Nature lover 🌲 Morning person ☀️ Let's motivate each other to be our best selves!",
                    Photos = new List<string>
                    {
                        "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=400&h=600&fit=crop&crop=face",
                        "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=400&h=600&fit=crop&crop=face",
                        "https://images.unsplash.com/photo-1506919258185-6078bba55d2a?w=400&h=600&fit=crop&crop=face",
                        "https://images.unsplash.com/photo-1519085360753-af0119f7cbe7?w=400&h=600&fit=crop&crop=face"
                    },
                    IsActive = true,
                    ProfileCompleted = true,
                    Location = new Location { City = "Bucharest", Country = "Romania" }
                }
            };

            // Shuffle pentru randomizare
            var random = new Random();
            return demoUsers.OrderBy(x => random.Next()).ToList();
        }

        public async Task<bool> HasUserAlreadySwiped(string currentUserId, string targetUserId)
        {
            try
            {
                // Check if current user has already swiped on target user
                // Implementare simplificată - în aplicația reală ai verifica în baza de date
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking swipe history: {ex.Message}");
                return false;
            }
        }

        public async Task<List<UserProfile>> GetNearbyUsersAsync(UserProfile currentUser, double radiusKm = 50)
        {
            try
            {
                var allUsers = await _firebaseService.GetPotentialMatchesAsync();

                if (currentUser.Location == null)
                    return allUsers ?? new List<UserProfile>();

                var nearbyUsers = allUsers?.Where(user =>
                {
                    if (user.Location == null) return false;

                    var distance = CalculateDistance(currentUser, user);
                    return distance <= radiusKm;
                }).ToList();

                return nearbyUsers ?? new List<UserProfile>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting nearby users: {ex.Message}");
                return new List<UserProfile>();
            }
        }
    }
}