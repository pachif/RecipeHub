using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Recipes.Provider;
using Recipes.BusinessObjects;
using System.Collections.ObjectModel;

namespace RecipeHubApp.ViewModels
{
    public class RecipeDetailViewModel : ViewModelBase
    {
        private ItemViewModel item;
        private Visibility vis;
        private Visibility alarmVisibility;

        public RecipeDetailViewModel()
        {
            item = new ItemViewModel();
            Alarms = new ObservableCollection<AlarmItemViewModel>();
            AlarmVisibility = Visibility.Collapsed;
        }

        public ObservableCollection<AlarmItemViewModel> Alarms
        {
            get;
            private set;
        }

        public ItemViewModel CurrentRecipe
        {
            get { return item; }
            set
            {
                item = value;
                OnPropertyChanged(() => CurrentRecipe);
            }
        }

        public Visibility ProgressVisibility
        {
            get { return vis; }
            set
            {
                vis = value;
                OnPropertyChanged(() => ProgressVisibility);
            }
        }

        public Visibility AlarmVisibility
        {
            get { return alarmVisibility; }
            set
            {
                alarmVisibility = value;
                OnPropertyChanged(() => AlarmVisibility);
            }
        }

        public Visibility AlarmConfigVisibility
        {
            get { return alarmVisibility; }
            set
            {
                alarmVisibility = value;
                OnPropertyChanged(() => AlarmConfigVisibility);
            }
        }

        /// <summary>
        /// Load details recipe data for the given url
        /// </summary>
        public void LoadData(string url)
        {
            string stringId = url.Substring(url.LastIndexOf("/"), url.Length - url.LastIndexOf("/"));
            ProgressVisibility = Visibility.Visible;

            // Sample data; replace with real data
            UtilisimaProvider provider = new UtilisimaProvider();
            provider.ObtainRecipeById(stringId);

            provider.ProcessEnded += (s, e) =>
            {
                System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() => UpdateUI(e));
            };
        }

        private void UpdateUI(ResultEventArgs e)
        {
            if (e.HasFail)
            {
                var ex = (Exception)e.Result;
                MessageBox.Show(ex.Message);
                return;
            }

            Recipe item = (Recipe)e.Result;
            var vm = new ItemViewModel
            {
                Author = item.Author,
                Title = item.Title,
                RecipeLink = item.LinkUrl,
                Procedure = item.Procedure,
                Category = item.Category,
                Portions = item.Portions,
                MainIngredient = item.MainIngredient
            };

            vm.SetImageRecipeFrom(item.ImageUrl);
            CurrentRecipe = vm;
            if (item.Ingridients != null)
                item.Ingridients.ForEach(ingr => CurrentRecipe.Ingridients.Add(ingr));
            ProgressVisibility = Visibility.Collapsed;
            UpdateAlarmSection(item);
        }

        private void UpdateAlarmSection(Recipe recipe)
        {
            if (App.AlarmDetection)
            {
                AlarmVisibility = Visibility.Visible;
                AlarmConfigVisibility = Visibility.Collapsed;
                Alarms.Clear();

                if (recipe.Alarms == null || recipe.Alarms.Count == 0)
                    return;
                //- TODO: Show not found message

                foreach (var alarm in recipe.Alarms)
                {
                    var alarmVM = new AlarmItemViewModel
                    {
                        AlarmName = string.Format(RecipeHubApp.AppResx.AlarmHeaderMessage, alarm.Name, alarm.Minutes),
                        Minutes = alarm.Minutes,
                        Checked = false
                    };
                    Alarms.Add(alarmVM);
                }
            }
            else
            {
                AlarmConfigVisibility = Visibility.Visible;
            }
        }
    }

    public class AlarmItemViewModel : ViewModelBase
    {
        private string _lineTwo;
        private bool _checked;
        private double _minutes;

        /// <summary>
        /// Gets or sets the AlarmName
        /// </summary>
        public string AlarmName
        {
            get
            {
                return _lineTwo;
            }
            set
            {
                _lineTwo = value;
                OnPropertyChanged(() => AlarmName);
            }
        }

        public bool Checked
        {
            get
            {
                return _checked;
            }
            set
            {
                _checked = value;
                OnPropertyChanged(() => Checked);
            }
        }

        public double Minutes
        {
            get
            {
                return _minutes;
            }
            set
            {
                _minutes = value;
                OnPropertyChanged(() => Minutes);
            }
        }
    }
}
