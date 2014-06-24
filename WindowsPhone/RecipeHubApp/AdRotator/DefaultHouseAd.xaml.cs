using System;
using System.Linq;
using System.IO.IsolatedStorage;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows;
using System.Reflection;
using System.Windows.Resources;

namespace AdRotator
{
    public partial class DefaultHouseAd : UserControl
    {
        public object HouseAdBody;

        public Uri DefaultHouseAdURI;

        public delegate void OnAdFailed(object sender, EventArgs e);
        public delegate void OnAdLoaded(object sender, EventArgs e);
        public delegate void OnAdClicked(object sender, EventArgs e);

        public event OnAdFailed AdLoadingFailed;
        public event OnAdLoaded AdLoaded;
        public event OnAdClicked AdClicked;

        public bool isValid = true;

        public DefaultHouseAd()
        {
            InitializeComponent();
        }

        public void GetDefaultHouseAd(string LocalHouseAdBodyName , string URL = "")
        {
            //object o = null;
            //var a = GetAssemblyType(LocalHouseAdBodyName);
            //if (a != null)
            //{
            //    o = Activator.CreateInstance(a);
            //}

            var o = createObject(LocalHouseAdBodyName);

            //check to see if the class is instantiated or not
            if (o != null)
            {
                HouseAdBody = o;
            }
             
            if (!string.IsNullOrEmpty(URL))
            {
                DefaultHouseAdURI = new Uri(URL, UriKind.RelativeOrAbsolute);
                GetRemoteHouseAdControl();
            }
            else
            {
                LoadProjectDefaultAd();
            }
            this.Tap += new EventHandler<System.Windows.Input.GestureEventArgs>(DefaultHouseAd_Tap);

            DataContext = this;

        }

        void DefaultHouseAd_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (AdClicked != null)
            {
                AdClicked("", new EventArgs());
            }
        }

        private void GetRemoteHouseAdControl()
        {
            try
            {
                //Remote Ad Process
                if (AdRotatorControl.IsNetworkAvailable && DefaultHouseAdURI != null)
                {
                    Networking.Network.GetStringFromURL(DefaultHouseAdURI, (s, e) =>
                    {
                        Dispatcher.BeginInvoke(() =>
                            {
                                if (e != null)
                                {
                                    LoadCachedAd();
                                }
                                else
                                {
                                    try
                                    {
                                        this.Content = (FrameworkElement)XamlReader.Load(s);
                                        if (IsolatedStorageSettings.ApplicationSettings.Contains("RemoteDefaultHouseAd"))
                                        {
                                            IsolatedStorageSettings.ApplicationSettings.Remove("RemoteDefaultHouseAd");
                                        }
                                        IsolatedStorageSettings.ApplicationSettings.Add("RemoteDefaultHouseAd", s);
                                        if (AdLoaded != null)
                                        {
                                            AdLoaded(null, new EventArgs());
                                        }
                                    }
                                    catch
                                    {
                                        LoadCachedAd();
                                    }
                                }
                            });
                    });
                }
                else
                {
                    LoadCachedAd();
                }
            }
            catch
            {
                if (HouseAdBody == null)
                {
                    if (AdLoadingFailed != null)
                    {
                        AdLoadingFailed("", new EventArgs());
                    }
                }
                else
                {
                    LoadCachedAd();
                }
            }
        }

        private void LoadCachedAd()
        {
            try
            {
                if (IsolatedStorageSettings.ApplicationSettings.Contains("RemoteDefaultHouseAd") && DefaultHouseAdURI != null)
                {
                    try
                    {
                        this.Content = (FrameworkElement)XamlReader.Load((string)IsolatedStorageSettings.ApplicationSettings["RemoteDefaultHouseAd"]);
                        if (AdLoaded != null)
                        {
                            AdLoaded(null, new EventArgs());
                        }
                    }
                    catch
                    {
                        LoadProjectDefaultAd();
                    }
                }
                else
                {
                    LoadProjectDefaultAd();
                }
            }
            catch (Exception error)
            {
                isValid = false;
                var value = error;
                if (AdLoadingFailed != null)
                {
                    AdLoadingFailed("", new EventArgs());
                }
            }
        }

        private void LoadProjectDefaultAd()
        {
            if (HouseAdBody == null)
            {
                isValid = false;
                if (AdLoadingFailed != null)
                {
                    AdLoadingFailed("Get Remote Ad Failed", new EventArgs());
                }
            }
            else
            {
                this.Content = (FrameworkElement)HouseAdBody;
                if (AdLoaded != null)
                {
                    AdLoaded(null, new EventArgs());
                }
            }
        }

        public static Type GetAssemblyType(string assemblyName, string className)
        {
            Type type = null;
            try
            {
                AssemblyName name = new AssemblyName(assemblyName.Substring(0, assemblyName.IndexOf(".")));
                var asm = Assembly.Load(name);
                type = asm.GetType(className);
            }
            catch { }

            return type;
        }

        public static Type GetAssemblyType(string className)
        {
            Type type = null;
            foreach (AssemblyPart part in Deployment.Current.Parts)
            {
                var test = part.GetType();
                type = GetAssemblyType(part.Source, className);
                if (type != null)
                    break;
            }
            return type;
        }

        public static object createObject(string name, params object[] constructorargs)
        {
            if (string.IsNullOrEmpty(name) || !name.Contains("."))
            {
                return null;

            }
            var asmName = name.Substring(0, name.IndexOf("."));
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.FullName.Contains(asmName)))
            {
                var typ = asm.GetExportedTypes().FirstOrDefault(type => type.FullName == name);
                if (typ != null)
                    return Activator.CreateInstance(typ, constructorargs);
            }
            if (AdRotatorControl.IsWP8)
            {
                var ExternalType = GetAssemblyType(name);
                if (ExternalType != null)
                {
                    return Activator.CreateInstance(ExternalType, constructorargs);
                }
            }

            return null;
        }


    }
}
