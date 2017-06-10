// -----------------------------------------------------------------------
// <copyright file="TradeViewModel.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Linq;
using System.Data.Entity;
using System.Threading.Tasks;
using EntityModel;
using QPAS.Properties;
using ReactiveUI;

namespace QPAS
{
    public class TradeViewModel : ViewModelBase
    {
        private readonly IDataSourcer _dataSourcer;
        private readonly IDBContext _context;
        private Trade _trade;
        public Trade Trade
        {
            get => _trade;
            set => this.RaiseAndSetIfChanged(ref _trade, value);
        }

        private TradeTracker _tracker;
        public TradeTracker Tracker
        {
            get => _tracker;
            set => this.RaiseAndSetIfChanged(ref _tracker, value);
        }

        public TradeViewModel() : base(null)
        {
            Trade = new Trade();
        }

        public TradeViewModel(Trade trade, IDataSourcer dataSourcer, IDBContext context) : base(null)
        {
            _dataSourcer = dataSourcer;
            _context = context;
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
        }

        public async Task SimulateTrade()
        {
            Tracker = await TradeSim.SimulateTrade(Trade, _context, _dataSourcer, Settings.Default.optionsCapitalUsageMultiplier).ConfigureAwait(true);
        }
    }
}
