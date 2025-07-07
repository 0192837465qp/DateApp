using DateApp.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DateApp.Views
{
    public partial class SwipePage : ContentPage
    {
        private readonly FirebaseService _firebaseService;
        private readonly MatchingService _matchingService;
        private List<UserProfile> _potentialMatches;
        private UserProfile _currentUser;
        private UserProfile _frontCardUser;
        private UserProfile _backCardUser;
        private int _currentPhotoIndex = 0;
        private bool _isAnimating = false;
        private const double SwipeThreshold = 80;

        // UI References that will be created dynamically
        private Image _mainPhoto;
        private Label _userNameLabel;
        private Label _distanceLabel;
        private Label _userBioLabel;
        private List<VisualElement> _indicators;
        private Grid _userInfoContainer;

        public SwipePage()
        {
            InitializeComponent();
            _firebaseService = new FirebaseService();
            _matchingService = new MatchingService(_firebaseService);
            _indicators = new List<VisualElement>();
            LoadCurrentUser();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Add entrance animations for floating orbs
            AnimateFloatingOrbs();

            await LoadPotentialMatches();
        }

        private async void LoadCurrentUser()
        {
            try
            {
                var userId = Preferences.Get("user_id", "");
                if (!string.IsNullOrEmpty(userId))
                {
                    _currentUser = await _firebaseService.GetUserProfileAsync(userId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load current user error: {ex.Message}");
            }
        }

        private async Task LoadPotentialMatches()
        {
            try
            {
                LoadingFrame.IsVisible = true;
                CardsContainer.IsVisible = false;
                NoMoreCardsFrame.IsVisible = false;

                // Get potential matches based on location and preferences
                _potentialMatches = await _matchingService.GetPotentialMatchesAsync(_currentUser);

                if (_potentialMatches?.Count > 0)
                {
                    await SetupCards();
                    LoadingFrame.IsVisible = false;
                    CardsContainer.IsVisible = true;
                }
                else
                {
                    LoadingFrame.IsVisible = false;
                    NoMoreCardsFrame.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load matches error: {ex.Message}");
                LoadingFrame.IsVisible = false;
                NoMoreCardsFrame.IsVisible = true;
            }
        }

        private async Task SetupCards()
        {
            if (_potentialMatches == null || _potentialMatches.Count == 0)
            {
                NoMoreCardsFrame.IsVisible = true;
                CardsContainer.IsVisible = false;
                return;
            }

            // Setup front card
            _frontCardUser = _potentialMatches[0];
            SetupFrontCard(_frontCardUser);

            // Setup back card if available
            if (_potentialMatches.Count > 1)
            {
                _backCardUser = _potentialMatches[1];
                SetupBackCard(_backCardUser);
                BackCard.IsVisible = true;
            }
            else
            {
                BackCard.IsVisible = false;
            }

            // Reset card positions
            await FrontCard.TranslateTo(0, 0, 0);
            await FrontCard.RotateTo(0, 0);
            FrontCard.Scale = 1;
            FrontCard.Opacity = 1;
        }

        private void SetupFrontCard(UserProfile user)
        {
            _currentPhotoIndex = 0;

            // Clear existing content
            FrontCardContent.Children.Clear();
            FrontCardContent.RowDefinitions.Clear();

            // Setup row definitions
            FrontCardContent.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            FrontCardContent.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Create photo area with premium styling
            var photoGrid = new Grid();

            // Main photo
            _mainPhoto = new Image
            {
                Aspect = Aspect.AspectFill,
                BackgroundColor = Color.FromRgb(243, 244, 246)
            };

            if (user.Photos?.Count > 0)
            {
                _mainPhoto.Source = user.Photos[0];
            }

            photoGrid.Children.Add(_mainPhoto);

            // Create premium photo indicators
            CreatePremiumPhotoIndicators(photoGrid, user.Photos?.Count ?? 1);

            // Create tap areas for photo navigation
            CreatePhotoTapAreas(photoGrid);

            // Premium gradient overlay with glassmorphism
            var gradientOverlay = new Grid
            {
                VerticalOptions = LayoutOptions.End,
                HeightRequest = 160
            };

            var gradientBorder = new Border
            {
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop { Color = Colors.Transparent, Offset = 0.0f },
                        new GradientStop { Color = Color.FromArgb("#60000000"), Offset = 0.4f },
                        new GradientStop { Color = Color.FromArgb("#90000000"), Offset = 1.0f }
                    }
                }
            };

            gradientOverlay.Children.Add(gradientBorder);
            photoGrid.Children.Add(gradientOverlay);

            // Create premium user info section
            CreatePremiumUserInfo(user);

            // Add to front card content
            FrontCardContent.Children.Add(photoGrid);
            FrontCardContent.Children.Add(_userInfoContainer);
            Grid.SetRow(_userInfoContainer, 1);

            UpdatePhotoIndicators();
        }

        private void CreatePremiumUserInfo(UserProfile user)
        {
            _userInfoContainer = new Grid
            {
                Padding = new Thickness(25, 25, 25, 30),
                RowDefinitions = new RowDefinitionCollection
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto }
                }
            };

            // Name and age with premium styling
            var nameContainer = new Border
            {
                BackgroundColor = Color.FromArgb("#15000000"),
                Padding = new Thickness(20, 12),
                Margin = new Thickness(0, 0, 0, 12)
            };
            nameContainer.StrokeShape = new RoundRectangle { CornerRadius = 20 };

            var nameStack = new HorizontalStackLayout { Spacing = 12 };

            _userNameLabel = new Label
            {
                Text = $"{user.Name}, {user.Age}",
                FontSize = 26,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromRgb(26, 26, 26),
                VerticalOptions = LayoutOptions.Center
            };

            // Premium location badge
            var locationBadge = new Border
            {
                BackgroundColor = Color.FromArgb("#FF4458"),
                Padding = new Thickness(12, 6)
            };
            locationBadge.StrokeShape = new RoundRectangle { CornerRadius = 15 };

            var locationStack = new HorizontalStackLayout { Spacing = 6 };

            var locationIcon = new Label
            {
                Text = "📍",
                FontSize = 14,
                VerticalOptions = LayoutOptions.Center
            };

            _distanceLabel = new Label
            {
                Text = CalculateDistance(user),
                FontSize = 12,
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center
            };

            locationStack.Children.Add(locationIcon);
            locationStack.Children.Add(_distanceLabel);
            locationBadge.Content = locationStack;

            nameStack.Children.Add(_userNameLabel);
            nameStack.Children.Add(locationBadge);
            nameContainer.Content = nameStack;

            // Bio with premium styling
            var bioBorder = new Border
            {
                BackgroundColor = Color.FromArgb("#08000000"),
                Padding = new Thickness(20, 16),
                Margin = new Thickness(0, 0, 0, 15)
            };
            bioBorder.StrokeShape = new RoundRectangle { CornerRadius = 18 };

            _userBioLabel = new Label
            {
                Text = user.Bio ?? "Living life to the fullest ✨",
                FontSize = 16,
                TextColor = Color.FromRgb(60, 60, 60),
                LineBreakMode = LineBreakMode.WordWrap,
                MaxLines = 4,
                LineHeight = 1.4
            };

            bioBorder.Content = _userBioLabel;

            // Interest tags (demo)
            var tagsContainer = CreateInterestTags();

            _userInfoContainer.Children.Add(nameContainer);
            _userInfoContainer.Children.Add(bioBorder);
            _userInfoContainer.Children.Add(tagsContainer);

            Grid.SetRow(bioBorder, 1);
            Grid.SetRow(tagsContainer, 2);
        }

        private HorizontalStackLayout CreateInterestTags()
        {
            var tagsStack = new HorizontalStackLayout
            {
                Spacing = 8,
                HorizontalOptions = LayoutOptions.Start
            };

            var interests = new[] { "🎵 Music", "✈️ Travel", "📚 Books", "🍕 Foodie" };
            var tagColors = new[] { "#FF6B6B", "#4ECDC4", "#45B7D1", "#FFA07A" };

            for (int i = 0; i < Math.Min(interests.Length, 3); i++)
            {
                var tagBorder = new Border
                {
                    BackgroundColor = Color.FromArgb($"20{tagColors[i].Substring(1)}"),
                    Padding = new Thickness(12, 6)
                };
                tagBorder.StrokeShape = new RoundRectangle { CornerRadius = 12 };

                var tagLabel = new Label
                {
                    Text = interests[i],
                    FontSize = 12,
                    TextColor = Color.FromArgb(tagColors[i]),
                    FontAttributes = FontAttributes.Bold
                };

                tagBorder.Content = tagLabel;
                tagsStack.Children.Add(tagBorder);
            }

            return tagsStack;
        }

        private void CreatePremiumPhotoIndicators(Grid photoGrid, int photoCount)
        {
            var indicatorsContainer = new Grid
            {
                VerticalOptions = LayoutOptions.Start,
                HorizontalOptions = LayoutOptions.Fill,
                Margin = new Thickness(20, 20, 20, 0),
                HeightRequest = 6
            };

            // Create glassmorphism background for indicators
            var indicatorsBg = new Border
            {
                BackgroundColor = Color.FromArgb("#20FFFFFF"),
                HeightRequest = 6
            };
            indicatorsBg.StrokeShape = new RoundRectangle { CornerRadius = 3 };
            indicatorsContainer.Children.Add(indicatorsBg);

            // Create individual indicators with premium styling
            var indicatorsGrid = new Grid
            {
                HeightRequest = 6,
                ColumnSpacing = 4
            };

            var columnDefs = new ColumnDefinitionCollection();
            for (int i = 0; i < Math.Min(6, photoCount); i++)
            {
                if (i > 0) columnDefs.Add(new ColumnDefinition { Width = new GridLength(4, GridUnitType.Absolute) });
                columnDefs.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            indicatorsGrid.ColumnDefinitions = columnDefs;

            _indicators.Clear();
            for (int i = 0; i < Math.Min(6, photoCount); i++)
            {
                var indicator = new Border
                {
                    HeightRequest = 6,
                    BackgroundColor = Color.FromArgb("#40FFFFFF")
                };
                indicator.StrokeShape = new RoundRectangle { CornerRadius = 3 };

                _indicators.Add(indicator);
                indicatorsGrid.Children.Add(indicator);
                Grid.SetColumn(indicator, i * 2);
            }

            indicatorsContainer.Children.Add(indicatorsGrid);
            photoGrid.Children.Add(indicatorsContainer);
        }

        private void CreatePhotoTapAreas(Grid photoGrid)
        {
            var tapGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                }
            };

            var leftTap = new Grid { BackgroundColor = Colors.Transparent };
            leftTap.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() => NavigatePhoto(-1))
            });

            var rightTap = new Grid { BackgroundColor = Colors.Transparent };
            rightTap.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() => NavigatePhoto(1))
            });

            tapGrid.Children.Add(leftTap);
            tapGrid.Children.Add(rightTap);
            Grid.SetColumn(rightTap, 1);

            photoGrid.Children.Add(tapGrid);
        }

        private void SetupBackCard(UserProfile user)
        {
            // Premium back card setup
            var backContent = new Grid();

            // Add a subtle preview image
            if (user.Photos?.Count > 0)
            {
                var previewImage = new Image
                {
                    Source = user.Photos[0],
                    Aspect = Aspect.AspectFill,
                    Opacity = 0.3
                };
                backContent.Children.Add(previewImage);
            }

            // Add glassmorphism overlay
            var overlay = new Border
            {
                BackgroundColor = Color.FromArgb("#60FFFFFF"),
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center,
                Padding = new Thickness(20, 12)
            };
            overlay.StrokeShape = new RoundRectangle { CornerRadius = 20 };

            var nameLabel = new Label
            {
                Text = $"{user.Name}, {user.Age}",
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromRgb(80, 80, 80),
                HorizontalTextAlignment = TextAlignment.Center
            };

            overlay.Content = nameLabel;
            backContent.Children.Add(overlay);
            BackCard.Content = backContent;
        }

        private void NavigatePhoto(int direction)
        {
            if (_frontCardUser?.Photos == null || _frontCardUser.Photos.Count <= 1) return;

            _currentPhotoIndex += direction;

            if (_currentPhotoIndex < 0)
                _currentPhotoIndex = _frontCardUser.Photos.Count - 1;
            else if (_currentPhotoIndex >= _frontCardUser.Photos.Count)
                _currentPhotoIndex = 0;

            if (_mainPhoto != null)
            {
                _mainPhoto.Source = _frontCardUser.Photos[_currentPhotoIndex];
            }

            UpdatePhotoIndicators();
        }

        private void UpdatePhotoIndicators()
        {
            if (_indicators == null || _frontCardUser?.Photos == null) return;

            for (int i = 0; i < _indicators.Count; i++)
            {
                if (i < _frontCardUser.Photos.Count)
                {
                    var indicator = _indicators[i];
                    if (indicator != null)
                    {
                        if (indicator is Border border)
                        {
                            border.BackgroundColor = i == _currentPhotoIndex ? Colors.White : Color.FromArgb("#40FFFFFF");

                            // Add subtle animation effect for Border
                            if (i == _currentPhotoIndex)
                            {
                                border.Shadow = new Shadow
                                {
                                    Brush = Colors.White,
                                    Offset = new Point(0, 0),
                                    Radius = 8,
                                    Opacity = 0.8f
                                };
                            }
                            else
                            {
                                border.Shadow = null;
                            }

                            border.IsVisible = true;
                        }
                    }
                }
                else
                {
                    _indicators[i].IsVisible = false;
                }
            }
        }

        private string CalculateDistance(UserProfile user)
        {
            // Placeholder - în aplicația reală ai calcula distanța reală
            var random = new Random();
            var distance = random.Next(1, 50);
            return $"{distance} km away";
        }

        private async void OnCardPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            if (_isAnimating || _frontCardUser == null) return;

            switch (e.StatusType)
            {
                case GestureStatus.Running:
                    await HandlePanRunning(e);
                    break;
                case GestureStatus.Completed:
                    await HandlePanCompleted(e);
                    break;
            }
        }

        private async Task HandlePanRunning(PanUpdatedEventArgs e)
        {
            var translation = e.TotalX;
            var rotation = translation * 0.1;
            var opacity = 1 - Math.Abs(translation) / 300;

            await FrontCard.TranslateTo(translation, e.TotalY * 0.1, 1, Easing.Linear);
            await FrontCard.RotateTo(rotation, 1, Easing.Linear);
            FrontCard.Opacity = Math.Max(0.3, opacity);

            // Show feedback overlays
            if (Math.Abs(translation) > 30)
            {
                if (translation > 0)
                {
                    LikeOverlay.IsVisible = true;
                    NopeOverlay.IsVisible = false;
                    SuperLikeOverlay.IsVisible = false;
                }
                else
                {
                    LikeOverlay.IsVisible = false;
                    NopeOverlay.IsVisible = true;
                    SuperLikeOverlay.IsVisible = false;
                }
            }
            else
            {
                LikeOverlay.IsVisible = false;
                NopeOverlay.IsVisible = false;
                SuperLikeOverlay.IsVisible = false;
            }

            // Super like on swipe up
            if (e.TotalY < -100)
            {
                LikeOverlay.IsVisible = false;
                NopeOverlay.IsVisible = false;
                SuperLikeOverlay.IsVisible = true;
            }
        }

        private async Task HandlePanCompleted(PanUpdatedEventArgs e)
        {
            var translation = e.TotalX;

            // Hide all overlays
            LikeOverlay.IsVisible = false;
            NopeOverlay.IsVisible = false;
            SuperLikeOverlay.IsVisible = false;

            if (Math.Abs(translation) > SwipeThreshold)
            {
                // Determine swipe direction
                if (translation > 0)
                {
                    await PerformSwipe(SwipeDirection.Right);
                }
                else
                {
                    await PerformSwipe(SwipeDirection.Left);
                }
            }
            else if (e.TotalY < -100)
            {
                await PerformSwipe(SwipeDirection.Up);
            }
            else
            {
                // Snap back to center
                await AnimateCardBack();
            }
        }

        private async Task AnimateCardBack()
        {
            var animationTasks = new List<Task>
            {
                FrontCard.TranslateTo(0, 0, 300, Easing.SpringOut),
                FrontCard.RotateTo(0, 300, Easing.SpringOut),
                FrontCard.ScaleTo(1, 300, Easing.SpringOut)
            };

            await Task.WhenAll(animationTasks);
            FrontCard.Opacity = 1;
        }

        private async Task PerformSwipe(SwipeDirection direction)
        {
            if (_isAnimating) return;
            _isAnimating = true;

            try
            {
                // Animate card out
                await AnimateCardOut(direction);

                // Record the swipe action
                await RecordSwipeAction(direction);

                // Move to next card
                await LoadNextCard();
            }
            finally
            {
                _isAnimating = false;
            }
        }

        private async Task AnimateCardOut(SwipeDirection direction)
        {
            var screenWidth = 400; // You can get actual screen width
            var targetX = direction switch
            {
                SwipeDirection.Right => screenWidth + 100,
                SwipeDirection.Left => -(screenWidth + 100),
                SwipeDirection.Up => 0,
                _ => 0
            };

            var targetY = direction == SwipeDirection.Up ? -1000 : 0;
            var rotation = direction switch
            {
                SwipeDirection.Right => 45,
                SwipeDirection.Left => -45,
                SwipeDirection.Up => 0,
                _ => 0
            };

            var scale = direction == SwipeDirection.Up ? 1.2 : 0.8;

            var animationTasks = new List<Task>
            {
                FrontCard.TranslateTo(targetX, targetY, 400, Easing.CubicIn),
                FrontCard.RotateTo(rotation, 400, Easing.CubicIn),
                FrontCard.ScaleTo(scale, 400, Easing.CubicIn),
                FrontCard.FadeTo(0, 300, Easing.CubicIn)
            };

            await Task.WhenAll(animationTasks);
        }

        private async Task RecordSwipeAction(SwipeDirection direction)
        {
            try
            {
                bool isLike = direction == SwipeDirection.Right || direction == SwipeDirection.Up;

                // Premium haptic feedback
                await PerformPremiumHapticFeedback(direction);

                // Record in Firebase
                await _firebaseService.CreateMatchAsync(_frontCardUser.Id, isLike);

                // Check for mutual match
                if (isLike)
                {
                    bool isMutualMatch = await _firebaseService.CheckMutualMatchAsync(_frontCardUser.Id);

                    if (isMutualMatch)
                    {
                        await ShowMatchDialog();
                    }
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Record swipe error: {ex.Message}");
            }
        }

        private async Task PerformPremiumHapticFeedback(SwipeDirection direction)
        {
            try
            {
                switch (direction)
                {
                    case SwipeDirection.Right: // Like
                        HapticFeedback.Perform(HapticFeedbackType.Click);
                        await Task.Delay(50);
                        HapticFeedback.Perform(HapticFeedbackType.Click);
                        break;
                    case SwipeDirection.Up: // Super Like
                        HapticFeedback.Perform(HapticFeedbackType.LongPress);
                        break;
                    case SwipeDirection.Left: // Nope
                        HapticFeedback.Perform(HapticFeedbackType.LongPress);
                        break;
                }
            }
            catch { }
        }

        private async Task ShowMatchDialog()
        {
            // Premium match dialog with animations
            try
            {
                HapticFeedback.Perform(HapticFeedbackType.LongPress);
            }
            catch { }

            await DisplayAlert("🎉 IT'S A MATCH! 🎉",
                $"You and {_frontCardUser.Name} have liked each other!\n\n💫 The stars have aligned! Start your conversation now! 💫",
                "💬 Start Chatting");
        }

        private async Task LoadNextCard()
        {
            // Remove current user from potential matches
            if (_potentialMatches?.Count > 0)
            {
                _potentialMatches.RemoveAt(0);
            }

            // Reset card positions and opacity
            await FrontCard.TranslateTo(0, 0, 0);
            await FrontCard.RotateTo(0, 0);
            FrontCard.Opacity = 1;
            FrontCard.Scale = 1;

            // Move to next card
            if (_potentialMatches?.Count > 0)
            {
                _frontCardUser = _backCardUser ?? _potentialMatches[0];

                // Setup front card with new user
                SetupFrontCard(_frontCardUser);

                // Setup new back card
                if (_potentialMatches.Count > 1)
                {
                    _backCardUser = _potentialMatches[1];
                    SetupBackCard(_backCardUser);
                    BackCard.IsVisible = true;

                    // Reset back card appearance
                    BackCard.Scale = 0.95;
                    BackCard.Opacity = 0.7;
                    BackCard.TranslationY = 8;
                }
                else
                {
                    BackCard.IsVisible = false;
                    _backCardUser = null;
                }

                _currentPhotoIndex = 0;
                UpdatePhotoIndicators();
            }
            else
            {
                // No more cards
                CardsContainer.IsVisible = false;
                NoMoreCardsFrame.IsVisible = true;
            }

            // Load more matches if running low
            if (_potentialMatches?.Count <= 2)
            {
                _ = Task.Run(async () =>
                {
                    var newMatches = await _matchingService.GetPotentialMatchesAsync(_currentUser);
                    if (newMatches?.Count > 0)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            if (_potentialMatches != null)
                            {
                                _potentialMatches.AddRange(newMatches);
                            }
                        });
                    }
                });
            }
        }

        private async void AnimateFloatingOrbs()
        {
            // Get references to floating orbs
            var orb1 = this.FindByName<Ellipse>("FloatingOrb1");
            var orb2 = this.FindByName<Ellipse>("FloatingOrb2");
            var orb3 = this.FindByName<Ellipse>("FloatingOrb3");

            if (orb1 == null || orb2 == null || orb3 == null) return;

            // Animate floating orbs continuously
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        var tasks = new List<Task>
                        {
                            orb1.RotateTo(360, 20000, Easing.Linear),
                            orb2.RotateTo(-360, 25000, Easing.Linear),
                            orb3.RotateTo(360, 30000, Easing.Linear)
                        };

                        await Task.WhenAll(tasks);
                    });
                }
            });

            // Scale animation for orbs
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await Task.WhenAll(
                            orb1.ScaleTo(1.1, 3000, Easing.SinInOut),
                            orb2.ScaleTo(0.9, 4000, Easing.SinInOut),
                            orb3.ScaleTo(1.2, 3500, Easing.SinInOut)
                        );

                        await Task.WhenAll(
                            orb1.ScaleTo(1, 3000, Easing.SinInOut),
                            orb2.ScaleTo(1, 4000, Easing.SinInOut),
                            orb3.ScaleTo(1, 3500, Easing.SinInOut)
                        );
                    });
                }
            });
        }

        // Navigation bar button handlers
        private async void OnDiscoverClicked(object sender, EventArgs e)
        {
            await RefreshMatches();
        }

        private async void OnLiveClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Coming Soon", "Live dating feature will be available soon!", "OK");
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Settings", "Settings page coming soon!", "OK");
        }

        // Action button handlers
        private async void OnRefreshActionClicked(object sender, EventArgs e)
        {
            await RefreshMatches();
        }

        private async void OnNopeActionClicked(object sender, EventArgs e)
        {
            if (_frontCardUser != null)
            {
                await AnimateButton(NopeActionButton);
                await PerformSwipe(SwipeDirection.Left);
            }
        }

        private async void OnLikeActionClicked(object sender, EventArgs e)
        {
            if (_frontCardUser != null)
            {
                await AnimateButton(LikeActionButton);
                await PerformSwipe(SwipeDirection.Right);
            }
        }

        private async void OnSuperLikeActionClicked(object sender, EventArgs e)
        {
            if (_frontCardUser != null)
            {
                await AnimateButton(SuperLikeActionButton);
                await PerformSwipe(SwipeDirection.Up);
            }
        }

        private async void OnBoostActionClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Boost", "Boost feature coming soon!", "OK");
        }

        private async Task AnimateButton(Button button)
        {
            await button.ScaleTo(0.85, 100, Easing.CubicOut);
            await button.ScaleTo(1.1, 100, Easing.CubicOut);
            await button.ScaleTo(1, 100, Easing.CubicOut);
        }

        // Photo navigation
        private void OnPreviousPhotoTapped(object sender, EventArgs e)
        {
            NavigatePhoto(-1);
        }

        private void OnNextPhotoTapped(object sender, EventArgs e)
        {
            NavigatePhoto(1);
        }

        // Refresh functionality
        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            await RefreshMatches();
        }

        private async Task RefreshMatches()
        {
            try
            {
                HapticFeedback.Perform(HapticFeedbackType.Click);
            }
            catch { }

            await LoadPotentialMatches();
        }

        protected override bool OnBackButtonPressed()
        {
            // Handle back button - maybe show exit dialog
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

    public enum SwipeDirection
    {
        Left,
        Right,
        Up
    }
}