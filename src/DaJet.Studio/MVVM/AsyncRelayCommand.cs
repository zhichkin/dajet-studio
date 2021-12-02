using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DaJet.Studio.MVVM
{
    public interface IAsyncCommand : ICommand
    {
        Task ExecuteAsync();
    }
    public sealed class AsyncRelayCommand : IAsyncCommand
    {
        private readonly Func<Task> _command;
        private readonly IErrorHandler _errorHandler;
        public AsyncRelayCommand(Func<Task> command, IErrorHandler errorHandler = null)
        {
            _command = command;
            _errorHandler = errorHandler;
        }
        public async Task ExecuteAsync()
        {
            await _command();
        }
        public async void Execute(object parameter)
        {
            try
            {
                await ExecuteAsync();
            }
            catch (Exception error)
            {
                _errorHandler?.HandleError(error);
            }
        }
        public bool CanExecute(object parameter)
        {
            return true;
        }
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        //protected void RaiseCanExecuteChanged()
        //{
        //    CommandManager.InvalidateRequerySuggested();
        //}
    }



    public interface IAsyncCommand<T> : ICommand
    {
        Task ExecuteAsync(T parameter);
        bool CanExecute(T parameter);
    }
    public class AsyncRelayCommand<T> : IAsyncCommand<T>
    {
        public event EventHandler CanExecuteChanged;

        private bool _isExecuting;
        private readonly Func<T, Task> _execute;
        private readonly Func<T, bool> _canExecute;
        private readonly IErrorHandler _errorHandler;

        public AsyncRelayCommand(Func<T, Task> execute, Func<T, bool> canExecute = null, IErrorHandler errorHandler = null)
        {
            _execute = execute;
            _canExecute = canExecute;
            _errorHandler = errorHandler;
        }
        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute((T)parameter);
        }
        void ICommand.Execute(object parameter)
        {
            ExecuteAsync((T)parameter).FireAndForgetSafeAsync(_errorHandler);
        }
        public bool CanExecute(T parameter)
        {
            return !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);
        }
        public async Task ExecuteAsync(T parameter)
        {
            if (CanExecute(parameter))
            {
                try
                {
                    _isExecuting = true;
                    await _execute(parameter);
                }
                finally
                {
                    _isExecuting = false;
                }
            }

            RaiseCanExecuteChanged();
        }
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}