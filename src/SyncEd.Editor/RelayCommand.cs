using System;
using System.Windows.Input;

namespace SyncEd.Editor
{
    public class RelayCommand
        : ICommand
    {
        private readonly Action<object> executeAction;
        private readonly Predicate<object> canExecutePredicate;

        public RelayCommand(Action<object> execute)
            : this(execute, null)
        {
        }

        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");

            executeAction = execute;
            canExecutePredicate = canExecute;
        }

        public void Execute(object parameter)
        {
            executeAction(parameter);
        }


        public bool CanExecute(object parameter)
        {
            return canExecutePredicate == null || canExecutePredicate(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}