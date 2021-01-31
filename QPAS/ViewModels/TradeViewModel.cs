// -----------------------------------------------------------------------
// <copyright file="TradeViewModel.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace QPAS
{
    public class TradeViewModel : ViewModelBase
    {
        private Trade _trade;
        public Trade Trade
        {
            get => _trade;
            set => this.RaiseAndSetIfChanged(ref _trade, value);
        }

        private TradeTracker _tracker;
        private IContextFactory _contextFactory;
        private readonly IDataSourcer _dataSourcer;
        private readonly IAppSettings _settings;

        public ObservableCollection<Order> Orders { get; } = new ObservableCollection<Order>();
        public ObservableCollection<FXTransaction> FxTransactions { get; } = new ObservableCollection<FXTransaction>();
        public ObservableCollection<CashTransaction> CashTransactions { get; } = new ObservableCollection<CashTransaction>();


        public TradeTracker Tracker
        {
            get => _tracker;
            set => this.RaiseAndSetIfChanged(ref _tracker, value);
        }

        public TradeViewModel(Trade trade, IContextFactory contextFactory, IDataSourcer dataSourcer, IAppSettings settings) : base(null)
        {
            _contextFactory = contextFactory;
            _dataSourcer = dataSourcer;
            _settings = settings;
            Trade = trade;
            Orders.AddRange(trade.Orders);
            FxTransactions.AddRange(trade.FXTransactions);
            CashTransactions.AddRange(trade.CashTransactions);
        }

        public async Task SimulateTrade()
        {
            Tracker = await TradeSim.SimulateTrade(Trade, _contextFactory, _dataSourcer, _settings.OptionsCapitalUsageMultiplier).ConfigureAwait(true);
        }
    }
}
