// -----------------------------------------------------------------------
// <copyright file="FXTransactionsPageViewModel.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using EntityModel;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;

namespace QPAS
{
    public class FXTransactionsPageViewModel : ViewModelBase
    {
        public CollectionViewSource FXTransactions { get; set; }

        internal IDBContext Context;
        private readonly IMainViewModel _mainVm;
        internal ITradesRepository TradesRepository;

        public ICommand Delete { get; set; }

        public FXTransactionsPageViewModel(IDBContext context, IDataSourcer datasourcer, IDialogCoordinator dialogService, IMainViewModel mainVm)
            : base(dialogService)
        {
            Context = context;
            _mainVm = mainVm;
            TradesRepository = mainVm.TradesRepository;

            FXTransactions = new CollectionViewSource();
            FXTransactions.Source = Context.FXTransactions.Local;
            FXTransactions.View.SortDescriptions.Add(new SortDescription("DateTime", ListSortDirection.Descending));

            CreateCommands();
        }

        private void CreateCommands()
        {
            Delete = ReactiveCommand.CreateFromTask<IList>(async x => await DeleteFxTransactions(x).ConfigureAwait(true));
        }

        private async Task DeleteFxTransactions(IList fxts)
        {
            if (fxts == null || fxts.Count == 0) return;

            var res = await DialogService.ShowMessageAsync(_mainVm,
                "Delete Order(s)",
                string.Format("Are you sure you want to delete {0} FX transaction(s)?", fxts.Count),
                MessageDialogStyle.AffirmativeAndNegative);

            if (res == MessageDialogResult.Affirmative)
            {
                foreach (FXTransaction fxt in fxts)
                {
                    if (fxt.Trade != null)
                    {
                        await TradesRepository.RemoveFXTransaction(fxt.Trade, fxt).ConfigureAwait(true);
                    }
                    Context.FXTransactions.Remove(fxt);
                }
                Context.SaveChanges();
            }
        }

        public override async Task Refresh()
        {
            await Context.FXTransactions.Include(x => x.FXCurrency).OrderBy(x => x.DateTime).LoadAsync().ConfigureAwait(true);

            FXTransactions.View.Refresh();
        }
    }
}