// -----------------------------------------------------------------------
// <copyright file="CheckBoxItem.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel;

namespace QPAS
{
    public class CheckListItem<T> : INotifyPropertyChanged
    {
        public object Tag { get; set; }

        private bool _isChecked;
        private T _item;

        public CheckListItem(T item, bool isChecked = false)
        {
            _item = item;
            _isChecked = isChecked;
        }

        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                _isChecked = value;
                NotifyPropertyChanged("IsChecked");
            }
        }

        public T Item
        {
            get { return _item; }
            set
            {
                _item = value;
                NotifyPropertyChanged("Item");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
