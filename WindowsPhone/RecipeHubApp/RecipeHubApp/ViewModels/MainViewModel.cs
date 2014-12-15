using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using Recipes.BusinessObjects;
using Recipes.Provider;
using System.Windows;
using System.Globalization;
using BugSense;

namespace RecipeHubApp
{
    public class MainViewModel : ViewModelBase
    {
        private Visibility _visibility;
        private string _searchText;
        private bool _isDataLoaded;
        private FoxProvider provider;

        public MainViewModel()
        {
            this.RecentRecipes = new ObservableCollection<ItemViewModel>();
            this.FoundRecipes = new ObservableCollection<ItemViewModel>();
            HistoryRecipes = new ObservableCollection<ItemViewModel>();
            ProgressVisibility = Visibility.Collapsed;
            provider = new FoxProvider();
        }

        /// <summary>
        /// A collection for ItemViewModel objects.
        /// </summary>
        public ObservableCollection<ItemViewModel> RecentRecipes { get; private set; }
        
        public ObservableCollection<ItemViewModel> FoundRecipes { get; private set; }

        public ObservableCollection<ItemViewModel> HistoryRecipes { get; private set; }

        public bool IsDataLoaded
        {
            get { return _isDataLoaded; }
            set
            {
                _isDataLoaded = value;
                OnPropertyChanged(() => IsDataLoaded);
            }
        }

        public string SearchText
        {
            get { return _searchText; }
            set
            {
                _searchText = value;
                OnPropertyChanged(() => SearchText);
            }
        }

        public Visibility ProgressVisibility
        {
            get { return _visibility; }
            set
            {
                _visibility = value;
                OnPropertyChanged(() => ProgressVisibility);
            }
        }

        public void LoadSearchResponse()
        {
            ProgressVisibility = Visibility.Visible;
            string url = string.Format("Following search was performed --> search={0}", SearchText);
            BugSenseHandler.Instance.SendEvent(url);
            provider.SearchRecipeByName(SearchText);
            provider.ProcessEnded += SearchProcessEndedHandler;
        }

        public void LoadSearchResponse(int page)
        {
            ProgressVisibility = Visibility.Visible;
            FoundRecipes.Clear();
            string url = string.Format("Following search was performed --> search={0}&page={1}", SearchText, page);
            provider.SearchRecipeByName(SearchText, page);
            provider.ProcessEnded += SearchProcessEndedHandler;
        }

        /// <summary>
        /// Creates and adds a few ItemViewModel objects into the Items collection.
        /// </summary>
        public void LoadData()
        {
            ProgressVisibility = Visibility.Visible;
            provider.ObtainMostRecents();
            provider.ProcessEnded += LoadProcessEndedHandler;

        }

        public void UpdateHistory()
        {
            HistoryRecipes.Clear();
            var list = App.Last10Visit.ToList();
            list.ForEach(item =>
                {
                    if (!HistoryRecipes.Contains(item))
                        HistoryRecipes.Add(item);
                });
        }

        private void LoadProcessEndedHandler(object sender, ResultEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() => { UpdateUI(e); });
            provider.ProcessEnded -= LoadProcessEndedHandler;
        }

        private void SearchProcessEndedHandler(object sender, ResultEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() => UpdateSearchUI(e));
            provider.ProcessEnded -= SearchProcessEndedHandler;
        }

        private void UpdateUI(ResultEventArgs e)
        {
            var list = e.Result as List<Recipe>;
            
            if (list != null)
            {
                foreach (var item in list)
                {
                    var vm = new ItemViewModel { Author = item.Author, Title = item.Title, RecipeLink = item.LinkUrl };
                    vm.SetImageRecipeFrom(item.ImageUrl);
                    RecentRecipes.Add(vm);
                }
                UpdateHistoryUI();
            }
            else if(e.Result is string)
            {
                string msg = (string)e.Result;
                MessageBox.Show(msg, "Error", MessageBoxButton.OK);
            }
            else if (e.Result is Exception)
            {
                var ex = ((Exception)e.Result);
                BugSenseHandler.Instance.SendException(ex);
                string msg = ex.Message;
                MessageBox.Show(msg, "Error", MessageBoxButton.OK);
            }
            this.IsDataLoaded = true;
            ProgressVisibility = Visibility.Collapsed;
        }

        private void UpdateHistoryUI()
        {
            foreach (var item in App.Last10Visit.ToArray())
            {
                item.SetImageRecipeFrom(item.ImageRecipeLink);
                if (!HistoryRecipes.Contains(item))
                    HistoryRecipes.Add(item);
            }
        }

        private void UpdateSearchUI(ResultEventArgs e)
        {
            var list = e.Result as List<Recipe>;
            if (list != null)
            {
                FoundRecipes.Clear();
                foreach (var item in list)
                {
                    var vm = new ItemViewModel { Author = item.Author, Title = item.Title, RecipeLink = item.LinkUrl };
                    vm.SetImageRecipeFrom(item.ImageUrl);
                    FoundRecipes.Add(vm);
                }

                this.IsDataLoaded = true;
                ProgressVisibility = Visibility.Collapsed;
            }
        }
    }
}