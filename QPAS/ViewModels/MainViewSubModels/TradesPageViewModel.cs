// -----------------------------------------------------------------------
// <copyright file="TradesPageViewModel.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using EntityModel;
using MahApps.Metro.Controls.Dialogs;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace QPAS
{
    public class TradesPageViewModel : ViewModelBase
    {
        public CollectionViewSource TradesSource { get; set; }

        public ObservableCollection<Strategy> Strategies { get; set; }

        public MainViewModel Parent { get; set; }

        internal IDataSourcer Datasourcer;
        internal IDBContext Context;
        internal ITradesRepository TradesRepository;

        public ICommand Delete { get; private set; }
        public ICommand Reset { get; private set; }
        public ICommand UpdateStats { get; private set; }
        public ICommand OpenTrades { get; private set; }
        public ICommand CloseTrades { get; private set; }
        public ICommand RunScripts { get; private set; }


        public TradesPageViewModel(IDBContext context, IDialogCoordinator dialogService, IDataSourcer datasourcer, MainViewModel parent)
            : base(dialogService)
        {
            Context = context;
            Parent = parent;
            Datasourcer = datasourcer;
            TradesRepository = parent.TradesRepository;

            TradesSource = new CollectionViewSource();
            TradesSource.Source = Context.Trades.Local;
            TradesSource.View.SortDescriptions.Add(new SortDescription("DateOpened", ListSortDirection.Descending));

            Strategies = new ObservableCollection<Strategy>();

            CreateCommands();
        }

        private void CreateCommands()
        {
            Delete = new RelayCommand<IList>(DeleteTrades);
            Reset = new RelayCommand<IList>(ResetTrades);
            UpdateStats = new RelayCommand<IList>(UpdateTradeStats);
            OpenTrades = new RelayCommand<IList>(Open);
            CloseTrades = new RelayCommand<IList>(Close);
            RunScripts = new RelayCommand<IList>(RunUserScripts);
        }

        private void RunUserScripts(IList trades)
        {
            if (trades == null || trades.Count == 0) return;
            Parent.ScriptRunner.RunTradeScripts(trades.Cast<Trade>().ToList(), Context.Strategies.ToList(), Context.Tags.ToList(), Context);

            foreach(Trade trade in trades)
            {
                trade.TagStringUpdated();
            }
        }

        public override void Refresh()
        {
            Context.Trades
                .Include(x => x.Strategy)
                .OrderByDescending(x => x.DateOpened)
                .Load();

            //populate Strategies, used in combobox strategy selector
            Strategies.Clear();
            var strats = Context.Strategies.OrderBy(x => x.Name).ToList();
            foreach (Strategy s in strats)
            {
                Strategies.Add(s);
            }

            TradesSource.View.Refresh();
        }

        private void Close(IList trades)
        {
            if (trades == null || trades.Count == 0) return;

            var closedTrades = new List<Trade>();
            foreach(Trade trade in trades)
            {
                //Already closed or can't close -> skip it
                if (!trade.Open) continue;
                
                //first load up the collections, needed for the IsClosable() check.
                Context.Entry(trade).Collection(x => x.Orders).Load();
                Context.Entry(trade).Collection(x => x.CashTransactions).Load();
                Context.Entry(trade).Collection(x => x.FXTransactions).Load();

                //this needs to be done after loading the orders
                if (!trade.IsClosable()) continue;

                trade.Open = false;
                closedTrades.Add(trade);
            }

            //Update the stats of the trades we closed
            Task.Run(() =>
            {
                foreach (Trade trade in closedTrades)
                {
                    //we can skip collection load since it's done a few lines up
                    TradesRepository.UpdateStats(trade, skipCollectionLoad: true); 
                }
                Context.SaveChanges();
            });
        }

        private void Open(IList trades)
        {
            if (trades == null || trades.Count == 0) return;

            foreach (Trade trade in trades)
            {
                trade.Open = true;
            }
            Context.SaveChanges();
        }

        private void UpdateTradeStats(IList trades)
        {
            if (trades == null || trades.Count == 0) return;

            foreach (Trade t in trades)
            {
                TradesRepository.UpdateStats(t);
            }
            Context.SaveChanges();
        }

        private async void ResetTrades(IList trades)
        {
            if (trades == null || trades.Count == 0) return;

            MessageDialogResult res = await DialogService.ShowMessageAsync(this,
                "Reset Trade",
                string.Format("Are you sure you want to reset {0} trades?", trades.Count),
                MessageDialogStyle.AffirmativeAndNegative);

            if (res != MessageDialogResult.Affirmative) return;

            //reset the trades
            foreach (Trade trade in trades)
            {
                TradesRepository.Reset(trade);
            }
            Context.SaveChanges();
        }

        private async void DeleteTrades(IList trades)
        {
            if (trades == null || trades.Count == 0) return;

            MessageDialogResult res = await DialogService.ShowMessageAsync(this,
                "Delete Trade",
                string.Format("Are you sure you want to delete {0} trades?", trades.Count),
                MessageDialogStyle.AffirmativeAndNegative);

            if (res != MessageDialogResult.Affirmative) return;

            var selectedTrades = trades.Cast<Trade>().ToList();

            //reset the trades
            foreach (Trade trade in selectedTrades)
            {
                Context.Trades.Remove(trade);
            }
            Context.SaveChanges();
        }
    }
}