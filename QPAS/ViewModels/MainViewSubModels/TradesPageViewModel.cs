// -----------------------------------------------------------------------
// <copyright file="TradesPageViewModel.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using MahApps.Metro.Controls.Dialogs;
using NLog;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace QPAS
{
    public class TradesPageViewModel : ViewModelBase
    {
        public CollectionViewSource TradesSource { get; set; }

        public ObservableCollection<Strategy> Strategies { get; set; }

        public IMainViewModel Parent { get; set; }

        private readonly IContextFactory _contextFactory;
        internal IDataSourcer Datasourcer;
        private readonly DataContainer _data;
        private Logger _logger = LogManager.GetCurrentClassLogger();

        public TradesRepository TradesRepository { get; }

        public ICommand Delete { get; private set; }
        public ICommand Reset { get; private set; }
        public ICommand UpdateStats { get; private set; }
        public ICommand OpenTrades { get; private set; }
        public ICommand CloseTrades { get; private set; }
        public ICommand RunScripts { get; private set; }
        public ReactiveCommand<string, Trade> Create { get; private set; }

        public TradesPageViewModel(IContextFactory contextFactory, IDialogCoordinator dialogService, IDataSourcer datasourcer, IAppSettings settings, DataContainer data, IMainViewModel parent)
            : base(dialogService)
        {
            Parent = parent;
            _contextFactory = contextFactory;
            Datasourcer = datasourcer;
            _data = data;
            TradesRepository = new TradesRepository(contextFactory, datasourcer, settings);

            TradesSource = new CollectionViewSource();
            TradesSource.Source = _data.Trades;
            TradesSource.View.SortDescriptions.Add(new SortDescription("DateOpened", ListSortDirection.Descending));

            Strategies = data.Strategies;

            CreateCommands();
        }

        private void CreateCommands()
        {
            Delete = new RelayCommand<IList>(async x => await DeleteTrades(x));
            Reset = ReactiveCommand.CreateFromTask<IList>(async x => await ResetTrades(x));
            UpdateStats = ReactiveCommand.CreateFromTask<IList>(async x => await UpdateTradeStats(x));
            OpenTrades = ReactiveCommand.CreateFromTask<IList>(async x => await Open(x));
            CloseTrades = ReactiveCommand.CreateFromTask<IList>(async x => await Close(x));
            RunScripts = ReactiveCommand.CreateFromTask<IList>(async x => await RunUserScripts(x));
            Create = ReactiveCommand.CreateFromTask<string, Trade>(async x => await CreateTrade(x));
        }

        public async Task<Trade> CreateTrade(string name)
        {
            //only add a trade if there's a name in the box
            if (String.IsNullOrEmpty(name)) return null;

            var newTrade = new Trade { Name = name, Open = true };
            await TradesRepository.Add(newTrade);
            _data.Trades.Add(newTrade);
            return newTrade;
        }

        public async Task AddOrders(Trade trade, List<Order> orders)
        {
            await TradesRepository.AddOrders(trade, orders);
        }

        public async Task RemoveOrder(Trade trade, Order order)
        {
            await TradesRepository.RemoveOrder(trade, order);
        }

        /// <summary>
        /// Opens/closes a trade
        /// </summary>
        public async Task ChangeOpenState(bool newOpenState, Trade trade)
        {
            if (newOpenState == false && !trade.IsClosable()) return;

            using (var dbContext = _contextFactory.Get())
            {
                trade.Open = newOpenState;
                await TradesRepository.UpdateStats(trade);
                await TradesRepository.UpdateTrade(trade);
            }
        }

        private async Task RunUserScripts(IList trades)
        {
            if (trades == null || trades.Count == 0) return;

            var openTrades = trades.Cast<Trade>().Where(x => x.Open).ToList();

            if (openTrades.Count == 0) return;

            List<UserScript> scripts;
            using (var dbContext = _contextFactory.Get())
            {
                scripts = dbContext.UserScripts.Where(x => x.Type == UserScriptType.TradeScript).ToList();
            }

            foreach (var script in scripts)
            {
                try
                {
                    await Parent.ScriptRunner.RunTradeScript(script, openTrades).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, "User script {0} generated an exception: ", script.Name);
                    _logger.Log(LogLevel.Error, ex);
                    await DialogService.ShowMessageAsync(Parent, "Error", $"User script {script.Name} generated an exception: {ex.Message}. See log for more details.");
                }
            }
        }

        private async Task Close(IList trades)
        {
            await TradesRepository.CloseTrades(trades);
        }

        private async Task Open(IList trades)
        {
            foreach (Trade trade in trades)
            {
                trade.Open = true;
            }

            await TradesRepository.UpdateTrade(trades: trades.Cast<Trade>());
        }

        private async Task UpdateTradeStats(IList trades)
        {
            if (trades == null || trades.Count == 0) return;

            foreach (Trade t in trades)
            {
                await TradesRepository.UpdateStats(t);
            }

            await TradesRepository.UpdateTrade(trades: trades.Cast<Trade>());
        }

        private async Task ResetTrades(IList trades)
        {
            if (trades == null || trades.Count == 0) return;

            MessageDialogResult res = await DialogService.ShowMessageAsync(Parent,
                "Reset Trade",
                string.Format("Are you sure you want to reset {0} trades?", trades.Count),
                MessageDialogStyle.AffirmativeAndNegative);

            if (res != MessageDialogResult.Affirmative) return;

            //reset the trades
            foreach (Trade trade in trades)
            {
                await TradesRepository.Reset(trade);
            }
        }

        public async Task AddCashTransactionsToTrade(Trade trade, IEnumerable<CashTransaction> cashTransactions)
        {
            await TradesRepository.AddCashTransactions(trade, cashTransactions).ConfigureAwait(false);
        }

        public async Task RemoveCashTransactionsFromTrade(Trade trade, IEnumerable<CashTransaction> cashTransactions)
        {
            foreach (var ct in cashTransactions)
            {
                await TradesRepository.RemoveCashTransaction(trade, ct).ConfigureAwait(false);
            }
        }

        public async Task AddFxTransactionToTrade(Trade trade, FXTransaction fxTransaction)
        {
            await TradesRepository.AddFXTransaction(trade, fxTransaction);
        }

        public async Task RemoveFxTransactionFromTrade(Trade trade, FXTransaction fxTransaction)
        {
            await TradesRepository.RemoveFXTransaction(trade, fxTransaction);
        }

        private async Task DeleteTrades(IList trades)
        {
            if (trades == null || trades.Count == 0) return;

            MessageDialogResult res = await DialogService.ShowMessageAsync(Parent,
                "Delete Trade",
                string.Format("Are you sure you want to delete {0} trades?", trades.Count),
                MessageDialogStyle.AffirmativeAndNegative);

            if (res != MessageDialogResult.Affirmative) return;

            var selectedTrades = trades.Cast<Trade>().ToList();

            //delete the trades
            using (var dbContext = _contextFactory.Get())
            {
                foreach (Trade trade in selectedTrades)
                {
                    _data.Trades.Remove(trade);
                    dbContext.Trades.Remove(trade);
                }
                await dbContext.SaveChangesAsync();
            }
        }
    }
}