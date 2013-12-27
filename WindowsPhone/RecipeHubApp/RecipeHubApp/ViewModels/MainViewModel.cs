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

namespace RecipeHubApp
{
    public class MainViewModel : ViewModelBase
    {
        private Visibility _visibility;
        private string _searchText;

        public MainViewModel()
        {
            this.RecentRecipes = new ObservableCollection<ItemViewModel>();
            this.FoundRecipes = new ObservableCollection<ItemViewModel>();
            HistoryRecipes = new ObservableCollection<ItemViewModel>();
            ProgressVisibility = Visibility.Collapsed;
        }

        /// <summary>
        /// A collection for ItemViewModel objects.
        /// </summary>
        public ObservableCollection<ItemViewModel> RecentRecipes { get; private set; }
        
        public ObservableCollection<ItemViewModel> FoundRecipes { get; private set; }

        public ObservableCollection<ItemViewModel> HistoryRecipes { get; private set; }

        public bool IsDataLoaded
        {
            get;
            private set;
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
            UtilisimaProvider provider = new UtilisimaProvider();
            
            string url = string.Format("Following URL was consumed --> http://s.ficfiles.com/utilisima/get_rss.php?seeker=recetas&search={0}&page={1}", SearchText, "0");
            BugSense.BugSenseHandler.Instance.SendEvent(url);
            provider.SearchRecipeByName(SearchText);
            provider.ProcessEnded += (s, e) =>
            {
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => UpdateSearchUI(e));
            };
        }

        public void LoadSearchResponse(int page)
        {
            ProgressVisibility = Visibility.Visible;
            UtilisimaProvider provider = new UtilisimaProvider();

            FoundRecipes.Clear();
            provider.SearchRecipeByName(SearchText, page);
            provider.ProcessEnded += (s, e) =>
            {
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => UpdateSearchUI(e));
            };
        }

        /// <summary>
        /// Creates and adds a few ItemViewModel objects into the Items collection.
        /// </summary>
        public void LoadData()
        {
            ProgressVisibility = Visibility.Visible;

            // Sample data; replace with real data
            UtilisimaProvider provider = new UtilisimaProvider();
            provider.ObtainMostRecents();

            provider.ProcessEnded += (s, e) =>
            {
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => UpdateUI(e));
            };
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

                this.IsDataLoaded = true;
                UpdateHistoryUI();
                ProgressVisibility = Visibility.Collapsed;
            }
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