// -----------------------------------------------------------------------
// <copyright file="TradeFilterSettings.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using EntityModel;

namespace QPAS
{
    public class TradeFilterSettings : INotifyPropertyChanged
    {
        private DateTime? _from;
        private DateTime? _to;
        private FilterMethod _tagFilterMethod;
        private FilterMethod _strategyFilterMethod;
        private FilterMethod _instrumentFilterMethod;

        public DateTime? From
        {
            get { return _from; }
            set { _from = value; OnPropertyChanged(); }
        }

        public DateTime? To
        {
            get { return _to; }
            set { _to = value; OnPropertyChanged(); }
        }

        public List<Tag> Tags { get; set; }

        public List<Strategy> Strategies { get; set; }

        public List<Instrument> Instruments { get; set; }

        public FilterMethod TagFilterMethod
        {
            get { return _tagFilterMethod; }
            set { _tagFilterMethod = value; OnPropertyChanged(); }
        }

        public FilterMethod StrategyFilterMethod
        {
            get { return _strategyFilterMethod; }
            set { _strategyFilterMethod = value; OnPropertyChanged(); }
        }

        public FilterMethod InstrumentFilterMethod
        {
            get { return _instrumentFilterMethod; }
            set { _instrumentFilterMethod = value; OnPropertyChanged(); }
        }

        public bool ClosedTradesOnly { get; set; }

        public TradeFilterSettings(IDBContext context)
        {
            //set dates
            var firstES = context.EquitySummaries.OrderBy(x => x.Date).FirstOrDefault();
            From = firstES == null ? new DateTime(1, 1, 1) : firstES.Date;

            var lastES = context.EquitySummaries.OrderByDescending(x => x.Date).FirstOrDefault();
            To = lastES == null ? DateTime.Now : lastES.Date;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}