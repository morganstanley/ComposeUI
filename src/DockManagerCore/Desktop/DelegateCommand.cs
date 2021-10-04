using System;
using System.Windows.Input;

namespace DockManagerCore.Desktop
{

    public class DelegateCommand : ICommand
    {
        private readonly Predicate<object> canExecute;
        private readonly Action<object> execute;
        event EventHandler ICommand.CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public DelegateCommand(Action<object> execute_, Predicate<object> canExecute_)
        {
            execute = execute_;
            canExecute = canExecute_;
        }
        public DelegateCommand(Action<object> execute_)
            : this(execute_, null)
        {
        }
        bool ICommand.CanExecute(object parameter_)
        {
            return canExecute == null || canExecute(parameter_);
        }
        void ICommand.Execute(object parameter_)
        {
            execute(parameter_);
        }
    }
}
