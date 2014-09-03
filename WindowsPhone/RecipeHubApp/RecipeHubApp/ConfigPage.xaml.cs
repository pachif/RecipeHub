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

namespace RecipeHubApp
{
    public partial class ConfigPage : PhoneApplicationPage
    {
        public ConfigPage()
        {
            InitializeComponent();
            Loaded += new RoutedEventHandler(ConfigPage_Loaded);
            DataContext = new ConfigsViewModel();
        }

        public ConfigsViewModel ViewModel
        {
            get { return DataContext as ConfigsViewModel; }
        }

        private void ConfigPage_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.FillBackgrounds();
            ViewModel.FillSearchEngines();
            ViewModel.DetectionActive = App.AlarmDetection;
        }

        private void backGroundComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            App.BackgroundSource = ((BackgroundItem)backGroundComboBox.SelectedValue).BackImg;
            ViewModel.UpdateSelection();
        }

        private void ToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            App.AlarmDetection = true;
        }

        private void ToggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            App.AlarmDetection = false;
        }

        private void SEToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            var sevm = ((System.Windows.FrameworkElement)(sender)).DataContext as SearchEngineItem;
            if (sevm != null)
            {
                //TODO do something
            }
        }

        private void SEToggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            // TODO Do something
            string name = (string)((SearchEngineItem)((System.Windows.FrameworkElement)(sender)).DataContext).Name;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var task = new Microsoft.Phone.Tasks.MarketplaceDetailTask();
            task.Show();
        }
    }
}