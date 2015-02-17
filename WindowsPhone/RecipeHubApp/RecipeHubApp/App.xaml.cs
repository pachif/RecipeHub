using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Marketplace;
using BugSense;
using System.Diagnostics;
using RecipeHubApp.Resources;

namespace RecipeHubApp
{
    public partial class App : Application
    {
        private static MainViewModel viewModel = null;

        /// <summary>
        /// A static ViewModel used by the views to bind against.
        /// </summary>
        /// <returns>The MainViewModel object.</returns>
        public static MainViewModel ViewModel
        {
            get
            {
                // Delay creation of the view model until necessary
                if (viewModel == null)
                    viewModel = new MainViewModel();

                return viewModel;
            }
        }

        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public PhoneApplicationFrame RootFrame { get; private set; }

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            // Global handler for uncaught exceptions. 
            // Note that exceptions thrown by ApplicationBarItem.Click will not get caught here.
            UnhandledException += Application_UnhandledException;
            

            // Show graphics profiling information while debugging.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Display the current frame rate counters.
                Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode, 
                // which shows areas of a page that are being GPU accelerated with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;
            }

            // Standard Silverlight initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();
            BugSenseHandler.Instance.InitAndStartSession(this, "e9243712");
        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            string value = DataStorage.ReadAlarmDetectionSetting();
            if (!string.IsNullOrEmpty(value))
                detection = bool.Parse(value);
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
            }
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            BumpVisitHistoryIntoFile();
            BumpSettingsFile();
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            BugSenseHandler.Instance.SendException(e.ExceptionObject);
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => MessageBox.Show(AppResx.ProblemServerKey));
            Debug.WriteLine(e.ExceptionObject);
            if (Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                Debugger.Break();
            }
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new TransitionFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        #endregion

        #region Last Visited Section
        private static Stack<ItemViewModel> _lastVisit;

        public static Stack<ItemViewModel> Last10Visit
        {
            get
            {
                if (_lastVisit == null)
                {
                    _lastVisit = new Stack<ItemViewModel>();
                    //- Grab from File
                    var list = DataStorage.ReadHistoryFile();
                    if (list != null)
                        list.ForEach(item => UpdateVisitHistory(item));
                }
                return _lastVisit;
            }
        }

        public static void UpdateVisitHistory(ItemViewModel ivm)
        {
            if (_lastVisit.Count == 10)
                _lastVisit.Pop();

            if (!_lastVisit.Any(x=>x.RecipeLink == ivm.RecipeLink))
                _lastVisit.Push(ivm);
        }

        private static void BumpVisitHistoryIntoFile()
        {
            //- File creation
            DataStorage.WriteHistoryFile(Last10Visit);
        }

        #endregion

        #region App Settings
        private static string backgroundSource;
        private static Dictionary<string, string> SettingsDic = new Dictionary<string, string>();
        private const string BackgroundSettingKey = "background";
        private const string DetectionSettingKey = "detection";
        private static bool detection;
        private static bool? isTrial = null;

        public static bool IsTrialMode
        {
            get {
                if (isTrial == null)
                {
                    Microsoft.Phone.Marketplace.LicenseInformation license = new Microsoft.Phone.Marketplace.LicenseInformation();
                    isTrial = license.IsTrial();
                }
                return isTrial.Value;
            }
        }

        public static bool AlarmDetection
        {
            get { return detection; }
            set {
                if (detection != value)
                    UpdateSettingDictionary("detection", value.ToString());
                detection = value; }
        }

        public static string BackgroundSource
        {
            get
            {
                if (string.IsNullOrEmpty(backgroundSource))
                    backgroundSource = GetCurrentBackgound();
                return backgroundSource;
            }
            set
            {
                if (backgroundSource != value)
                    UpdateSettingDictionary(BackgroundSettingKey, value);
                backgroundSource = value;
            }
        }

        public static bool GetCurrentDetectionAlarm()
        {
            bool result = false;
            string setting = DataStorage.ReadAlarmDetectionSetting();
            if (!string.IsNullOrEmpty(setting))
            {
                result = bool.Parse(setting);
            }
            UpdateSettingDictionary(BackgroundSettingKey, setting);
            return result;
        }

        public static string GetCurrentBackgound()
        {
            string background = DataStorage.ReadBackgroundSetting();
            if (string.IsNullOrEmpty(background))
            {
                background = "PanoramaBackground.png";
            }
            UpdateSettingDictionary(BackgroundSettingKey, background);
            return background;
        }

        private void BumpSettingsFile()
        {
            DataStorage.WriteSettingsFile(SettingsDic);
        }

        private static void UpdateSettingDictionary(string key, string value)
        {
            if (!SettingsDic.ContainsKey(key))
                SettingsDic.Add(key, null);

            SettingsDic[key] = value;
        }

        #endregion
    }
}