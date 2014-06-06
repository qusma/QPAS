// -----------------------------------------------------------------------
// <copyright file="BenchmarkComponent.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace EntityModel
{
    public class BenchmarkComponent : INotifyPropertyChanged
    {
        private int _id;
        private double _weight;
        private int _qdmsInstrumentID;
        private int _benchmarkID;
        private Benchmark _benchmark;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID
        {
            get { return _id; }
            set { _id = value; OnPropertyChanged(); }
        }

        public double Weight
        {
            get { return _weight; }
            set { _weight = value; OnPropertyChanged(); }
        }

        public int QDMSInstrumentID
        {
            get { return _qdmsInstrumentID; }
            set { _qdmsInstrumentID = value; OnPropertyChanged(); }
        }

        public int BenchmarkID
        {
            get { return _benchmarkID; }
            set { _benchmarkID = value; OnPropertyChanged(); }
        }

        public virtual Benchmark Benchmark
        {
            get { return _benchmark; }
            set { _benchmark = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}