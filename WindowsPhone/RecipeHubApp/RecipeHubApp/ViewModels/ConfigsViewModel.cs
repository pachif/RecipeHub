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
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RecipeHubApp
{
    public class ConfigsViewModel : ViewModelBase
    {
        public ConfigsViewModel()
        {
            Backgrounds = new ObservableCollection<BackgroundItem>();
            SearchEngines = new ObservableCollection<SearchEngineItem>();
        }

        public void FillBackgrounds()
        {
            Backgrounds.Add(new BackgroundItem { BackImg = "PanoramaBackground.png", BackName = "Default" });
            Backgrounds.Add(new BackgroundItem { BackImg = "/Backgrounds/PanoramaBackground1.png", BackName = "pattern 1" });
            Backgrounds.Add(new BackgroundItem { BackImg = "/Backgrounds/PanoramaBackground2.png", BackName = "pattern 2" });
            Backgrounds.Add(new BackgroundItem { BackImg = "/Backgrounds/PanoramaBackground3.png", BackName = "stripes 3" });
            Backgrounds.Add(new BackgroundItem { BackImg = "/Backgrounds/PanoramaBackground4.png", BackName = "stripes 4" });
        }

        public void FillSearchEngines()
        {
            SearchEngines.Add(new SearchEngineItem("Fox Life", true));
            SearchEngines.Add(new SearchEngineItem("Utilisima", false));
        }

        private bool _detectionActive;

        public bool DetectionActive
        {
            get { return _detectionActive; }
            set
            {
                _detectionActive = value;
                OnPropertyChanged(() => DetectionActive);
            }
        }


        public ObservableCollection<BackgroundItem> Backgrounds { get; private set; }

        public ObservableCollection<SearchEngineItem> SearchEngines { get; private set; }

        private ICommand _deleteHistoryCommand;

        public ICommand DeleteHistoryCommand
        {
            get
            {
                if (_deleteHistoryCommand == null)
                {
                    _deleteHistoryCommand = new CommandBase(DeleteHistory);
                }
                return _deleteHistoryCommand;
            }
        }

        public void UpdateSelection()
        {
            foreach (var item in Backgrounds)
            {
                item.CheckCurrent();
            }
        }

        private void DeleteHistory()
        {
            //var resources = Application.Current.Resources["LocalizedResx"] as ApplicationResx;
            var result = MessageBox.Show(AppResx.ConfirmMessage, "Confirm", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                App.Last10Visit.Clear();
            }
        }
    }

    public class BackgroundItem : ViewModelBase
    {
        private string _backImg;
        private string _backName;
        private Thickness _isSelected;

        public string BackImg
        {
            get { return _backImg; }
            set
            {
                _backImg = value;
                OnPropertyChanged(() => BackImg);
            }
        }
        public string BackName
        {
            get { return _backName; }
            set
            {
                _backName = value;
                OnPropertyChanged(() => BackName);
            }
        }

        public Thickness IsSelected
        {
            get
            {
                _isSelected = CheckCurrent();
                return _isSelected;
            }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(() => IsSelected);
                }
            }
        }

        public Thickness CheckCurrent()
        {
            double d = App.BackgroundSource.Trim().Equals(this.BackImg) ? 1.0 : 0.0;
            var thickness = IsSelected = new Thickness(d);
            return thickness;
        }
    }

    public class SearchEngineItem : ViewModelBase
    {
        private string name;
        private bool isActive;

        public SearchEngineItem(string name, bool isActive = false)
        {
            Name = name;
            IsActive = isActive;
        }

        public string Name
        {
            get { return name; }
            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged(() => Name);
                }
            }
        }

        public bool IsActive
        {
            get { return isActive; }
            set
            {
                if (isActive != value)
                {
                    isActive = value;
                    OnPropertyChanged(() => IsActive);
                }
            }
        }
    }
}
