// -----------------------------------------------------------------------
// <copyright file="Trade.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;

namespace EntityModel
{
    public class Trade : INotifyPropertyChanged
    {
        private decimal _resultDollars;
        private decimal _unrealizedResultDollars;
        private decimal _resultDollarsLong;
        private decimal _resultDollarsShort;
        private double _resultPctLong;
        private double _resultPctShort;
        private decimal _unrealizedResultDollarsLong;
        private decimal _unrealizedResultDollarsShort;
        private double _unrealizedResultPctLong;
        private double _unrealizedResultPctShort;
        private decimal _capitalLong;
        private decimal _capitalShort;
        private decimal _capitalTotal;
        private decimal _capitalNet;
        private double _unrealizedResultPct;
        private decimal _commissions;
        private double _resultPct;
        private DateTime _dateOpened;
        private DateTime? _dateClosed;
        private bool _open;
        private Strategy _strategy;
        private int _id;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID
        {
            get { return _id; }
            set { _id = value; OnPropertyChanged(); }
        }

        public int? StrategyID { get; set; }

        public virtual Strategy Strategy
        {
            get { return _strategy; }
            set { _strategy = value; OnPropertyChanged(); }
        }

        [MaxLength(255)]
        public string Name { get; set; }

        public bool Open
        {
            get { return _open; }
            set { _open = value; OnPropertyChanged(); }
        }

        [Index]
        public DateTime DateOpened
        {
            get { return _dateOpened; }
            set { _dateOpened = value; OnPropertyChanged(); OnPropertyChanged("Length"); }
        }

        public DateTime? DateClosed
        {
            get { return _dateClosed; }
            set { _dateClosed = value; OnPropertyChanged(); OnPropertyChanged("Length"); }
        }

        [NotMapped]
        public TimeSpan Length
        {
            get
            {
                if (DateClosed.HasValue) 
                    return DateClosed.Value - DateOpened;
                else 
                    return DateTime.Now - DateOpened;
            }
        }

        /// <summary>
        /// Realized % return
        /// </summary>
        public double ResultPct
        {
            get { return _resultPct; }
            set { _resultPct = value; OnPropertyChanged(); OnPropertyChanged("TotalResultPct"); }
        }

        /// <summary>
        /// Realized profit/loss
        /// </summary>
        public decimal ResultDollars
        {
            get { return _resultDollars; }
            set { _resultDollars = value; OnPropertyChanged(); OnPropertyChanged("TotalResultDollars"); }
        }

        /// <summary>
        /// Realized and unrealized dollar profit/loss.
        /// </summary>
        [NotMapped]
        public decimal TotalResultDollars
        {
            get
            {
                return ResultDollars + UnrealizedResultDollars;
            }
        }

        /// <summary>
        /// Realized and unrealized percent returns.
        /// </summary>
        [NotMapped]
        public double TotalResultPct
        {
            get
            {
                return ResultPct + UnrealizedResultPct;
            }
        }

        public decimal Commissions
        {
            get { return _commissions; }
            set { _commissions = value; OnPropertyChanged(); }
        }

        public double UnrealizedResultPct
        {
            get { return _unrealizedResultPct; }
            set { _unrealizedResultPct = value; OnPropertyChanged(); OnPropertyChanged("TotalResultPct"); }
        }

        public decimal UnrealizedResultDollars
        {
            get { return _unrealizedResultDollars; }
            set { _unrealizedResultDollars = value; OnPropertyChanged(); OnPropertyChanged("TotalResultDollars"); }
        }

        public decimal ResultDollarsLong
        {
            get { return _resultDollarsLong; }
            set { _resultDollarsLong = value; OnPropertyChanged(); }
        }

        public decimal ResultDollarsShort
        {
            get { return _resultDollarsShort; }
            set { _resultDollarsShort = value; OnPropertyChanged(); }
        }

        public double ResultPctLong
        {
            get { return _resultPctLong; }
            set { _resultPctLong = value; OnPropertyChanged(); }
        }

        public double ResultPctShort
        {
            get { return _resultPctShort; }
            set { _resultPctShort = value; OnPropertyChanged(); }
        }

        public decimal UnrealizedResultDollarsLong
        {
            get { return _unrealizedResultDollarsLong; }
            set { _unrealizedResultDollarsLong = value; OnPropertyChanged(); }
        }

        public decimal UnrealizedResultDollarsShort
        {
            get { return _unrealizedResultDollarsShort; }
            set { _unrealizedResultDollarsShort = value; OnPropertyChanged(); }
        }

        public double UnrealizedResultPctLong
        {
            get { return _unrealizedResultPctLong; }
            set { _unrealizedResultPctLong = value; OnPropertyChanged(); }
        }

        public double UnrealizedResultPctShort
        {
            get { return _unrealizedResultPctShort; }
            set { _unrealizedResultPctShort = value; OnPropertyChanged(); }
        }

        public decimal CapitalLong
        {
            get { return _capitalLong; }
            set { _capitalLong = value; OnPropertyChanged(); }
        }

        public decimal CapitalShort
        {
            get { return _capitalShort; }
            set { _capitalShort = value; OnPropertyChanged(); }
        }

        public decimal CapitalTotal
        {
            get { return _capitalTotal; }
            set { _capitalTotal = value; OnPropertyChanged(); }
        }

        public decimal CapitalNet
        {
            get { return _capitalNet; }
            set { _capitalNet = value; OnPropertyChanged(); }
        }

        public virtual ICollection<Tag> Tags { get; set; }

        [MaxLength(65535)]
        [Column(TypeName = "TEXT")]
        public string Notes { get; set; }

        [NotMapped]
        public string TagString
        {
            get
            {
                if (Tags != null)
                {
                    return string.Join(", ", Tags.Select(x => x.Name));
                }
                else
                {
                    return string.Empty;
                }
            }
            
            //Empty set is needed so this can be edited in the datagrid
            set
            {
            }
        }

        public void TagStringUpdated()
        {
            OnPropertyChanged("TagString");
        }

        public ICollection<Order> Orders { get; set; }

        public ICollection<CashTransaction> CashTransactions { get; set; }

        public ICollection<FXTransaction> FXTransactions { get; set; }

        public override string ToString()
        {
            return string.Format("{0} {1} ({2}) {3}",
                ID,
                Name,
                Strategy == null ? "" : Strategy.Name,
                Open ? "Open" : "Closed");
        }

        /// <summary>
        /// Returns true if there are no open positions.
        /// </summary>
        /// <returns></returns>
        public bool IsClosable()
        {
            if (Orders.Count == 0)
            {
                return true;
            }
            return Orders.GroupBy(x => x.ConID).Select(x => x.Sum(y => y.Quantity)).All(x => x == 0);
            //This will fail in case of splits, not sure how to work around that...

            //Also note currency positions don't affect closability, 
            //this is because sometimes they are impossible to close completely.
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}