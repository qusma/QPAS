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
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using ReactiveUI;

namespace QPAS
{
    public class CashTransactionsPageViewModel : ViewModelBase
    {
        internal IDBContext Context;
        private readonly MainViewModel _mainVm;
        internal ITradesRepository TradesRepository;

        public CollectionViewSource CashTransactionsSource { get; set; }
        public ICommand Delete { get; private set; }

        public CashTransactionsPageViewModel(IDBContext context, IDataSourcer datasourcer, IDialogCoordinator dialogService, MainViewModel mainVm)
            : base(dialogService)
        {
            Context = context;
            _mainVm = mainVm;

            CashTransactionsSource = new CollectionViewSource();
            CashTransactionsSource.Source = Context.CashTransactions.Local;
            CashTransactionsSource.View.SortDescriptions.Add(new SortDescription("TransactionDate", ListSortDirection.Descending));

            TradesRepository = mainVm.TradesRepository;

            CreateCommands();
        }

        public override async Task Refresh()
        {
            await Context
                .CashTransactions
                .Include(x => x.Trade)
                .Include(x => x.Instrument)
                .OrderByDescending(x => x.TransactionDate)
                .LoadAsync().ConfigureAwait(true);

            CashTransactionsSource.View.Refresh();
        }

        private void CreateCommands()
        {
            Delete = ReactiveCommand.CreateFromTask<IList>(async x => await DeleteCashTransactions(x).ConfigureAwait(true));
        }

        private async Task DeleteCashTransactions(IList cts)
        {
            if (cts == null || cts.Count == 0) return;

            var res = await DialogService.ShowMessageAsync(_mainVm,
                "Delete Order(s)",
                string.Format("Are you sure you want to delete {0} cash transaction(s)?", cts.Count),
                MessageDialogStyle.AffirmativeAndNegative);

            if (res == MessageDialogResult.Affirmative)
            {
                foreach (CashTransaction ct in cts)
                {
                    if (ct.Trade != null)
                    {
                        await TradesRepository.RemoveCashTransaction(ct.Trade, ct).ConfigureAwait(true);
                    }
                    Context.CashTransactions.Remove(ct);
                }
                Context.SaveChanges();
            }
        }
    }
}