// -----------------------------------------------------------------------
// <copyright file="ViewModelBase.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel;
using System.Runtime.CompilerServices;
using QPAS.Annotations;

namespace QPAS
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        protected IDialogService DialogService;

        public virtual void Refresh()
        {
        }

        public ViewModelBase(IDialogService dialogService = null)
        {
            DialogService = dialogService;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
