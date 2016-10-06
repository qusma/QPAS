// -----------------------------------------------------------------------
// <copyright file="DividendAccrual.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NXmlMapper;

namespace EntityModel
{
    public class DividendAccrual
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [NotXmlMapped]
        public int ID { get; set; }

        [NotXmlMapped]
        public int CurrencyID { get; set; }

        [NotXmlMapped]
        public virtual Currency Currency { get; set; }

        /// <summary>
        /// Used for xml parsing.
        /// </summary>
        [NotMapped]
        [AttributeName("currency")]
        public string CurrencyString { get; set; }

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

        public int InstrumentID { get; set; }

        public virtual Instrument Instrument { get; set; }

        /// <summary>
        /// Used for xml parsing.
        /// </summary>
        [NotMapped]
        [AttributeName("symbol")]
        public string SymbolString { get; set; }

        [AttributeName("conid")]
        public long ConID { get; set; }

        [AttributeName("exDate", "yyyyMMdd")]
        public DateTime ExDate { get; set; }

        [AttributeName("payDate", "yyyyMMdd")]
        public DateTime? PayDate { get; set; }

        [AttributeName("quantity")]
        public int Quantity { get; set; }

        [AttributeName("tax")]
        public decimal Tax { get; set; }

        [AttributeName("grossRate")]
        public decimal GrossRate { get; set; }

        [AttributeName("grossAmount")]
        public decimal GrossAmount { get; set; }

        [AttributeName("netAmount")]
        public decimal NetAmount { get; set; }

        [AttributeName("code")]
        [MaxLength(100)]
        public string Code { get; set; }

        public int? AccountID { get; set; }

        public virtual Account Account { get; set; }
    }
}