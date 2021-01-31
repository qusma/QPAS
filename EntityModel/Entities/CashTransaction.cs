// -----------------------------------------------------------------------
// <copyright file="CashTransaction.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using NXmlMapper;

namespace EntityModel
{
    public class CashTransaction : INotifyPropertyChanged
    {
        private Trade _trade;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [NotXmlMapped]
        public int ID { get; set; }

        [NotXmlMapped]
        public int CurrencyID { get; set; }

        [NotXmlMapped]
        public virtual Currency Currency { get; set; }

        [AttributeName("currency")]
        [NotMapped]
        public string CurrencyString { get; set; }

        [NotXmlMapped]
        public int? TradeID { get; set; }

        [NotXmlMapped]
        public virtual Trade Trade
        {
            get { return _trade; }
            set { _trade = value; OnPropertyChanged(); }
        }

        [NotXmlMapped]
        public AssetClass AssetCategory { get; set; }

        [AttributeName("assetCategory")]
        [NotMapped]
        public string SetAssetClass
        {
            set
            {
                AssetCategory = Utils.GetValueFromDescription<AssetClass>(value);
            }
        }

        [AttributeName("fxRateToBase")]
        public decimal FXRateToBase { get; set; }

        [NotXmlMapped]
        public int? InstrumentID { get; set; }

        [NotXmlMapped]
        public virtual Instrument Instrument { get; set; }

        [AttributeName("conid")]
        public long? ConID { get; set; }

        [AttributeName("dateTime", "yyyyMMdd")]
        public DateTime TransactionDate { get; set; }

        [AttributeName("amount")]
        public decimal Amount { get; set; }

        [NotMapped]
        [NotXmlMapped]
        public decimal AmountInBase
        {
            get
            {
                return Amount * FXRateToBase;
            }
        }

        [AttributeName("type")]
        [MaxLength(255)]
        public string Type { get; set; }

        [AttributeName("description")]
        [MaxLength(255)]
        public string Description { get; set; }

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