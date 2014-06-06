// -----------------------------------------------------------------------
// <copyright file="IMessageService.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;

namespace QPAS
{
    public interface IDialogService
    {
        //
        // Summary:
        //     Creates a ProgressDialog inside of the current window.
        //
        // Parameters:
        //   title:
        //     The title of the ProgressDialog.
        //
        //   message:
        //     The message within the ProgressDialog.
        //
        //   isCancelable:
        //     Determines if the cancel button is visible.
        //
        //   settings:
        //     Optional Settings that override the global metro dialog settings.
        //
        // Returns:
        //     A task promising the instance of ProgressDialogController for this operation.
        Task<ProgressDialogController> ShowProgressAsync(string title, string message, bool isCancelable = false, MetroDialogSettings settings = null);
        //
        // Summary:
        //     Creates a MessageDialog inside of the current window.
        //
        // Parameters:
        //   title:
        //     The title of the MessageDialog.
        //
        //   message:
        //     The message contained within the MessageDialog.
        //
        //   style:
        //     The type of buttons to use.
        //
        //   settings:
        //     Optional settings that override the global metro dialog settings.
        //
        // Returns:
        //     A task promising the result of which button was pressed.
        Task<MessageDialogResult> ShowMessageAsync(string title, string message, MessageDialogStyle style = MessageDialogStyle.Affirmative, MetroDialogSettings settings = null);

        bool? OpenFileDialog(string type, out string filePath);
    }
}
