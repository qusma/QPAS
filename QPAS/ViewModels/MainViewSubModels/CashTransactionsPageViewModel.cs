// -----------------------------------------------------------------------
// <copyright file="CashTransactionsPageViewModel.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace QPAS
{
    public class CashTransactionsPageViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainVm;
        private readonly IContextFactory contextFactory;

        public CollectionViewSource CashTransactionsSource { get; }
        public ObservableCollection<CashTransaction> CashTransactions { get; }
        public ICommand Delete { get; private set; }

        public CashTransactionsPageViewModel(IContextFactory contextFactory, IDataSourcer datasourcer, IDialogCoordinator dialogService, ObservableCollection<CashTransaction> cashTransactions, MainViewModel mainVm)
            : base(dialogService)
        {
            _mainVm = mainVm;

            CashTransactions = cashTransactions;
            CashTransactionsSource = new CollectionViewSource();
            CashTransactionsSource.Source = CashTransactions;
            CashTransactionsSource.View.SortDescriptions.Add(new SortDescription("TransactionDate", ListSortDirection.Descending));

            CreateCommands();
            this.contextFactory = contextFactory;
        }

        public override async Task Refresh()
        {
        }

        private void CreateCommands()
        {
            Delete = ReactiveCommand.CreateFromTask<IList>(async x => await DeleteCashTransactions(x));
        }

        private async Task DeleteCashTransactions(IList cts)
        {
            if (cts == null || cts.Count == 0) return;

            var res = await DialogService.ShowMessageAsync(_mainVm,
                "Delete Order(s)",
                string.Format("Are you sure you want to delete {0} cash transaction(s)?", cts.Count),
                MessageDialogStyle.AffirmativeAndNegative);

            if (res != MessageDialogResult.Affirmative)
            {
                return;
            }

            using (var context = contextFactory.Get())
            {
                foreach (CashTransaction ct in cts)
                {
                    if (ct.Trade != null)
                    {
                        //only delete those that are not assigned to a trade
                        continue;
                        //await TradesRepository.RemoveCashTransaction(ct.Trade, ct);
                    }
                    context.CashTransactions.Remove(ct);
                }
                context.SaveChanges();
            }
        }
    }
}