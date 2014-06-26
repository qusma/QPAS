// -----------------------------------------------------------------------
// <copyright file="OpenPosition.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NXmlMapper;

namespace EntityModel
{
    public class OpenPosition
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
        [AttributeName("currency")]
        [NotMapped]
        public string CurrencyString { get; set; }

        [NotXmlMapped]
        public int InstrumentID { get; set; }

        [NotXmlMapped]
        public Instrument Instrument { get; set; }

        /// <summary>
        /// Used for xml parsing.
        /// </summary>
        [AttributeName("symbol")]
        [NotMapped]
        public string InstrumentSymbol { get; set; }

        [AttributeName("fxRateToBase")]
        public decimal FXRateToBase { get; set; }

        [AttributeName("position")]
        public int Quantity { get; set; }

        [AttributeName("multiplier")]
        public int Multiplier { get; set; }

        [AttributeName("markPrice")]
        public decimal MarkPrice { get; set; }

        [AttributeName("positionValue")]
        public decimal PositionValue { get; set; }

        [AttributeName("openPrice")]
        public decimal OpenPrice { get; set; }

        [AttributeName("costBasisPrice")]
        public decimal CostBasisPrice { get; set; }

        [AttributeName("costBasisMoney")]
        public decimal CostBasisDollars { get; set; }

        [AttributeName("percentOfNAV")]
        public double PercentOfNAV { get; set; }

        [AttributeName("fifoPnlUnrealized")]
        public decimal UnrealizedPnL { get; set; }

        [AttributeName("side")]
        [MaxLength(10)]
        public string Side { get; set; }

        [AttributeName("conid")]
        public long ConID { get; set; }

        public int? AccountID { get; set; }

        public virtual Account Account { get; set; }
    }
}