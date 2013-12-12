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
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Shell;

namespace RecipeHubApp
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Set the data context of the listbox control to the sample data
            DataContext = App.ViewModel;
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
        }

        public MainViewModel ViewModel
        {
            get
            {
                return DataContext as MainViewModel;
            }
        }

        // Load data for the ViewModel Items
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
                BuildApplicationBar();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (ViewModel != null)
            {
                ViewModel.UpdateHistory();
                //- Update Background from File
                panorama.Background = new ImageBrush
                {
                    ImageSource = new System.Windows.Media.Imaging.BitmapImage(new Uri(App.BackgroundSource, UriKind.Relative)),
                    Opacity = 0.5
                };
            }
        }

        private void StackPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var stak = sender as StackPanel;
            if (stak == null)
            {
                MessageBox.Show("Seleccionaste cualquiera");
                return;
            }
            var ivm = (ItemViewModel)stak.DataContext;
            //- Store Visits
            App.UpdateVisitHistory(ivm);

            //- Navigate to Page
            string query = string.Format("/RecipeDetailPage.xaml?detail={0}", ivm.RecipeLink);
            var uri = new Uri(query, UriKind.Relative);
            NavigationService.Navigate(uri);
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ViewModel.SearchText = SearchTextBox.Text;
                ViewModel.LoadSearchResponse();
                SearchListBox.Focus();
            }
        }

        private void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/ConfigPage.xaml", UriKind.Relative));
        }

        private void review_Click(object sender, EventArgs e)
        {
            var task = new Microsoft.Phone.Tasks.MarketplaceDetailTask();
            task.Show();
        }

        private void BuildApplicationBar()
        {
            ApplicationBar = new ApplicationBar();

            var appSettingsButton = new ApplicationBarIconButton(new Uri("/settings.png", UriKind.Relative)) { Text = RecipeHubApp.AppResx.SettingsKey };
            appSettingsButton.Click += ApplicationBarIconButton_Click;
            ApplicationBar.Buttons.Add(appSettingsButton);

            var appBarMenuReview = new ApplicationBarMenuItem(RecipeHubApp.AppResx.RateKey + " ...");
            appBarMenuReview.Click += review_Click;
            ApplicationBar.MenuItems.Add(appBarMenuReview);
        }

        private void AdControl_ErrorOccurred(object sender, Microsoft.Advertising.AdErrorEventArgs e)
        {
            BugSense.BugSenseHandler.Instance.SendException(e.Error);
            System.Diagnostics.Debug.WriteLine("AdControl error: " + e.Error.Message); 
        }
    }
}