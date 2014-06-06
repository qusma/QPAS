// -----------------------------------------------------------------------
// <copyright file="TradesPageViewModel.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

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
        internal TradesRepository TradesRepository;

        public ICommand Delete { get; set; }

        public ICommand Reset { get; set; }

        public ICommand UpdateStats { get; set; }

        public TradesPageViewModel(IDBContext context, IDialogService dialogService, IDataSourcer datasourcer, MainViewModel parent)
            : base(dialogService)
        {
            Context = context;
            Parent = parent;
            Datasourcer = datasourcer;
            TradesRepository = new TradesRepository(Context, Datasourcer);

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

            MessageDialogResult res = await DialogService.ShowMessageAsync(
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

            MessageDialogResult res = await DialogService.ShowMessageAsync(
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