namespace DateApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Use Shell for navigation
            MainPage = new AppShell();
        }

        protected override Window CreateWindow(IActivationState activationState)
        {
            var window = base.CreateWindow(activationState);

            // Set minimum window size for desktop
#if WINDOWS
            window.MinimumHeight = 600;
            window.MinimumWidth = 400;
#endif

            return window;
        }

        protected override void OnStart()
        {
            // Handle when your app starts
            base.OnStart();
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
            base.OnSleep();
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
            base.OnResume();
        }
    }
}