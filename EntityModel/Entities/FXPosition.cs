// -----------------------------------------------------------------------
// <copyright file="FXPosition.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NXmlMapper;

namespace EntityModel
{
    public class FXPosition
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [NotXmlMapped]
        public int ID { get; set; }

        [NotXmlMapped]
        public int FunctionalCurrencyID { get; set; }

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

        [NotXmlMapped]
        public virtual Currency FXCurrency { get; set; }

        /// <summary>
        /// Used for xml parsing.
        /// </summary>
        [AttributeName("fxCurrency")]
        [NotMapped]
        public string FXCurrencyString { get; set; }

        [AttributeName("quantity")]
        public decimal Quantity { get; set; }

        [AttributeName("costPrice")]
        public decimal CostPrice { get; set; }

        [AttributeName("costBasis")]
        public decimal CostBasis { get; set; }

        [AttributeName("closePrice")]
        public decimal ClosePrice { get; set; }

        [AttributeName("value")]
        public decimal Value { get; set; }

        [AttributeName("unrealizedPL")]
        public decimal UnrealizedPnL { get; set; }

        public int? AccountID { get; set; }

        public virtual Account Account { get; set; }
    }
}