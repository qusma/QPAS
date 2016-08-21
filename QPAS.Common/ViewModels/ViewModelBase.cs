// -----------------------------------------------------------------------
// <copyright file="ViewModelBase.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel;
using System.Runtime.CompilerServices;
using MahApps.Metro.Controls.Dialogs;

namespace QPAS
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        protected IDialogCoordinator DialogService;

        public virtual void Refresh()
        {
        }

        public ViewModelBase(IDialogCoordinator dialogService = null)
        {
            DialogService = dialogService;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
