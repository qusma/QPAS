// -----------------------------------------------------------------------
// <copyright file="Order.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using NXmlMapper;

namespace EntityModel
{
    [Serializable]
    public class Order : ICloneable, INotifyPropertyChanged
    {
        private Trade _trade;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [NotXmlMapped]
        public int ID { get; set; }

        /// <summary>
        /// The conid of the contract traded.
        /// </summary>
        [AttributeName("conid")]
        public long ConID { get; set; }

        [NotXmlMapped]
        public int? TradeID { get; set; }

        [NotXmlMapped]
        public virtual Trade Trade
        {
            get { return _trade; }
            set { _trade = value; OnPropertyChanged(); }
        }

        [NotXmlMapped]
        public int InstrumentID { get; set; }

        [NotXmlMapped]
        public virtual Instrument Instrument { get; set; }

        /// <summary>
        /// Used for xml parsing. Ignore otherwise.
        /// </summary>
        [NotMapped]
        [AttributeName("symbol")]
        public string SymbolString { get; set; }

        [NotXmlMapped]
        public DateTime TradeDate { get; set; }

        /// <summary>
        /// Used to bypass retarded grid stuff. Ignore otherwise
        /// </summary>
        [NotXmlMapped]
        [NotMapped]
        public TimeSpan TradeTime
        {
            get
            {
                return TradeDate.TimeOfDay;
            }
        }


        /// <summary>
        /// Used for parsing
        /// </summary>
        [NotMapped]
        [AttributeName("tradeDate", "yyyyMMdd")]
        public DateTime SetDate
        {
            set
            {
                TradeDate = new DateTime(value.Year, value.Month, value.Day, TradeDate.Hour, TradeDate.Minute, TradeDate.Second);
            }
        }

        /// <summary>
        /// Used for parsing
        /// </summary>
        [NotMapped]
        [AttributeName("tradeTime", "HHmmss")]
        public DateTime SetTime
        {
            set
            {
                TradeDate = new DateTime(TradeDate.Year, TradeDate.Month, TradeDate.Day, value.Hour, value.Minute, value.Second);
            }
        }

        /// <summary>
        /// The time at which the order was submitted.
        /// </summary>
        [AttributeName("orderTime", "yyyyMMdd;HHmmss")]
        public DateTime? OrderPlacementTime { get; set; }

        /// <summary>
        /// The number of units for the transaction.
        /// </summary>
        [AttributeName("quantity")]
        public int Quantity { get; set; }

        /// <summary>
        /// The transaction price.
        /// </summary>
        [AttributeName("tradePrice")]
        public decimal Price { get; set; }

        /// <summary>
        /// The total amount of commission for the transaction.
        /// </summary>
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

        /// <summary>
        /// The currency denomination of the trade.
        /// </summary>
        [NotXmlMapped]
        public virtual Currency CommissionCurrency { get; set; }

        /// <summary>
        /// Used for xml parsing.
        /// </summary>
        [NotMapped]
        [AttributeName("ibCommissionCurrency")]
        public string CommissionCurrencyString { get; set; }

        /// <summary>
        /// The asset class of the contract traded.
        /// </summary>
        [NotXmlMapped]
        public AssetClass AssetCategory { get; set; }

        /// <summary>
        /// Used for xml parsing. Ignore otherwise.
        /// </summary>
        [AttributeName("assetCategory")]
        [NotMapped]
        public string SetAssetClass
        {
            set
            {
                AssetCategory = Utils.GetValueFromDescription<AssetClass>(value);
            }
        }

        /// <summary>
        /// The conversion rate from asset currency to base currency.
        /// </summary>
        [AttributeName("fxRateToBase")]
        public decimal FXRateToBase { get; set; }

        /// <summary>
        /// The multiplier of the contract traded.
        /// </summary>
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

        /// <summary>
        /// Put or call.
        /// </summary>
        [AttributeName("putCall")]
        [MaxLength(10)]
        public string OptionType { get; set; }

        /// <summary>
        /// Buy or sell.
        /// </summary>
        [AttributeName("buySell")]
        [MaxLength(10)]
        public string BuySell { get; set; }

        /// <summary>
        /// Null for fake trades.
        /// </summary>
        [AttributeName("ibOrderID")]
        public long? IBOrderID { get; set; }

        /// <summary>
        /// Net cash is calculated by subtracting the commissions and taxes from trade money.
        /// </summary>
        [AttributeName("netCash")]
        public decimal NetCash { get; set; }

        /// <summary>
        /// STP, LMT, MKT, etc.
        /// </summary>
        [AttributeName("orderType")]
        [MaxLength(20)]
        public string OrderType { get; set; }

        /// <summary>
        /// The order reference number as defined by the user on the order ticket. 
        /// Available on daily (single-day) activity flex queries only.
        /// </summary>
        [MaxLength(255)]
        [AttributeName("orderReference")]
        public string OrderReference { get; set; }

        /// <summary>
        /// Is the order actually real? False for virtual "helper" orders.
        /// </summary>
        [NotXmlMapped]
        public bool IsReal { get; set; }

        /// <summary>
        /// User-set reference price for execution analysis.
        /// </summary>
        [NotXmlMapped]
        public decimal? ReferencePrice { get; set; }

        /// <summary>
        /// User-set reference time for execution analysis.
        /// </summary>
        [NotXmlMapped]
        public DateTime? ReferenceTime { get; set; }

        [NotXmlMapped]
        public ICollection<Execution> Executions { get; set; } = new ObservableCollection<Execution>();

        /// <summary>
        /// This property is used in some situations to keep track of FIFO PnL within a single trade.
        /// The FIFORealizedPnL value is provided by IB for the account as a whole.
        /// </summary>
        [NotXmlMapped]
        [NotMapped]
        public decimal PerTradeFIFOPnL { get; set; }

        public int? AccountID { get; set; }

        public virtual Account Account { get; set; }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            var clone = new Order();
            clone.ConID = ConID;
            clone.Instrument = Instrument;
            clone.InstrumentID = InstrumentID;
            clone.TradeDate = TradeDate;
            clone.OrderPlacementTime = OrderPlacementTime;
            clone.Quantity = Quantity;
            clone.Price = Price;
            clone.Commission = Commission;
            clone.Currency = Currency;
            clone.CurrencyID = CurrencyID;
            clone.CommissionCurrency = CommissionCurrency;
            clone.CommissionCurrencyID = CommissionCurrencyID;
            clone.AssetCategory = AssetCategory;
            clone.FXRateToBase = FXRateToBase;
            clone.Multiplier = Multiplier;
            clone.TradeMoney = TradeMoney;
            clone.Proceeds = Proceeds;
            clone.Taxes = Taxes;
            clone.ClosePrice = ClosePrice;
            clone.OpenClose = OpenClose;
            clone.Notes = Notes;
            clone.CostBasis = CostBasis;
            clone.FIFORealizedPnL = FIFORealizedPnL;
            clone.MTMPnL = MTMPnL;
            clone.OptionType = OptionType;
            clone.OrderType = OrderType;
            clone.BuySell = BuySell;
            clone.IsReal = false;
            clone.IBOrderID = null;
            clone.ReferencePrice = ReferencePrice;
            clone.AccountID = AccountID;
            clone.Account = Account;
            clone.OrderReference = OrderReference;

            return clone;
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2} {3} for {4:c2} ({5}) at {6}",
                ID,
                OrderType,
                BuySell,
                Quantity,
                Price,
                Currency.Name,
                TradeDate);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}