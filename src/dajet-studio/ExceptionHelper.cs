using System;
using System.Windows;
using System.Collections.Generic;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace DaJet.Studio
{
    public static class ExceptionHelper
    {
        private const string DAJET_WINDOW_CAPTION = "DaJet";
        public static string GetErrorText(Exception ex)
        {
            string errorText = string.Empty;
            Exception error = ex;
            while (error != null)
            {
                errorText += (errorText == string.Empty) ? error.Message : Environment.NewLine + error.Message;
                error = error.InnerException;
            }
            return errorText;
        }
        public static string GetErrorTextAndStackTrace(Exception ex)
        {
            string errorText = string.Empty;

            string stackTrace = string.IsNullOrEmpty(ex.StackTrace)
                ? string.Empty
                : ex.StackTrace;

            Exception error = ex;
            while (error != null)
            {
                errorText += (errorText == string.Empty) ? error.Message : Environment.NewLine + error.Message;
                error = error.InnerException;
            }
            return errorText + Environment.NewLine + stackTrace;
        }
        public static string GetParseErrorsText(IList<ParseError> errors)
        {
            string errorMessage = string.Empty;
            foreach (ParseError error in errors)
            {
                errorMessage += error.Message + Environment.NewLine;
            }
            return errorMessage;
        }
        public static void ShowException(Exception ex)
        {
            _ = MessageBox.Show(GetErrorText(ex), DAJET_WINDOW_CAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}