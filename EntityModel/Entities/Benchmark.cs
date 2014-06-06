// -----------------------------------------------------------------------
// <copyright file="Benchmark.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace EntityModel
{
    [Serializable]
    public class Benchmark : INotifyPropertyChanged
    {
        private int _id;
        private string _name;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID
        {
            get { return _id; }
            set { _id = value; OnPropertyChanged(); }
        }

        [MaxLength(255)]
        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged(); }
        }

        public virtual ObservableCollection<BenchmarkComponent> Components { get; set; }

        public Benchmark()
        {
            Components = new ObservableCollection<BenchmarkComponent>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}