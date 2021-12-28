using System;
using System.Threading.Tasks;

namespace DaJet.Studio.MVVM
{
    public static class TaskExtensions
    {
        public static async void FireAndForgetSafeAsync(this Task task, IErrorHandler handler = null)
        {
            try
            {
                await task;
            }
            catch (Exception error)
            {
                handler?.HandleError(error);
            }
        }
    }
}