﻿using System;
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
using RecipeHubApp.ViewModels;
using Microsoft.Phone.Scheduler;
using RecipeHubApp.Resources;

namespace RecipeHubApp
{
    public partial class RecipeDetailPage : PhoneApplicationPage
    {
        public RecipeDetailPage()
        {
            InitializeComponent();
            DataContext = new RecipeDetailViewModel();
            Loaded += new RoutedEventHandler(RecipeDetailPage_Loaded);
        }

        private void RecipeDetailPage_Loaded(object sender, RoutedEventArgs ev)
        {
            adRotatorControl1.Log += new AdRotator.AdRotatorControl.LogHandler(adRotatorControl1_Log);
            adRotatorControl1.Invalidate();
        }

        private void adRotatorControl1_Log(string message)
        {
            System.Diagnostics.Debug.WriteLine("AdRotator: " + message);
        }

        public RecipeDetailViewModel ViewModel
        {
            get { return DataContext as RecipeDetailViewModel; }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            string query = this.NavigationContext.QueryString["detail"];
            ViewModel.LoadData(query);
        }

        private void ToggleSwitch_Checked(object sender, RoutedEventArgs e)
        {
            var alarm = ((System.Windows.FrameworkElement)(sender)).DataContext as AlarmItemViewModel;
            if (alarm!=null)
            {
                var already = ScheduledActionService.Find(alarm.AlarmName);
                if (already != null)
                    ScheduledActionService.Remove(alarm.AlarmName);

                var task = new Alarm(alarm.AlarmName)
                {
                    Content = string.Format(AppResx.AlarmContentKey, ViewModel.CurrentRecipe.Title, alarm.AlarmName),
                    RecurrenceType = RecurrenceInterval.None,
                    Sound = new Uri("alarm-ring.wma", UriKind.Relative)
                };
                task.BeginTime = DateTime.Now.AddMinutes(alarm.Minutes-0.5);
                task.ExpirationTime = DateTime.Now.AddMinutes(alarm.Minutes);
                
                ScheduledActionService.Add(task);
            }
        }

        private void ToggleSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            // The scheduled action name is stored in the Tag property
            // of the delete button for each reminder.
            string name = (string)((AlarmItemViewModel)((System.Windows.FrameworkElement)(sender)).DataContext).AlarmName;

            // Call Remove to unregister the scheduled action with the service.
            ScheduledActionService.Remove(name);
        }

        private void AdControl_ErrorOccurred(object sender, Microsoft.Advertising.AdErrorEventArgs e)
        {
            pivot.Margin = new Thickness(0);
            System.Diagnostics.Debug.WriteLine(string.Format("{0} - AdControl error: {1}", DateTime.Now, e.Error.Message));
        }

        private void AdControl_AdRefreshed(object sender, EventArgs e)
        {
            pivot.Margin = new Thickness(0, 0, 0, 80);
        }
    }
}
