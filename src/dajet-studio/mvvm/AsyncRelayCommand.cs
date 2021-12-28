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
        bool CanExecute(T parameter);
        Task ExecuteAsync(T parameter);
    }
    public class AsyncRelayCommand<T> : IAsyncCommand<T>
    {
        private readonly Func<T, Task> _command;
        private readonly IErrorHandler _errorHandler;
        public AsyncRelayCommand(Func<T, Task> command, IErrorHandler errorHandler = null)
        {
            _command = command;
            _errorHandler = errorHandler;
        }
        public async Task ExecuteAsync(T parameter)
        {
            await _command(parameter);
        }
        public async void Execute(object parameter)
        {
            try
            {
                await ExecuteAsync((T)parameter);
            }
            catch (Exception error)
            {
                _errorHandler?.HandleError(error);
            }
        }
        public bool CanExecute(T parameter)
        {
            return true;
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
    }
}