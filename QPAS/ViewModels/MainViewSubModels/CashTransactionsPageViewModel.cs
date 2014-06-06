// -----------------------------------------------------------------------
// <copyright file="CashTransactionsPageViewModel.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using MahApps.Metro.Controls.Dialogs;
using System.Collections;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace QPAS
{
    public class CashTransactionsPageViewModel : ViewModelBase
    {
        internal IDBContext Context;
        internal TradesRepository TradesRepository;

        public CollectionViewSource CashTransactionsSource { get; set; }
        public ICommand Delete { get; private set; }

        public CashTransactionsPageViewModel(IDBContext context, IDataSourcer datasourcer, IDialogService dialogService)
            : base(dialogService)
        {
            Context = context;

            CashTransactionsSource = new CollectionViewSource();
            CashTransactionsSource.Source = Context.CashTransactions.Local;
            CashTransactionsSource.View.SortDescriptions.Add(new SortDescription("TransactionDate", ListSortDirection.Descending));

            TradesRepository = new TradesRepository(Context, datasourcer);

            CreateCommands();
        }

        public override void Refresh()
        {
            Context
                .CashTransactions
                .Include(x => x.Trade)
                .Include(x => x.Instrument)
                .OrderByDescending(x => x.TransactionDate)
                .Load();

            CashTransactionsSource.View.Refresh();
        }

        private void CreateCommands()
        {
            Delete = new RelayCommand<IList>(DeleteCashTransactions);
        }

        private async void DeleteCashTransactions(IList cts)
        {
            if (cts == null || cts.Count == 0) return;

            var res = await DialogService.ShowMessageAsync(
                "Delete Order(s)",
                string.Format("Are you sure you want to delete {0} cash transaction(s)?", cts.Count),
                MessageDialogStyle.AffirmativeAndNegative);

            if (res == MessageDialogResult.Affirmative)
            {
                foreach (CashTransaction ct in cts)
                {
                    if (ct.Trade != null)
                    {
                        TradesRepository.RemoveCashTransaction(ct.Trade, ct);
                    }
                    Context.CashTransactions.Remove(ct);
                }
                Context.SaveChanges();
            }
        }
    }
}