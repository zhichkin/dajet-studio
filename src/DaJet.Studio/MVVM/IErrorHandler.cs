using System;

namespace DaJet.Studio.MVVM
{
    public interface IErrorHandler
    {
        void HandleError(Exception error);
    }
}