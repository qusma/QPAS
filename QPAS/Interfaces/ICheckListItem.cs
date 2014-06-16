// -----------------------------------------------------------------------
// <copyright file="ICheckListItem.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel;

namespace QPAS
{
    public interface ICheckListItem : INotifyPropertyChanged
    {
        object Tag { get; set; }
        bool IsChecked { get; set; }
    }

    public interface ICheckListItem<T>
    {
        T Item { get; set; }
    }
}
