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

namespace RecipeHubApp
{
    public class CommandBase : ICommand
    {
        private Action executeMethod;

        public CommandBase(Action methodToExecute)
        {
            executeMethod = methodToExecute;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            executeMethod.Invoke();
        }

    }

    public class CommandBase<T> : ICommand
    {
        private Action<T> executeMethod;

        public CommandBase(Action<T> methodToExecute)
        {
            executeMethod = methodToExecute;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            executeMethod.Invoke((T)parameter);
        }

    }
}
