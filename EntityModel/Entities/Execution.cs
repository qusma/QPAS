// -----------------------------------------------------------------------
// <copyright file="Execution.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NXmlMapper;

namespace EntityModel
{
    public class Execution
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [NotXmlMapped]
        public int ID { get; set; }

        [AttributeName("conid")]
        public long ConID { get; set; }

        [NotXmlMapped]
        public int InstrumentID { get; set; }

        [NotXmlMapped]
        public virtual Instrument Instrument { get; set; }

        /// <summary>
        /// Used for xml parsing.
        /// </summary>
        [NotMapped]
        [AttributeName("symbol")]
        public string SymbolString { get; set; }

        [AttributeName("exchange")]
        [MaxLength(50)]
        public string Exchange { get; set; }

        [NotXmlMapped]
        public DateTime TradeDate { get; set; }

        [NotMapped]
        [AttributeName("tradeDate", "yyyyMMdd")]
        public DateTime SetDate
        {
            set
            {
                TradeDate = new DateTime(value.Year, value.Month, value.Day, TradeDate.Hour, TradeDate.Minute, TradeDate.Second);
            }
        }

        [NotMapped]
        [AttributeName("tradeTime", "HHmmss")]
        public DateTime SetTime
        {
            set
            {
                TradeDate = new DateTime(TradeDate.Year, TradeDate.Month, TradeDate.Day, value.Hour, value.Minute, value.Second);
            }
        }

        [AttributeName("orderTime", "yyyyMMdd;HHmmss")]
        public DateTime? OrderPlacementTime { get; set; }

        [AttributeName("quantity")]
        public int Quantity { get; set; }

        [AttributeName("tradePrice")]
        public decimal Price { get; set; }

        [AttributeName("ibCommission")]
        public decimal Commission { get; set; }

        [NotMapped]
        [NotXmlMapped]
        public decimal CommissionInBase
        {
            get
            {
                return Commission * FXRateToBase;
            }
        }

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
        public int CommissionCurrencyID { get; set; }

        [NotXmlMapped]
        public virtual Currency CommissionCurrency { get; set; }

        /// <summary>
        /// Used for xml parsing.
        /// </summary>
        [NotMapped]
        [AttributeName("ibCommissionCurrency")]
        public string CommissionCurrencyString { get; set; }

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

        [AttributeName("multiplier")]
        public int Multiplier { get; set; }

        /// <summary>
        /// Trade money is calculated by multiplying the trade price and quantity.
        /// </summary>
        [AttributeName("tradeMoney")]
        public decimal TradeMoney { get; set; }

        /// <summary>
        /// Calculated by mulitplying the quantity and the transaction price. The proceeds figure will be negative for buys and positive for sales.
        /// </summary>
        [AttributeName("proceeds")]
        public decimal Proceeds { get; set; }

        /// <summary>
        /// The total amount of tax for the transaction.
        /// </summary>
        [AttributeName("taxes")]
        public decimal Taxes { get; set; }

        /// <summary>
        /// The closing price of the contract traded.
        /// </summary>
        [AttributeName("closePrice")]
        public decimal ClosePrice { get; set; }

        /// <summary>
        /// The indicator denotes if the trade is an opening or closing trade.
        /// </summary>
        [AttributeName("openCloseIndicator")]
        [MaxLength(10)]
        public string OpenClose { get; set; }

        /// <summary>
        /// The note/code abbreviation.
        /// </summary>
        [AttributeName("notes")]
        [MaxLength(50)]
        public string Notes { get; set; }

        /// <summary>
        /// The basis of an opening trade is the inverse of proceeds plus commission and tax amount.
        /// For closing trades, the basis is the basis of the opening trade.
        /// </summary>
        [AttributeName("cost")]
        public decimal CostBasis { get; set; }

        /// <summary>
        /// Realized P/L can be calculated by the proceeds of the closing trade plus commissions and then adding the basis.
        /// </summary>
        [AttributeName("fifoPnlRealized")]
        public decimal FIFORealizedPnL { get; set; }

        /// <summary>
        /// The difference between the transaction price and closing price multiplied by the quantity.
        /// </summary>
        [AttributeName("mtmPnl")]
        public decimal MTMPnL { get; set; }

        [AttributeName("putCall")]
        [MaxLength(10)]
        public string OptionType { get; set; }

        [AttributeName("buySell")]
        [MaxLength(10)]
        public string BuySell { get; set; }

        /// <summary>
        /// Net cash is calculated by subtracting the commissions and taxes from trade money.
        /// </summary>
        [AttributeName("netCash")]
        public decimal NetCash { get; set; }

        [AttributeName("orderType")]
        [MaxLength(10)]
        public string OrderType { get; set; }

        [AttributeName("tradeID")]
        [MaxLength(100)]
        public string IBTradeID { get; set; }

        [AttributeName("ibExecID")]
        [MaxLength(100)]
        public string IBExecID { get; set; }

        [AttributeName("brokerageOrderID")]
        [MaxLength(100)]
        public string BrokerageOrderID { get; set; }

        [AttributeName("ibOrderID")]
        public long IBOrderID { get; set; }

        /// <summary>
        /// The order reference number as defined by the user on the order ticket. 
        /// Available on daily (single-day) activity flex queries only.
        /// </summary>
        [MaxLength(255)]
        [AttributeName("orderReference")]
        public string OrderReference { get; set; }

        [NotXmlMapped]
        public int OrderID { get; set; }

        public Order Order { get; set; }

        public int? AccountID { get; set; }

        public virtual Account Account { get; set; }
    }
}