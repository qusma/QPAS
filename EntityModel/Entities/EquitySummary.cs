// -----------------------------------------------------------------------
// <copyright file="EquitySummary.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NXmlMapper;

namespace EntityModel
{
    public class EquitySummary
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [NotXmlMapped]
        public int ID { get; set; }

        [AttributeName("reportDate", "yyyyMMdd")]
        public DateTime Date { get; set; }

        [AttributeName("cash")]
        public decimal Cash { get; set; }

        [AttributeName("cashLong")]
        public decimal CashLong { get; set; }

        [AttributeName("cashShort")]
        public decimal CashShort { get; set; }

        [AttributeName("slbCashCollateral")]
        public decimal SLBCashCollateral { get; set; }

        [AttributeName("slbCashCollateralLong")]
        public decimal SLBCashCollateralLong { get; set; }

        [AttributeName("slbCashCollateralShort")]
        public decimal SLBCashCollateralShort { get; set; }

        [AttributeName("stock")]
        public decimal Stock { get; set; }

        [AttributeName("stockLong")]
        public decimal StockLong { get; set; }

        [AttributeName("stockShort")]
        public decimal StockShort { get; set; }

        [AttributeName("slbDirectSecuritiesBorrowed")]
        public decimal SLBDirectSecuritiesBorrowed { get; set; }

        [AttributeName("slbDirectSecuritiesBorrowedLong")]
        public decimal SLBDirectSecuritiesBorrowedLong { get; set; }

        [AttributeName("slbDirectSecuritiesBorrowedShort")]
        public decimal SLBDirectSecuritiesBorrowedShort { get; set; }

        [AttributeName("slbDirectSecuritiesLent")]
        public decimal SLBDirectSecuritiesLent { get; set; }

        [AttributeName("slbDirectSecuritiesLentLong")]
        public decimal SLBDirectSecuritiesLentLong { get; set; }

        [AttributeName("slbDirectSecuritiesLentShort")]
        public decimal SLBDirectSecuritiesLentShort { get; set; }

        [AttributeName("options")]
        public decimal Options { get; set; }

        [AttributeName("optionsLong")]
        public decimal OptionsLong { get; set; }

        [AttributeName("optionsShort")]
        public decimal OptionsShort { get; set; }

        [AttributeName("commodities")]
        public decimal Commodities { get; set; }

        [AttributeName("commoditiesLong")]
        public decimal CommoditiesLong { get; set; }

        [AttributeName("commoditiesShort")]
        public decimal CommoditiesShort { get; set; }

        [AttributeName("bonds")]
        public decimal Bonds { get; set; }

        [AttributeName("bondsLong")]
        public decimal BondsLong { get; set; }

        [AttributeName("bondsShort")]
        public decimal BondsShort { get; set; }

        [AttributeName("notes")]
        public decimal Notes { get; set; }

        [AttributeName("notesLong")]
        public decimal NotesLong { get; set; }

        [AttributeName("notesShort")]
        public decimal NotesShort { get; set; }

        [AttributeName("interestAccruals")]
        public decimal InterestAccruals { get; set; }

        [AttributeName("interestAccrualsLong")]
        public decimal InterestAccrualsLong { get; set; }

        [AttributeName("interestAccrualsShort")]
        public decimal InterestAccrualsShort { get; set; }

        [AttributeName("softDollars")]
        public decimal SoftDollars { get; set; }

        [AttributeName("softDollarsLong")]
        public decimal SoftDollarsLong { get; set; }

        [AttributeName("softDollarsShort")]
        public decimal SoftDollarsShort { get; set; }

        [AttributeName("dividendAccruals")]
        public decimal DividendAccruals { get; set; }

        [AttributeName("dividendAccrualsLong")]
        public decimal DividendAccrualsLong { get; set; }

        [AttributeName("dividendAccrualsShort")]
        public decimal DividendAccrualsShort { get; set; }

        [AttributeName("total")]
        public decimal Total { get; set; }

        [AttributeName("totalLong")]
        public decimal TotalLong { get; set; }

        [AttributeName("totalShort")]
        public decimal TotalShort { get; set; }

        /// <summary>
        /// Used for charting.
        /// </summary>
        [NotMapped]
        [NotXmlMapped]
        public double Zero { get { return 0; } }

        public int? AccountID { get; set; }

        public virtual Account Account { get; set; }
    }
}