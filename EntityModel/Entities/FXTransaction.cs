// -----------------------------------------------------------------------
// <copyright file="FXTransaction.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using NXmlMapper;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace EntityModel
{
    public class FXTransaction : INotifyPropertyChanged
    {
        private Trade _trade;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [NotXmlMapped]
        public int ID { get; set; }

        [NotXmlMapped]
        public int FunctionalCurrencyID { get; set; }

        /// <summary>
        /// The functional currency of your account.
        /// </summary>
        [NotXmlMapped]
        public virtual Currency FunctionalCurrency { get; set; }

        /// <summary>
        /// Used for xml parsing.
        /// </summary>
        [AttributeName("functionalCurrency")]
        [NotMapped]
        public string FunctionalCurrencyString { get; set; }

        [NotXmlMapped]
        public int FXCurrencyID { get; set; }

        /// <summary>
        /// The non-functional currency involved in the activity.
        /// </summary>
        [NotXmlMapped]
        public virtual Currency FXCurrency { get; set; }

        /// <summary>
        /// Used for xml parsing.
        /// </summary>
        [AttributeName("fxCurrency")]
        [NotMapped]
        public string FXCurrencyString { get; set; }

        /// <summary>
        /// The description of the activity.
        /// </summary>
        [AttributeName("activityDescription")]
        [MaxLength(255)]
        public string Description { get; set; }

        [AttributeName("dateTime", "yyyyMMdd;HHmmss")]
        public DateTime DateTime { get; set; }

        /// <summary>
        /// The number of units in the activity. When gaining currency, quantity is positive and when losing currency, quantity is negative.
        /// </summary>
        [AttributeName("quantity")]
        public decimal Quantity { get; set; }

        [NotXmlMapped]
        public int? TradeID { get; set; }

        [NotXmlMapped]
        public virtual Trade Trade
        {
            get { return _trade; }
            set { _trade = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// The proceeds in your functional currency resulting from the activity. For closed lots, this is the proceeds of closing against the cost of opening. For transactions, proceeds are as follows:
        ///
        ///  For spot trades, the amount is the value of the nonfunctional currency expressed in your functional currency using the spot rate on the trade date.
        ///  For securities trades, the amount is the value of the nonfunctional currency expressed in your functional currency using the spot rate on the trade date.
        ///  For interest, dividends or deposits, the amount is the spot rate on the day of the transaction.
        ///
        ///  When gaining currency, proceeds are positive and when losing currency, proceeds are negative. Proceeds equals quantity * the conversion rate from non-functional currency to functional currency for the report date of the transaction.
        /// </summary>
        [AttributeName("proceeds")]
        public decimal Proceeds { get; set; }

        /// <summary>
        /// The inverse of the proceeds (proceeds negated).
        /// </summary>
        [AttributeName("cost")]
        public decimal Cost { get; set; }

        [AttributeName("code")]
        [MaxLength(255)]
        public string Code { get; set; }

        public int? AccountID { get; set; }

        public virtual Account Account { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}