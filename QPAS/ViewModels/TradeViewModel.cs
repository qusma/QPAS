// -----------------------------------------------------------------------
// <copyright file="TradeViewModel.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Linq;
using System.Data.Entity;
using EntityModel;

namespace QPAS
{
    public class TradeViewModel : ViewModelBase
    {
        private Trade _trade;
        public Trade Trade
        {
            get
            {
                return _trade;
            }
            set
            {
                _trade = value;
                OnPropertyChanged();
            }
        }

        private TradeTracker _tracker;
        public TradeTracker Tracker
        {
            get
            {
                return _tracker;
            }
            set
            {
                _tracker = value;
                OnPropertyChanged();
            }
        }

        public TradeViewModel() : base(null)
        {
            Trade = new Trade();
        }

        public TradeViewModel(Trade trade, IDataSourcer dataSourcer, IDBContext context) : base(null)
        {
            context.Trades
                    .Where(x => x.ID == trade.ID)
                    .Include(x => x.Strategy)
                    .Include(x => x.Orders)
                    .Include("Orders.Instrument")
                    .Include("Orders.Currency")
                    .Include(x => x.CashTransactions)
                    .Include("CashTransactions.Instrument")
                    .Include("CashTransactions.Currency")
                    .Include(x => x.FXTransactions)
                    .Include("FXTransactions.FunctionalCurrency")
                    .Include("FXTransactions.FXCurrency")
                    .Load();

            Trade = trade;
            Tracker = TradeSim.SimulateTrade(trade, context, dataSourcer);
        }
    }
}
