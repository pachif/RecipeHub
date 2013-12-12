using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RecipeHubApp
{
    public class ItemViewModel : ViewModelBase
    {
        private const int MAXCHARS = 15;
        private string _title;
        private string _lineTwo;
        private ImageSource _lineThree;
        private string _recipeLink;
        private string _category;
        private ObservableCollection<string> _ingredientes;
        private string _procedure;
        private ICommand command;
        private string _principal;
        private int _portions;

        /// <summary>
        /// Gets or sets the title
        /// </summary>
        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                _title = value;
                OnPropertyChanged(() => Title);
                OnPropertyChanged(() => ShortTitle);
            }
        }

        /// <summary>
        /// Gets or sets the title
        /// </summary>
        public string ShortTitle
        {
            get
            {
                string shortitle = _title;
                if (!string.IsNullOrEmpty(_title) && _title.Length > MAXCHARS)
                    shortitle = string.Format("{0}..", _title.Substring(0, MAXCHARS));
                //OnPropertyChanged(() => ShortTitle);
                return shortitle;
            }
        }

        /// <summary>
        /// Gets or sets the Author
        /// </summary>
        public string Author
        {
            get
            {
                return _lineTwo;
            }
            set
            {
                _lineTwo = value;
                OnPropertyChanged(() => Author);
            }
        }

        /// <summary>
        /// Gets or sets the Category
        /// </summary>
        public string Category
        {
            get
            {
                return _category;
            }
            set
            {
                _category = value;
                OnPropertyChanged(() => Category);
            }
        }

        /// <summary>
        /// Gets or sets the Category
        /// </summary>
        public string Procedure
        {
            get
            {
                return _procedure;
            }
            set
            {
                _procedure = value;
                OnPropertyChanged(() => Procedure);
            }
        }

        /// <summary>
        /// Gets or sets the Main Ingredient
        /// </summary>
        public string MainIngredient
        {
            get
            {
                return _principal;
            }
            set
            {
                _principal = value;
                OnPropertyChanged(() => MainIngredient);
            }
        }

        /// <summary>
        /// Gets or sets the amount of Portions
        /// </summary>
        public int Portions
        {
            get
            {
                return _portions;
            }
            set
            {
                _portions = value;
                OnPropertyChanged(() => Portions);
            }
        }

        /// <summary>
        /// Gets the ingredients
        /// </summary>
        public ObservableCollection<string> Ingridients
        {
            get
            {
                if (_ingredientes == null)
                {
                    _ingredientes = new ObservableCollection<string>();
                }
                return _ingredientes;
            }
        }

        /// <summary>
        /// gets or sets the Image Source
        /// </summary>
        /// <returns></returns>
        public ImageSource ImageRecipe
        {
            get
            {
                return _lineThree;
            }
            set
            {
                _lineThree = value;
                OnPropertyChanged(() => ImageRecipe);
            }
        }

        public string ImageRecipeLink { get; set; }

        /// <summary>
        /// Gets or sets the RecipeLink
        /// </summary>
        public string RecipeLink
        {
            get
            {
                return _recipeLink;
            }
            set
            {
                _recipeLink = value;
                OnPropertyChanged(() => RecipeLink);
            }
        }

        public ICommand ReceipDetailsCommand
        {
            get
            {
                if (command == null)
                {
                    command = new CommandBase(GoToReceipt);
                }
                return command;
            }
        }

        public void SetImageRecipeFrom(string url)
        {
            ImageRecipeLink = url;
            Uri uri = new Uri(url);
            ImageRecipe = new BitmapImage(uri);
        }

        private void GoToReceipt()
        {
            //TODO
            //System.Windows.Navigation.NavigationService.
        }

    }
}