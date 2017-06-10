// -----------------------------------------------------------------------
// <copyright file="ViewModelBase.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;

namespace QPAS
{
    public class ViewModelBase : ReactiveObject
    {
        protected IDialogCoordinator DialogService;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public virtual async Task Refresh()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
        }

        public ViewModelBase(IDialogCoordinator dialogService = null)
        {
            DialogService = dialogService;
        }
    }
}
