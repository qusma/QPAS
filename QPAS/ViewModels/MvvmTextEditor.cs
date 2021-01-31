// -----------------------------------------------------------------------
// <copyright file="MvvmTextEditor.cs" company="">
// Copyright 2015 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Windows;

namespace QPAS
{
    public class MvvmTextEditor : ICSharpCode.AvalonEdit.TextEditor, INotifyPropertyChanged
    {
        public new string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        internal string baseText { get { return base.Text; } set { base.Text = value; } }

        public static DependencyProperty TextProperty =
        DependencyProperty.Register("Text", typeof(string), typeof(MvvmTextEditor),
            // binding changed callback: set value of underlying property
            new PropertyMetadata((obj, args) =>
            {
                MvvmTextEditor target = (MvvmTextEditor)obj;
                if (target.baseText != (string)args.NewValue)
                    target.baseText = (string)args.NewValue;
            })
        );

        protected override void OnTextChanged(EventArgs e)
        {
            SetCurrentValue(TextProperty, baseText);
            RaisePropertyChanged("Text");
            base.OnTextChanged(e);
        }

        /// <summary>
        /// Raises a property changed event
        /// </summary>
        /// <param name="property">The name of the property that updates</param>
        public void RaisePropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}