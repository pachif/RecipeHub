﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Threading;
using System.Windows.Controls.Primitives;
using System.ComponentModel;

namespace RecipeHubApp
{
    public partial class MainPage : PhoneApplicationPage
    {
        BackgroundWorker backroungWorker;
        Popup myPopup;
        private bool loading;
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            //Set initial page
            myPopup = new Popup() { IsOpen = true, Child = new AnimatedSplashScreenControl() };
            backroungWorker = new BackgroundWorker();
            loading = true;
            RunBackgroundWorker();

            // Set the data context of the listbox control to the sample data
            DataContext = App.ViewModel;
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
        }

        public int PageCount { get; set; }        

        public MainViewModel ViewModel
        {
            get
            {
                return DataContext as MainViewModel;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (ViewModel != null)
            {
                PageCount = 0;
                ViewModel.UpdateHistory();
                //- Update Background from File
                panorama.Background = new ImageBrush
                {
                    ImageSource = new System.Windows.Media.Imaging.BitmapImage(new Uri(App.BackgroundSource, UriKind.Relative)),
                    Opacity = 0.5
                };
            }
        }

        // Load data for the ViewModel Items
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
                App.ViewModel.PropertyChanged += new PropertyChangedEventHandler(MainViewModel_PropertyChanged);
                BuildApplicationBar();
            }
        }

        private void MainViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ProgressVisibility" && App.ViewModel.ProgressVisibility == System.Windows.Visibility.Collapsed)
            {
                loading = false;
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

            string url = string.Format("Following Page was requested --> /RecipeDetailPage.xaml?detail={0}", ivm.RecipeLink);
            BugSense.BugSenseHandler.Instance.SendEvent(url);

            //- Navigate to Page
            string query = string.Format("/RecipeDetailPage.xaml?detail={0}", ivm.RecipeLink);
            var uri = new Uri(query, UriKind.Relative);
            NavigationService.Navigate(uri);
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddApplicationBarButton();
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

        private void panorama_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems == null || e.AddedItems.Count.Equals(0)) return;
            string header = ((PanoramaItem)e.AddedItems[0]).Header.ToString();
            if (header != RecipeHubApp.AppResx.SearchKey)
            {
                // remove button
                ApplicationBarIconButton toremove = null;
                foreach (ApplicationBarIconButton appbarButton in ApplicationBar.Buttons)
                {
                    if (appbarButton.Text == AppResx.AddMoreKey) {
                        toremove = appbarButton;
                        break;
                    }
                }

                ApplicationBar.Buttons.Remove(toremove);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(ViewModel.SearchText)) {
                    ViewModel.FoundRecipes.Clear();
                    PageCount = 0;
                }

                if (ViewModel.FoundRecipes.Count > 0)
                {
                    AddApplicationBarButton();
                }
            }
        }

        private void AddApplicationBarButton()
        {
            // check if the button already exist
            if (ApplicationBar.Buttons.OfType<ApplicationBarIconButton>().Any(x => x.Text == AppResx.AddMoreKey))
                return;

            // add the load more items button in ApplicationBar
            var appAddMoreButton = new ApplicationBarIconButton(new Uri("/add.png", UriKind.Relative)) { Text = RecipeHubApp.AppResx.AddMoreKey };
            appAddMoreButton.Click += OnAddMoreClick;
            ApplicationBar.Buttons.Add(appAddMoreButton);
        }

        private void OnAddMoreClick(object sender, EventArgs e)
        {
            if (ViewModel.ProgressVisibility == System.Windows.Visibility.Visible)
            {
                MessageBox.Show(AppResx.SystemBusyMessageKey);
                return;
            }

            if (!string.IsNullOrWhiteSpace(ViewModel.SearchText))
            {   // is not busy and user performed a previous search
                PageCount++;
            }
            ViewModel.LoadSearchResponse(PageCount);
        }

        private void RunBackgroundWorker()
        {
            backroungWorker.DoWork += ((s, args) =>
            {
                while (loading)
                {
                    Thread.Sleep(1000);
                }
                
            });

            backroungWorker.RunWorkerCompleted += ((s, args) =>
            {
                this.Dispatcher.BeginInvoke(() =>
                {
                    this.myPopup.IsOpen = false;
                }
            );
            });
            backroungWorker.RunWorkerAsync();
        }
    }
}