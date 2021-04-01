// -----------------------------------------------------------------------
// <copyright file="PriorPosition.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using NXmlMapper;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityModel
{
    public class PriorPosition
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [NotXmlMapped]
        public int ID { get; set; }

        [AttributeName("date", "yyyyMMdd")]
        public DateTime Date { get; set; }

        [NotXmlMapped]
        public int CurrencyID { get; set; }

        [NotXmlMapped]
        public virtual Currency Currency { get; set; }

        /// <summary>
        /// Used for xml parsing
        /// </summary>
        [NotMapped]
        [AttributeName("currency")]
        public string CurrencyString { get; set; }

        [AttributeName("fxRateToBase")]
        public decimal FXRateToBase { get; set; }

        [NotXmlMapped]
        public int InstrumentID { get; set; }

        [NotXmlMapped]
        public virtual Instrument Instrument { get; set; }

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

        [AttributeName("conid")]
        public long ConID { get; set; }

        [AttributeName("underlyingSymbol")]
        [MaxLength(100)]
        public string UnderlyingSymbol { get; set; }

        [AttributeName("underlyingConid")]
        public long? UnderlyingConID { get; set; }

        [AttributeName("price")]
        public decimal Price { get; set; }

        [NotMapped]
        [NotXmlMapped]
        public decimal PriceInBaseCurrency
        {
            get
            {
                return Price * FXRateToBase;
            }
        }

        [AttributeName("priorMtmPnl")]
        public decimal PriorMTMPnL { get; set; }

        public int? AccountID { get; set; }

        public virtual Account Account { get; set; }
    }
}