// -----------------------------------------------------------------------
// <copyright file="DialogService.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System.Threading.Tasks;

namespace QPAS
{
    public class DialogService : IDialogService
    {
        private readonly MetroWindow _window;

        public DialogService(MetroWindow window)
        {
            _window = window;
        }

        public bool? OpenFileDialog(string filter, out string filePath)
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = filter;
            bool? result = fd.ShowDialog();
            if (result.HasValue && result.Value)
            {
                filePath = fd.FileName;
            }
            else
            {
                filePath = "";
            }

            return result;
        }

        public Task<MessageDialogResult> ShowMessageAsync(string title, string message, MessageDialogStyle style = MessageDialogStyle.Affirmative,
            MetroDialogSettings settings = null)
        {
            return _window.ShowMessageAsync(title, message, style, settings);
        }

        public Task<ProgressDialogController> ShowProgressAsync(string title, string message, bool isCancelable = false, MetroDialogSettings settings = null)
        {
            return _window.ShowProgressAsync(title, message, isCancelable, settings);
        }
    }
}