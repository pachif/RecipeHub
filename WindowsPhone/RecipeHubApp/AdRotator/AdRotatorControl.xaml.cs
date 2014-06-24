using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Xml.Serialization;
using AdRotator.Model;
using Microsoft.Phone.Net.NetworkInformation;
using InneractiveAdSDK = Inneractive.Nokia.Ad;
namespace AdRotator
{
    public partial class AdRotatorControl : UserControl, IDisposable
    {
        private bool _loaded = false;

        private bool _initialised = false;

        private bool _slidingAdHidden = false;

        private bool _slidingAdTimerStarted = false;

        public delegate void LogHandler(string message);

        public event LogHandler Log;

        private AdType CurrentAdType;

        private AdCultureDescriptor CurrentCulture = null;

        private const string SETTINGS_FILE_NAME = "AdRotatorSettings";

        public static bool IsWP8 { get { return Environment.OSVersion.Version >= TargetedVersion; } }

        private static Version TargetedVersion = new Version(8, 0);

        public bool IsInDesignMode
        {
            get
            {
                return DesignerProperties.GetIsInDesignMode(this);
            }
        }

        /// <summary>
        /// The displayed ad control instance
        /// </summary>
        private object _currentAdControl;

        /// <summary>
        /// Random generato
        /// </summary>
        private static Random _rnd = new Random();

        /// <summary>
        /// List of the ad types that have failed to load
        /// </summary>
        private static List<AdType> _failedAdTypes = new List<AdType>();

        /// <summary>
        /// The ad settings based on which the ad descriptor for the current UI culture can be selected
        /// </summary>
        private static AdSettings _settings;

        /// <summary>
        /// Indicates whether there has been an attemt to fetch the remote settings file
        /// </summary>
        private static bool _remoteAdSettingsFetched = false;

        /// <summary>
        /// Local variable to allow other components of AdRotator to access settings without UI Thread
        /// </summary>
        private string _settingsURL;

        /// <summary>
        /// Indicates whether a network has been detected and available, is turned off if none found
        /// </summary>
        public static bool IsNetworkAvailable { get; set; }

        #region LoggingEventCode
        protected void OnLog(string message)
        {
            if (Log != null)
            {
                Log(message);
            }
        }
        #endregion

        #region SettingsUrl

        /// <summary>
        /// Gets or sets the URL of the remote ad descriptor file
        /// </summary>
        public string SettingsUrl
        {
            get { return (string)GetValue(SettingsUrlProperty); }
            set { SetValue(SettingsUrlProperty, value); _settingsURL = value.ToString(); }
        }

        public static readonly DependencyProperty SettingsUrlProperty = DependencyProperty.Register("SettingsUrl", typeof(string), typeof(AdRotatorControl), new PropertyMetadata(""));

        //Force Loading Settings Manually
        //private static void SettingsUrlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
        //    var sender = d as AdRotatorControl;
        //    if(sender != null)
        //    {
        //        sender.OnSettingsUrlChanged(e);
        //    }
        //}

        //private void OnSettingsUrlChanged(DependencyPropertyChangedEventArgs e)
        //{
        //    if (!IsInDesignMode)
        //    {
        //        FetchAdSettingsThreaded();
        //    }
        //}

#endregion

        #region DefaultSettingsFileUri

        public Uri DefaultSettingsFileUri
        {
            get { return (Uri)GetValue(DefaultSettingsFileUriProperty); }
            set { SetValue(DefaultSettingsFileUriProperty, value); }
        }

        public static readonly DependencyProperty DefaultSettingsFileUriProperty = DependencyProperty.Register("DefaultSettingsFileUri", typeof(Uri), typeof(AdRotatorControl), new PropertyMetadata(DefaultSettingsFileUriChanged));

        private static void DefaultSettingsFileUriChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = d as AdRotatorControl;
            if (sender != null)
            {
                sender.OnDefaultSettingsFileUriChanged(e);
            }
        }

        private void OnDefaultSettingsFileUriChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!_initialised && string.IsNullOrEmpty(SettingsUrl))
            {
                if (_loaded)
                {
                    LoadAdSettings();
                    Dispatcher.BeginInvoke(() =>
                    {
                        Invalidate();
                    });               
                }

            }
        }

        #endregion

        #region DefaultAdType

        public AdType DefaultAdType
        {
            get { return (AdType)GetValue(DefaultAdTypeProperty); }
            set { SetValue(DefaultAdTypeProperty, value); }
        }

        public static readonly DependencyProperty DefaultAdTypeProperty = DependencyProperty.Register("DefaultAdType", typeof(AdType), typeof(AdRotatorControl), new PropertyMetadata(AdType.None));

        #endregion

        #region IsEnabled

        /// <summary>
        /// When set to false the control does not display
        /// </summary>
        public bool IsAdRotatorEnabled
        {
            get { return (bool)GetValue(IsEnabledProperty); }
            set { SetValue(IsEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsAdRotatorEnabledProperty = DependencyProperty.Register("IsAdRotatorEnabled", typeof(bool), typeof(AdRotatorControl), new PropertyMetadata(true, IsAdRotatorEnabledChanged));

        private static void IsAdRotatorEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = d as AdRotatorControl;
            if (sender != null)
            {
                sender.OnIsEnabledChangedChanged(e);
            }
        }

        private void OnIsEnabledChangedChanged(DependencyPropertyChangedEventArgs e)
        {
            IsEnabled = (bool)e.NewValue;
            if (!IsEnabled)
            {
                SlidingAdDirection = SlideDirection.None;
                SlidingAdTimer = null;
            }
            if (_initialised)
            {

                Dispatcher.BeginInvoke(() =>
                {
                    Invalidate();
                });
            }

        }

        #endregion

        #region IsInitialised

        public bool IsInitialised
        {
            get { return _initialised; }
        }

        #endregion

        #region SlidingAd Properties

        #region SlidingAdDirection

        /// <summary>
        /// Direction the popup will hide / appear from
        /// If not set the AdControl will remain on screen
        /// </summary>
        public SlideDirection SlidingAdDirection
        {
            get { return (SlideDirection)GetValue(SlidingAdDirectionProperty); }
            set { SetValue(SlidingAdDirectionProperty, value); }
        }

        public static readonly DependencyProperty SlidingAdDirectionProperty = DependencyProperty.Register("SlidingAdDirection", typeof(SlideDirection), typeof(AdRotatorControl), new PropertyMetadata(SlideDirection.None, SlidingAdDirectionChanged));

        private static void SlidingAdDirectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = d as AdRotatorControl;
            if (sender != null)
            {
                sender.OnSlidingAdDirectionChanged(e);
            }
        }

        private void OnSlidingAdDirectionChanged(DependencyPropertyChangedEventArgs e)
        {
            switch ((SlideDirection)e.NewValue)
            {
                case SlideDirection.Left:
                    SlideOutLRAdStoryboardAnimation.To = -485;
                    SlideInLRAdStoryboardAnimation.From = -485;
                    break;
                case SlideDirection.Right:
                    SlideOutLRAdStoryboardAnimation.To = 485;
                    SlideInLRAdStoryboardAnimation.From = 485;
                    break;
                case SlideDirection.Bottom:
                    SlideOutUDAdStoryboardAnimation.To = 485;
                    SlideInUDAdStoryboardAnimation.From = 485;
                    break;
                case SlideDirection.Top:
                    SlideOutUDAdStoryboardAnimation.To = -485;
                    SlideInUDAdStoryboardAnimation.From = -485;
                    break;
                default:
                    SlideOutLRAdStoryboardAnimation.To = 0;
                    SlideInLRAdStoryboardAnimation.From = 0;
                    SlideOutUDAdStoryboardAnimation.To = 0;
                    SlideInUDAdStoryboardAnimation.From = 0;
                    break;
            }
        }


        #endregion

        #region SlidingAdDisplaySeconds

        /// <summary>
        /// Amount of time in seconds the ad is displayed on Screen if <see cref="SlidingAdDirection"/> is set to something else than None
        /// </summary>
        public int SlidingAdDisplaySeconds
        {
            get { return (int)GetValue(SlidingAdDisplaySecondsProperty); }
            set { SetValue(SlidingAdDisplaySecondsProperty, value); }
        }

        public static readonly DependencyProperty SlidingAdDisplaySecondsProperty = DependencyProperty.Register("SlidingAdDisplaySeconds", typeof(int), typeof(AdRotatorControl), new PropertyMetadata(10));

        #endregion

        #region SlidingAdHiddenSeconds

        /// <summary>
        ///  Amount of time in seconds to wait before displaying the ad again 
        ///  (if <see cref="SlidingAdDirection"/> is set to something else than None).
        ///  Basically the lower this number the more the user is "nagged" by the ad coming back now and again
        /// </summary>
        public int SlidingAdHiddenSeconds
        {
            get { return (int)GetValue(SlidingAdHiddenSecondsProperty); }
            set { SetValue(SlidingAdHiddenSecondsProperty, value); }
        }

        public static readonly DependencyProperty SlidingAdHiddenSecondsProperty = DependencyProperty.Register("SlidingAdHiddenSeconds", typeof(int), typeof(AdRotatorControl), new PropertyMetadata(20));

        #endregion
        #endregion

        #region ADProviderProperties

        #region DefaultHouseAd

        #region DefaultHouseAdBody



        public string DefaultHouseAdBody
        {
            get
            {
                if (string.IsNullOrEmpty((string)GetValue(DefaultHouseAdBodyProperty)))
                {
                    SetValue(DefaultHouseAdBodyProperty, GetSecondaryID(AdType.DefaultHouseAd));
                }
                return (string)GetValue(DefaultHouseAdBodyProperty);
            }
            set { SetValue(DefaultHouseAdBodyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DefaultHouseAdBody.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DefaultHouseAdBodyProperty =
            DependencyProperty.Register("DefaultHouseAdBody", typeof(string), typeof(AdRotatorControl), new PropertyMetadata(string.Empty));

        #endregion

        #region DefaultHouseAdURI

        public string DefaultHouseAdURI
        {
            get
            {
                if (string.IsNullOrEmpty((string)GetValue(DefaultHouseAdURIProperty)))
                {
                    SetValue(DefaultHouseAdURIProperty, GetAppID(AdType.DefaultHouseAd));
                }
                return (string)GetValue(DefaultHouseAdURIProperty);
            }
            set { SetValue(DefaultHouseAdURIProperty, value); }
        }

        public static readonly DependencyProperty DefaultHouseAdURIProperty = DependencyProperty.Register("DefaultHouseAdURI", typeof(string), typeof(AdRotatorControl), new PropertyMetadata(String.Empty));

        #endregion

        public delegate void DefaultHouseAdClickEventHandler();

        public event DefaultHouseAdClickEventHandler DefaultHouseAdClick;
        #endregion

        #region Pubcenter
        #region PubCenterAppId

        public string PubCenterAppId
        {
            get 
            {
                if (string.IsNullOrEmpty((string)GetValue(PubCenterAppIdProperty)))
                {
                    SetValue(PubCenterAppIdProperty,GetAppID(AdType.PubCenter));
                }
                return (string)GetValue(PubCenterAppIdProperty);
            }
            set { SetValue(PubCenterAppIdProperty, value); }
        }

        public static readonly DependencyProperty PubCenterAppIdProperty = DependencyProperty.Register("PubCenterAppId", typeof(string), typeof(AdRotatorControl), new PropertyMetadata(String.Empty));

        #endregion

        #region PubCenterAdUnitId

        public string PubCenterAdUnitId
        {
            get 
            {
                if (string.IsNullOrEmpty((string)GetValue(PubCenterAdUnitIdProperty)))
                {
                    SetValue(PubCenterAdUnitIdProperty,GetSecondaryID(AdType.PubCenter));
                }
                return (string)GetValue(PubCenterAdUnitIdProperty);
            }
            set { SetValue(PubCenterAdUnitIdProperty, value); }
        }

        public static readonly DependencyProperty PubCenterAdUnitIdProperty = DependencyProperty.Register("PubCenterAdUnitId", typeof(string), typeof(AdRotatorControl), new PropertyMetadata(""));

        #endregion
        #endregion

        #region AdDuplexAppId

        public string AdDuplexAppId
        {
            get
            {
                if (string.IsNullOrEmpty((string)GetValue(AdDuplexAppIdProperty)))
                {
                    SetValue(AdDuplexAppIdProperty, GetAppID(AdType.AdDuplex));
                }
                return (string)GetValue(AdDuplexAppIdProperty);
            }
            set { SetValue(AdDuplexAppIdProperty, value); }
        }

        public static readonly DependencyProperty AdDuplexAppIdProperty = DependencyProperty.Register("AdDuplexAppId", typeof(string), typeof(AdRotatorControl), new PropertyMetadata(""));

        #endregion

        #region Inneractive

        #region InneractiveAppId

        public string InneractiveAppId
        {
            get
            {
                if (string.IsNullOrEmpty((string)GetValue(InneractiveAppIdProperty)))
                {
                    SetValue(InneractiveAppIdProperty, GetAppID(AdType.InnerActive));
                }
                return (string)GetValue(InneractiveAppIdProperty);
            }
            set { SetValue(InneractiveAppIdProperty, value); }
        }

        public static readonly DependencyProperty InneractiveAppIdProperty = DependencyProperty.Register("InneractiveAppId", typeof(string), typeof(AdRotatorControl), new PropertyMetadata(String.Empty));

        #endregion

        #region InneractiveExternalId

        public string InneractiveExternalId
        {
            get { return (string)GetValue(InneractiveExternalIdProperty); }
            set { SetValue(InneractiveExternalIdProperty, value); }
        }

        public static readonly DependencyProperty InneractiveExternalIdProperty = DependencyProperty.Register("InneractiveExternalId", typeof(string), typeof(AdRotatorControl), new PropertyMetadata(String.Empty));

        #endregion

        #region InneractiveGender

        public string InneractiveGender
        {
            get { return (string)GetValue(InneractiveGenderProperty); }
            set { SetValue(InneractiveGenderProperty, value); }
        }

        public static readonly DependencyProperty InneractiveGenderProperty = DependencyProperty.Register("InneractiveGender", typeof(string), typeof(AdRotatorControl), new PropertyMetadata(String.Empty));

        #endregion

        #region InneractiveAge

        public string InneractiveAge
        {
            get { return (string)GetValue(InneractiveAgeProperty); }
            set { SetValue(InneractiveAgeProperty, value); }
        }

        public static readonly DependencyProperty InneractiveAgeProperty = DependencyProperty.Register("InneractiveAge", typeof(string), typeof(AdRotatorControl), new PropertyMetadata(String.Empty));

        #endregion

        #region InneractiveKeywords

        public string InneractiveKeywords
        {
            get { return (string)GetValue(InneractiveKeywordsProperty); }
            set { SetValue(InneractiveKeywordsProperty, value); }
        }

        public static readonly DependencyProperty InneractiveKeywordsProperty = DependencyProperty.Register("InneractiveKeywords", typeof(string), typeof(AdRotatorControl), new PropertyMetadata(String.Empty));

        #endregion

        #region InneractiveReloadTime

        public int InneractiveReloadTime
        {
            get { return (int)GetValue(InneractiveReloadTimeProperty); }
            set { SetValue(InneractiveReloadTimeProperty, value); }
        }

        public static readonly DependencyProperty InneractiveReloadTimeProperty = DependencyProperty.Register("InneractiveReloadTime", typeof(int), typeof(AdRotatorControl), new PropertyMetadata(60));

        #endregion
        #endregion
#if(!WP8)
        #region MobFox

        public string MobFoxAppId
        {
            get 
            {
                if (string.IsNullOrEmpty((string)GetValue(MobFoxAppIdProperty)))
                {
                    SetValue(MobFoxAppIdProperty,GetAppID(AdType.MobFox));
                }
                return (string)GetValue(MobFoxAppIdProperty);
            }
            set { SetValue(MobFoxAppIdProperty, value); }
        }

        public static readonly DependencyProperty MobFoxAppIdProperty = DependencyProperty.Register("MobFoxAppId", typeof(string), typeof(AdRotatorControl), new PropertyMetadata(""));


        public bool MobFoxIsTest
        {
            get { return (bool)GetValue(MobFoxIsTestProperty); }
            set { SetValue(MobFoxIsTestProperty, value); }
        }

        public static readonly DependencyProperty MobFoxIsTestProperty = DependencyProperty.Register("MobFoxIsTest", typeof(bool), typeof(AdRotatorControl), new PropertyMetadata(false));

        #endregion

        #region AdMobAdUnitId

        public string AdMobAdUnitId
        {
            get
            {
                if (string.IsNullOrEmpty((string)GetValue(AdMobAdUnitIdProperty)))
                {
                    SetValue(AdMobAdUnitIdProperty, GetAppID(AdType.AdMob));
                }
                return (string)GetValue(AdMobAdUnitIdProperty);
            }
            set { SetValue(AdMobAdUnitIdProperty, value); }
        }

        public static readonly DependencyProperty AdMobAdUnitIdProperty = DependencyProperty.Register("AdMobAdUnitId", typeof(string), typeof(AdRotatorControl), new PropertyMetadata(""));

        #endregion
#endif
#if(!NONLOCATION)

        #region Smaato

        #region SmaatoPublisherId

        public string SmaatoPublisherId
        {
            get 
            {
                if (string.IsNullOrEmpty((string)GetValue(SmaatoPublisherIdProperty)))
                {
                    SetValue(SmaatoPublisherIdProperty,GetSecondaryID(AdType.Smaato));
                }
                return (string)GetValue(SmaatoPublisherIdProperty);
            }
            set { SetValue(SmaatoPublisherIdProperty, value); }
        }

        public static readonly DependencyProperty SmaatoPublisherIdProperty = DependencyProperty.Register("SmaatoPublisherId", typeof(string), typeof(AdRotatorControl), new PropertyMetadata(""));
        #endregion

        #region SmaatoAppId
        public string SmaatoAppId
        {
            get 
            {
                if (string.IsNullOrEmpty((string)GetValue(SmaatoAppIdProperty)))
                {
                    SetValue(SmaatoAppIdProperty,GetAppID(AdType.Smaato));
                }
                return (string)GetValue(SmaatoAppIdProperty);
            }
            set { SetValue(SmaatoAppIdProperty, value); }
        }

        public static readonly DependencyProperty SmaatoAppIdProperty = DependencyProperty.Register("SmaatoAppId", typeof(string), typeof(AdRotatorControl), new PropertyMetadata(""));

        #endregion

        #endregion
#endif

        #region AdValidSettings

        private bool IsDefaultHouseAdValid
        {
            get
            {
                return DefaultHouseAdBody != null || !string.IsNullOrEmpty(DefaultHouseAdURI);
            }
        }
        
        private bool IsPubCenterValid
        {
            get
            {
                return !String.IsNullOrEmpty(PubCenterAppId) && !String.IsNullOrEmpty(PubCenterAdUnitId);
            }
        }

        private bool IsAdDuplexValid
        {
            get
            {
                return !String.IsNullOrEmpty(AdDuplexAppId);
            }
        }
#if(!WP8)
        private bool IsMobFoxValid
        {
            get
            {
                return !String.IsNullOrEmpty(MobFoxAppId);
            }
        }

        private bool IsAdMobValid
        {
            get
            {
                return !String.IsNullOrEmpty(AdMobAdUnitId);
            }
        }
#endif
#if(!NONLOCATION)

        private bool IsSmaatoValid
        {
            get
            {
                int SmaatoIDTest;
                return (!String.IsNullOrEmpty(SmaatoAppId) && int.TryParse(SmaatoAppId, out SmaatoIDTest) && !String.IsNullOrEmpty(SmaatoPublisherId) && int.TryParse(SmaatoPublisherId, out SmaatoIDTest));
            }
        }
#endif
        private bool IsInnerActiveValid
        {
            get
            {
                return !String.IsNullOrEmpty(InneractiveAppId);
            }
        }

        private bool IsAdTypeValid(AdType adType)
        {
            switch (adType)
            {
                case AdType.PubCenter:
                    OnLog(string.Format("Testing \"{0}\" - Result {1}", adType.ToString(), IsPubCenterValid.ToString()));
                    return IsPubCenterValid;
                case AdType.AdDuplex:
                    OnLog(string.Format("Testing \"{0}\" - Result {1}", adType.ToString(), IsAdDuplexValid.ToString()));
                    return IsAdDuplexValid;
#if(!WP8)
                    case AdType.AdMob:
                    OnLog(string.Format("Testing \"{0}\" - Result {1}", adType.ToString(), IsAdMobValid.ToString()));
                    return IsAdMobValid;
                case AdType.MobFox:
                    OnLog(string.Format("Testing \"{0}\" - Result {1}", adType.ToString(), IsMobFoxValid.ToString()));
                    return IsMobFoxValid;
#endif
#if(!NONLOCATION)
                case AdType.Smaato:
                    OnLog(string.Format("Testing \"{0}\" - Result {1}", adType.ToString(), IsSmaatoValid.ToString()));
                    return IsSmaatoValid;
#endif
                case AdType.InnerActive:
                    OnLog(string.Format("Testing \"{0}\" - Result {1}", adType.ToString(), IsInnerActiveValid.ToString()));
                    return IsInnerActiveValid;
                case AdType.DefaultHouseAd:
                    OnLog(string.Format("Testing \"{0}\" - Result {1}", adType.ToString(), IsDefaultHouseAdValid.ToString()));
                    return IsDefaultHouseAdValid;

            }
            //Davide Cleopadre www.cleosolutions.com
            //if for any reason the AdType cannot be found is not valid
            //if we add new ads type the control will continue to work
            //also not updated
            return false;
        }

        #endregion

        #endregion

        #region AdWidth

        /// <summary>
        /// When set to false the control does not display
        /// </summary>
        public int AdWidth
        {
            get { return (int)GetValue(AdWidthProperty); }
            set { SetValue(AdWidthProperty, value); }
        }

        public static readonly DependencyProperty AdWidthProperty = DependencyProperty.Register("AdWidth", typeof(int), typeof(AdRotatorControl), new PropertyMetadata(480, AdWidthChanged));

        private static void AdWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = d as AdRotatorControl;
            if (sender != null)
            {
                sender.AdWidthChanged(e);
            }
        }

        private void AdWidthChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        #endregion

        #region AdHeight

        /// <summary>
        /// When set to false the control does not display
        /// </summary>
        public int AdHeight
        {
            get { return (int)GetValue(AdHeightProperty); }
            set { SetValue(AdHeightProperty, value); }
        }

        public static readonly DependencyProperty AdHeightProperty = DependencyProperty.Register("AdHeight", typeof(int), typeof(AdRotatorControl), new PropertyMetadata(80, AdHeightChanged));

        private static void AdHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = d as AdRotatorControl;
            if (sender != null)
            {
                sender.AdHeightChanged(e);
            }
        }

        private void AdHeightChanged(DependencyPropertyChangedEventArgs e)
        {
        }

        #endregion

        #region PubCenter Enabled Fix
        public bool ApplyIsEnabledFix
        {
            get { return (bool)GetValue(ApplyIsEnabledFixProperty); }
            set { SetValue(ApplyIsEnabledFixProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ApplyIsEnabledFix.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ApplyIsEnabledFixProperty =
            DependencyProperty.Register("ApplyIsEnabledFix", typeof(bool), typeof(AdRotatorControl), new PropertyMetadata(false));
        #endregion



        public AdRotatorControl()
        {
            InitializeComponent();

            //FetchAdSettingsFile();
            this.Loaded += new RoutedEventHandler(AdRotatorControl_Loaded);
        }

        void AdRotatorControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (IsInDesignMode)
            {
                LayoutRoot.Children.Add(new TextBlock() { Text = "AdRotator in design mode, No ads will be displayed", VerticalAlignment = System.Windows.VerticalAlignment.Center });
            }

            //Assume Network is available until we know for sure it isn't
            IsNetworkAvailable = true;
            //if (!NetworkInterface.GetIsNetworkAvailable() || NetworkInterface.NetworkInterfaceType == NetworkInterfaceType.None)
            //{
            //    IsNetworkAvailable = false;
            //}

            _loaded = true;

        }

         /// <summary>
        /// Displays a new ad
        /// </summary>
        /// <param name="selectNextAdType">If set to true, selects the next ad type in order, otherwise chooses 
        ///     a random one that hasn't had issues loading previously</param>
        /// <returns></returns>
        public string Invalidate(bool selectNextAdType = false)
        {
            string adTypeName = "";

            if (!_loaded || !_initialised)
            {
                if (!_initialised)
                {
                    LoadAdSettings();
                    return "";
                }
                else
                {
                    OnLog("Control tested before loaded");
                    return "";
                }

            }
            if (!IsAdRotatorEnabled)
            {
                OnLog("Ad control disabled");
                Visibility = Visibility.Collapsed;
                return adTypeName;
            }
            else
            {
                OnLog("Ads are enabled for display");
                Visibility = Visibility.Visible;
            }
            if (LayoutRoot == null)
            {
                OnLog("No layout to attach to");
                return adTypeName;
            }

            if (SlidingAdDirection != SlideDirection.None && !_slidingAdTimerStarted)
            {
                _slidingAdTimerStarted = true;
                ResetSlidingAdTimer(SlidingAdDisplaySeconds);           
            }

            RemoveEventHandlersFromAdControl();
            
            LayoutRoot.Children.Clear();
            AdType adType;
            //if (IsNetworkAvailable)
            //{
                if (selectNextAdType)
                {
                    adType = CurrentAdType++;
                    if (adType >= AdType.None)
                    {
                        adType = AdType.None;
                    }
                }
                else
                {
                    adType = GetNextAdType();
                }
            //}
            //else
            //{
            //    CurrentCulture = CurrentCulture == null ? GetAdDescriptorBasedOnUICulture() : CurrentCulture;
            //    if (CurrentCulture == null)
            //    {
            //        //Failed to get any add configuration, control disabled
            //        adType = AdType.None;
            //    }
            //    else
            //    {
            //        var defaultHouseAd = CurrentCulture.AdProbabilities.FirstOrDefault(x => x.AdType == AdType.DefaultHouseAd);
            //        if (defaultHouseAd != null && !_failedAdTypes.Contains(AdType.DefaultHouseAd))
            //        {
            //            adType = AdType.DefaultHouseAd;
            //        }
            //        else
            //        {
            //            adType = AdType.None;
            //        }
            //    }
            //}
            OnLog(string.Format("Ads being requested for: {0}", adType.ToString()));
            switch (adType)
            {                
                case AdType.PubCenter:
                    if (_currentAdControl as Microsoft.Advertising.Mobile.UI.AdControl == null)
                        _currentAdControl = CreatePubCentertAdControl();
                    break;
                case AdType.AdDuplex:
                    if (_currentAdControl as AdDuplex.AdControl == null)
                        _currentAdControl = CreateAdDuplexControl();
                    break;
#if(!WP8)
                case AdType.AdMob:
                    if (_currentAdControl as Google.AdMob.Ads.WindowsPhone7.WPF.BannerAd == null)
                        _currentAdControl = CreateAdMobAdControl();
                    break;
                case AdType.MobFox:
                    if(!IsWP8)
                        if(_currentAdControl as MobFox.Ads.AdControl == null)
                            _currentAdControl = CreateMobFoxControl();
                    break;
#endif
                case AdType.InnerActive:
                    if (_currentAdControl as InneractiveAdSDK.InneractiveAd == null)
                        _currentAdControl = CreateInneractiveControl();
                    break;
#if(!NONLOCATION)
                case AdType.Smaato:
#if(WP8)
                    if (_currentAdControl as SOMAWP8.SomaAdViewer == null)
#else
                    if (_currentAdControl as SOMAWP7.SomaAdViewer == null)
#endif
                        _currentAdControl = CreateSmaatoControl();
                    break;
#endif
                case AdType.DefaultHouseAd:
                        _currentAdControl = CreateDefaultHouseAdControl();
                    break;
                default:
                    break;
                    
            }


            if (_currentAdControl as NoneProvider != null || adType == AdType.None)
            {
                IsAdRotatorEnabled = false;
                Visibility = Visibility.Collapsed;
                OnLog("No ads available");
                return adTypeName;
            }
            else
            {
                Visibility = Visibility.Visible;
                AddEventHandlersToAdControl();
                LayoutRoot.Children.Add((FrameworkElement)_currentAdControl);
                OnLog(string.Format("Ads being served for: {0}", adType.ToString()));
            }

            if (CurrentAdType != AdType.None)
            {
                adTypeName = adType.ToString();
                OnLog(string.Format("Ads being served for: {0}", adType.ToString()));
            }
            else
            {
                OnLog("No ads available, nothing to show");
            }
            return adTypeName;
        }

        /// <summary>
        /// Generates what the next ad type to display should be
        /// </summary>
        /// <returns></returns>
        private AdType GetNextAdType()
        {
            if (CurrentCulture == null)
            {
                CurrentCulture = GetAdDescriptorBasedOnUICulture();
            }

            if (CurrentCulture == null)
            {
                return DefaultAdType;
            }

            var validDescriptors = CurrentCulture.AdProbabilities
                .Where(x => !_failedAdTypes.Contains(x.AdType)
                            && x.ProbabilityValue > 0
                            && IsAdTypeValid(x.AdType))
                .ToList();
            var defaultHouseAd = CurrentCulture.AdProbabilities.FirstOrDefault(x => x.AdType == AdType.DefaultHouseAd && !_failedAdTypes.Contains(x.AdType));
            if (validDescriptors.Count > 0)
            {
                var totalValueBetweenValidAds = validDescriptors.Sum(x => x.ProbabilityValue);
                var randomValue = _rnd.NextDouble() * totalValueBetweenValidAds;
                double totalCounter = 0;
                foreach (var probabilityDescriptor in validDescriptors)
                {
                    totalCounter += probabilityDescriptor.ProbabilityValue;
                    if (randomValue < totalCounter)
                    {
                        return probabilityDescriptor.AdType;
                    }
                }
            }
            if (defaultHouseAd != null )
            {
                return AdType.DefaultHouseAd;
            }
            return !_failedAdTypes.Contains(DefaultAdType) ? DefaultAdType : AdType.None;
        }

        /// <summary>
        /// Called when the settings have been loaded. Clears all failed ad types and invalidates the control
        /// </summary>
        private void Init()
        {
            _failedAdTypes.Clear();
            Dispatcher.BeginInvoke(() =>
            {
                Invalidate();
            });
        }



        #region Ad Event Handlers
        private void AddEventHandlersToAdControl()
        {
            var pubCenterAd = _currentAdControl as Microsoft.Advertising.Mobile.UI.AdControl;
            if (pubCenterAd != null)
            {
                pubCenterAd.AdRefreshed += new EventHandler(pubCenterAd_AdRefreshed);
                pubCenterAd.ErrorOccurred += new EventHandler<Microsoft.Advertising.AdErrorEventArgs>(pubCenterAd_ErrorOccurred);
            }
#if(!WP8)
            var adMobAd = _currentAdControl as Google.AdMob.Ads.WindowsPhone7.WPF.BannerAd;
            if (adMobAd != null)
            {
                adMobAd.AdFailed += adMobAd_AdFailed;
                adMobAd.AdReceived += adMobAd_AdReceived;
                adMobAd.AdLeavingApplication += adMobAd_AdLeavingApplication;
                adMobAd.AdPresentingScreen += adMobAd_AdPresentingScreen;
            }

            var mobFoxAd = _currentAdControl as MobFox.Ads.AdControl;
            if (mobFoxAd != null)
            {
                mobFoxAd.NoAd += new MobFox.Ads.NoAdEventHandler(mobFox_NoAd);
                mobFoxAd.NewAd += new MobFox.Ads.NewAdEventHandler(mobFox_NewAd);
            }
#endif
#if(!NONLOCATION)
#if(WP8)
            var somaAd = _currentAdControl as SOMAWP8.SomaAdViewer;
#else
            var somaAd = _currentAdControl as SOMAWP7.SomaAdViewer;
#endif
            if (somaAd != null)
            {
                somaAd.NewAdAvailable += somaAd_NewAdAvailable;
                somaAd.AdError += somaAd_AdError;
            }
#endif

        }

        private void RemoveEventHandlersFromAdControl()
        {
            var pubCenterAd = _currentAdControl as Microsoft.Advertising.Mobile.UI.AdControl;
            if (pubCenterAd != null)
            {
                pubCenterAd.AdRefreshed -= new EventHandler(pubCenterAd_AdRefreshed);
                pubCenterAd.ErrorOccurred -= new EventHandler<Microsoft.Advertising.AdErrorEventArgs>(pubCenterAd_ErrorOccurred);
            }
#if(!WP8)
            var adMobAd = _currentAdControl as Google.AdMob.Ads.WindowsPhone7.WPF.BannerAd;
            if (adMobAd != null)
            {
                adMobAd.AdFailed -= new Google.AdMob.Ads.WindowsPhone7.ErrorEventHandler(adMobAd_AdFailed);
                adMobAd.AdReceived -= new RoutedEventHandler(adMobAd_AdReceived);
            }
            var mobFoxAd = _currentAdControl as MobFox.Ads.AdControl;
            if (mobFoxAd != null)
            {
                mobFoxAd.NoAd -= new MobFox.Ads.NoAdEventHandler(mobFox_NoAd);
                mobFoxAd.NewAd -= new MobFox.Ads.NewAdEventHandler(mobFox_NewAd);
            }
#endif
#if(!NONLOCATION)
#if(WP8)
            var somaAd = _currentAdControl as SOMAWP8.SomaAdViewer;
#else
            var somaAd = _currentAdControl as SOMAWP7.SomaAdViewer;
#endif
            if (somaAd != null)
            {
                somaAd.NewAdAvailable -= somaAd_NewAdAvailable;
                somaAd.AdError -= somaAd_AdError;
            }
#endif
            var DefaultHouseAd = _currentAdControl as DefaultHouseAd;
            if (DefaultHouseAd != null)
            {
                ((DefaultHouseAd)DefaultHouseAd).AdClicked -= defaultHouseAd_AdClicked;
                ((DefaultHouseAd)DefaultHouseAd).AdLoaded -= defaultHouseAd_AdLoaded;
                ((DefaultHouseAd)DefaultHouseAd).AdLoadingFailed -= defaultHouseAd_AdLoadingFailed;
            }

        }

        private AdCultureDescriptor GetAdDescriptorBasedOnUICulture()
        {
            if (_settings == null || _settings.CultureDescriptors == null)
            {
                return null;
            }
            var cultureLongName = Thread.CurrentThread.CurrentUICulture.Name;
            if(String.IsNullOrEmpty(cultureLongName))
            {
                cultureLongName = AdSettings.DEFAULT_CULTURE;
            }
            var cultureShortName = cultureLongName.Substring(0, 2);
            var descriptor = _settings.CultureDescriptors.Where(x => x.CultureName == cultureLongName).FirstOrDefault();
            if (descriptor != null)
            {
                return descriptor;
            }
            var sameLanguageDescriptor = _settings.CultureDescriptors.Where(x => x.CultureName.StartsWith(cultureShortName)).FirstOrDefault();
            if (sameLanguageDescriptor != null)
            {
                return sameLanguageDescriptor;
            }
            var defaultDescriptor = _settings.CultureDescriptors.Where(x => x.CultureName == AdSettings.DEFAULT_CULTURE).FirstOrDefault();
            if (defaultDescriptor != null)
            {
                return defaultDescriptor;
            }
            return null;
        }

        private void RemoveAdFromFailedAds(AdType adType)
        {
            if(_failedAdTypes.Contains(adType))
            {
                _failedAdTypes.Remove(adType);
            }
        }

        /// <summary>
        /// Called when <paramref name="adType"/> has failed to load
        /// </summary>
        /// <param name="adType"></param>
        private void OnAdLoadFailed(AdType adType)
        {
            OnLog(string.Format("Ads failed request for: {0}", adType.ToString()));
            if (!_failedAdTypes.Contains(adType))
            {
                _failedAdTypes.Add(adType);
            }
            Dispatcher.BeginInvoke(() =>
            {
                Invalidate();
            });
        }

        /// <summary>
        /// Called when <paramref name="adType"/> has succeeded to load
        /// </summary>
        /// <param name="adType"></param>
        private void OnAdLoadSucceeded(AdType adType)
        {
            OnLog(string.Format("Ads being successfully served for: {0}", adType.ToString()));
            if (_failedAdTypes.Contains(adType))
            {
                _failedAdTypes.Remove(adType);
            }
        }

        #endregion

        #region Ad Settings Retrieval
        private string GetAppID(AdType adType)
        {
            return CurrentCulture.AdProbabilities
                    .Where(x => x.AdType == adType)
                    .First().AppID;
        }

        private string GetSecondaryID(AdType adType)
        {
            return CurrentCulture.AdProbabilities
                    .Where(x => x.AdType == adType)
                    .First().SecondaryID;
        }

        #endregion

        #region Save and Load
        /// <summary>
        /// Loads the ad settings object either from isolated storage or from the resource path defined in DefaultSettingsFileUri.
        /// </summary>
        /// <returns></returns>
        private void LoadAdSettings()
        {
            //If not checked remote && network available - get remote
            if (!_remoteAdSettingsFetched && !String.IsNullOrEmpty(_settingsURL) && IsNetworkAvailable)
            {
                FetchAdSettingsThreaded();
            }
            else
            {
                if (!String.IsNullOrEmpty(_settingsURL))
                {
                    // if successful set and invalidate
                    try
                    {
                        var isfData = IsolatedStorageFile.GetUserStoreForApplication();
                        IsolatedStorageFileStream isfStream = null;
                        if (isfData.FileExists(SETTINGS_FILE_NAME))
                        {
                            using (isfStream = new IsolatedStorageFileStream(SETTINGS_FILE_NAME, FileMode.Open, isfData))
                            {
                                XmlSerializer xs = new XmlSerializer(typeof(AdSettings));
                                try
                                {
                                    _settings = (AdSettings)xs.Deserialize(isfStream);
                                    FinishLoadingSettings();
                                }
                                catch { }
                            }
                        }
                    }
                    finally 
                    {
                        FinishLoadingSettings();
                    }
                }
                else
                {
                    FinishLoadingSettings();
                }
            }

            
        }

        private void FinishLoadingSettings()
        {
            if (_settings == null)
            {
                _settings = GetDefaultSettings();
            }
            if (_settings == null)
            {
                OnLog("Ad control disabled no settings available");
                Visibility = Visibility.Collapsed;
                IsEnabled = false;
            }
            else
            {
                //Everything OK, continue loading
                _initialised = true;
                Invalidate();
            }
        }

        private AdSettings GetDefaultSettings()
        {
            if (DefaultSettingsFileUri != null)
            {
                var xs = new XmlSerializer(typeof(AdSettings));
                try
                {
                    var defaultSettingsFileInfo = Application.GetResourceStream(DefaultSettingsFileUri);
                    var settings = (AdSettings)xs.Deserialize(defaultSettingsFileInfo.Stream);
                    return settings;
                }
                catch (Exception e)
                {
                    var error = e;
                }
            }
            return new AdSettings();
        }

        /// <summary>
        /// Fetches the ad settings file from the address specified at using a seperate thread <see cref=""/>
        /// </summary>
        private void FetchAdSettingsThreaded()
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(FetchAdSettingsFile));
        }
        /// <summary>
        /// Fetches the ad settings file from the address specified
        /// </summary>
        public void FetchAdSettingsFile(Object stateInfo)
        {
            var request = (HttpWebRequest)HttpWebRequest.Create(new Uri(_settingsURL));
                request.BeginGetResponse(r =>
                {
                    try
                    {
                        var httpRequest = (HttpWebRequest)r.AsyncState;
                        var httpResponse = (HttpWebResponse)httpRequest.EndGetResponse(r);
                        var settingsStream = httpResponse.GetResponseStream();

                        var s = new XmlSerializer(typeof(AdSettings));
                        _settings = (AdSettings)s.Deserialize(settingsStream);
                        // Only persist the settings if they've been retreived from the remote file
                        SaveAdSettings(_settings);
                        _remoteAdSettingsFetched = true;
                        _initialised = true;
                        Dispatcher.BeginInvoke(Init);
                    }
                    catch 
                    {
                        _remoteAdSettingsFetched = true;
                        IsNetworkAvailable = false;
                        _initialised = true;
                        Dispatcher.BeginInvoke(LoadAdSettings);
                    }
                }, request);
            
        }


        /// <summary>
        /// Saves the passed settings file to isolated storage
        /// </summary>
        /// <param name="settings"></param>
        private static void SaveAdSettings(AdSettings settings)
        {
            try
            {
                XmlSerializer xs = new XmlSerializer(typeof(AdSettings));
                IsolatedStorageFileStream isfStream = new IsolatedStorageFileStream(SETTINGS_FILE_NAME, FileMode.Create, IsolatedStorageFile.GetUserStoreForApplication());
                xs.Serialize(isfStream, settings);
                isfStream.Close();
            }
            catch
            {
            }
        }

        #endregion

        #region Specific ad controls

        #region DefaultHouseAd


        private object CreateDefaultHouseAdControl()
        {
            var defaultHouseAd = new DefaultHouseAd();
            defaultHouseAd.Width = AdWidth;
            defaultHouseAd.Height = AdHeight;
            ((DefaultHouseAd)defaultHouseAd).AdClicked += new DefaultHouseAd.OnAdClicked(defaultHouseAd_AdClicked);
            ((DefaultHouseAd)defaultHouseAd).AdLoaded += new DefaultHouseAd.OnAdLoaded(defaultHouseAd_AdLoaded);
            ((DefaultHouseAd)defaultHouseAd).AdLoadingFailed += new DefaultHouseAd.OnAdFailed(defaultHouseAd_AdLoadingFailed);
            defaultHouseAd.GetDefaultHouseAd(DefaultHouseAdBody, DefaultHouseAdURI);

            if (defaultHouseAd == null || !defaultHouseAd.isValid)
            {
                OnLog(string.Format("No Default HouseAd Config available"));
                return new Model.NoneProvider();
            }
            else
            {
                OnLog(string.Format("DefaultHouseAd Created"));
                return defaultHouseAd;
            }


        }

        void defaultHouseAd_AdLoadingFailed(object sender, EventArgs e)
        {
            OnLog(string.Format("Failed loading Default HouseAd"));
            OnAdLoadFailed(AdType.DefaultHouseAd);
        }

        void defaultHouseAd_AdLoaded(object sender, EventArgs e)
        {
            OnAdLoadSucceeded(AdType.DefaultHouseAd);
        }

        void defaultHouseAd_AdClicked(object sender, EventArgs e)
        {
            OnLog(string.Format("DefaultHouseAd Clicked"));
            if (DefaultHouseAdClick != null)
            {
                DefaultHouseAdClick();
            }
        }

        #endregion

        #region Pubcenter
        private object CreatePubCentertAdControl()
        {
            try
            {
                var pubCenterAdControl = new Microsoft.Advertising.Mobile.UI.AdControl(
                    PubCenterAppId,
                    PubCenterAdUnitId,
                    true); // isAutoRefreshEnabled
                pubCenterAdControl.Width = AdWidth;
                pubCenterAdControl.Height = AdHeight;
                pubCenterAdControl.IsEnabled = !ApplyIsEnabledFix;
                return pubCenterAdControl;
            }
            catch (Exception)
            {
                return new Model.NoneProvider();
            }

        }


        void pubCenterAd_AdRefreshed(object sender, EventArgs e)
        {
            OnLog(string.Format("pubCenter Success: {0}", e.ToString()));
            OnAdLoadSucceeded(AdType.PubCenter);
        }

        void pubCenterAd_ErrorOccurred(object sender, Microsoft.Advertising.AdErrorEventArgs e)
        {
            OnLog(string.Format("PubCenter Failed: {0}", e.Error.Message.ToString()));
            OnAdLoadFailed(AdType.PubCenter);
        }

        #endregion

        #region AdDuplex
        private object CreateAdDuplexControl()
        {
            try
            {
                var adDuplexAd = new AdDuplex.AdControl();
                adDuplexAd.AppId = AdDuplexAppId;
                //adDuplexAd.IsTest = true;
                return adDuplexAd;
            }
            catch (Exception)
            {
                return new Model.NoneProvider();
            }
        }

        #endregion

        #region Inneractive
        private object CreateInneractiveControl()
        {
            try
            {
                Dictionary<InneractiveAdSDK.InneractiveAd.IaOptionalParams, string> optionalParams = new Dictionary<InneractiveAdSDK.InneractiveAd.IaOptionalParams, string>();
                optionalParams.Add(InneractiveAdSDK.InneractiveAd.IaOptionalParams.Key_Distribution_Id, "659");
                if (InneractiveGender != String.Empty)
                {
                    optionalParams.Add(InneractiveAdSDK.InneractiveAd.IaOptionalParams.Key_Gender, InneractiveGender);
                }
                if (InneractiveAge != String.Empty)
                {
                    optionalParams.Add(InneractiveAdSDK.InneractiveAd.IaOptionalParams.Key_Age, InneractiveAge);
                }
                if (InneractiveKeywords != String.Empty)
                {
                    optionalParams.Add(InneractiveAdSDK.InneractiveAd.IaOptionalParams.Key_Keywords, InneractiveKeywords);
                }
                InneractiveAdSDK.InneractiveAd inneractiveAd = new InneractiveAdSDK.InneractiveAd("MyCompany_MyApp", InneractiveAdSDK.InneractiveAd.IaAdType.IaAdType_Banner, 60, optionalParams);

                inneractiveAd.AdReceived += new InneractiveAdSDK.InneractiveAd.IaAdReceived(InneractiveAd_AdReceived);
                inneractiveAd.DefaultAdReceived += new InneractiveAdSDK.InneractiveAd.IaDefaultAdReceived(InneractiveAd_DefaultAdReceived);
                inneractiveAd.AdFailed += new InneractiveAdSDK.InneractiveAd.IaAdFailed(InneractiveAd_AdFailed);
                inneractiveAd.AdClicked += new InneractiveAdSDK.InneractiveAd.IaAdClicked(InneractiveAd_AdClicked);

                return inneractiveAd;
            }
            catch
            {
                return false;
            }         
        }

        void InneractiveAd_AdReceived(object sender)
        {
            OnLog(string.Format("InnerActive Success:"));
            OnAdLoadSucceeded(AdType.InnerActive);
        }

        void InneractiveAd_DefaultAdReceived(object sender)
        {
            OnLog(string.Format("InnerActive Success:"));
            OnAdLoadSucceeded(AdType.InnerActive);
        }

        void InneractiveAd_AdFailed(object sender)
        {
            OnLog(string.Format("InnerActive Failed"));
            OnAdLoadFailed(AdType.InnerActive);
        }

        void InneractiveAd_AdClicked(object sender)
        {
            System.Diagnostics.Debug.WriteLine("InneractiveAd: AdClicked");
        }


        #endregion

#if(!WP8)
        #region MobFox
        private object CreateMobFoxControl()
        {

            try
            {
                var MobFoxAd = new MobFox.Ads.AdControl();
                MobFoxAd.PublisherID = MobFoxAppId;
                MobFoxAd.AutoRotate = true;
                MobFoxAd.TestMode = MobFoxIsTest;
                MobFoxAd.RequestNextAd();
                return MobFoxAd;
            }
            catch (Exception)
            {
                return new Model.NoneProvider();
            }
        }


        void mobFox_NewAd(object sender, MobFox.Ads.NewAdEventArgs e)
        {
            OnAdLoadSucceeded(AdType.MobFox);
        }

        void mobFox_NoAd(object sender, MobFox.Ads.NoAdEventArgs e)
        {
            OnAdLoadFailed(AdType.MobFox);
        }

        #endregion

        #region AdMob

        private object CreateAdMobAdControl()
        {
            try
            {
                var adMobAd = new Google.AdMob.Ads.WindowsPhone7.WPF.BannerAd();
                adMobAd.AdUnitID = AdMobAdUnitId;
                adMobAd.Height = AdHeight;
                adMobAd.Width = AdWidth;
                return adMobAd;
            }
            catch (Exception)
            {
                return new Model.NoneProvider();
            }
        }


        void adMobAd_AdReceived(object sender, RoutedEventArgs e)
        {
            OnLog(string.Format("Admob Success:"));
            OnAdLoadSucceeded(AdType.AdMob);
        }

        void adMobAd_AdFailed(object sender, Google.AdMob.Ads.WindowsPhone7.AdException exception)
        {
            OnLog(string.Format("Admob Failed: {0}", exception.Message.ToString()));
            OnAdLoadFailed(AdType.AdMob);
        }

        private void adMobAd_AdLeavingApplication(object sender, RoutedEventArgs e)
        {
        }

        private void adMobAd_AdPresentingScreen(object sender, RoutedEventArgs e)
        {
        }

        #endregion
#endif
#if(!NONLOCATION)

        #region Smaato
        private object CreateSmaatoControl()
        {
            try
            {
#if(WP8)
                var SomaAd = new SOMAWP8.SomaAdViewer();
#else
                var SomaAd = new SOMAWP7.SomaAdViewer();
#endif
                SomaAd.Pub = int.Parse(SmaatoPublisherId);
                SomaAd.Adspace = int.Parse(SmaatoAppId);
                SomaAd.AdSpaceWidth = AdWidth;
                SomaAd.AdSpaceHeight = AdHeight;
                SomaAd.LocationUseOK = true;
                SomaAd.PopupAd = false;
                //Note - Do NOT use the Debug flag as it has issues
                SomaAd.StartAds();
                return SomaAd;
            }
            catch (Exception)
            {
                return new Model.NoneProvider();
            }
        }

        void somaAd_AdError(object sender, string ErrorCode, string ErrorDescription)
        {
            OnLog(string.Format("Smaato Failed: {0} - {1} ", ErrorCode, ErrorDescription));
            OnAdLoadFailed(AdType.Smaato);
        }

        void somaAd_NewAdAvailable(object sender, EventArgs e)
        {
            OnLog(string.Format("Smaato Success: {0}", e.ToString()));
            OnAdLoadSucceeded(AdType.Smaato);
        }
        #endregion
#endif
        #endregion

        #region Animation Events
        private void SlideOutAdStoryboard_Completed(object sender, EventArgs e)
        {
            _slidingAdHidden = true;
            Dispatcher.BeginInvoke(() =>
            {
                Invalidate();
            });
            ResetSlidingAdTimer(SlidingAdHiddenSeconds);
        }

        private void SlideInAdStoryboard_Completed(object sender, EventArgs e)
        {
            _slidingAdHidden = false;
            ResetSlidingAdTimer(SlidingAdDisplaySeconds);
        }

        private void ResetSlidingAdTimer(int durationInSeconds)
        {
            if (IsAdRotatorEnabled)
            {
                SlidingAdTimer.Duration = new TimeSpan(0, 0, durationInSeconds);
                SlidingAdTimer.Begin();               
            }

        }

        private void SlidingAdTimer_Completed(object sender, EventArgs e)
        {
            switch (SlidingAdDirection)
            {
                case SlideDirection.Top:
                case SlideDirection.Bottom:
                    if (_slidingAdHidden)
                    {
                        SlideInUDAdStoryboard.Begin();
                    }
                    else
                    {
                        SlideOutUDAdStoryboard.Begin();
                    }
                    break;
                case SlideDirection.Left:
                case SlideDirection.Right:
                    if (_slidingAdHidden)
                    {
                        SlideInLRAdStoryboard.Begin();
                    }
                    else
                    {
                        SlideOutLRAdStoryboard.Begin();
                    }
                    break;
                default:
                    break;
            }
        }
        #endregion

        public void Dispose()
        {
            LayoutRoot.Children.Clear();
            _currentAdControl = null;
            DefaultHouseAdBody = null;
        }
    }
}
