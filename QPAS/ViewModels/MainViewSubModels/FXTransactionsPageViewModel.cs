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
using System.Windows.Data;
using System.Windows.Input;
using EntityModel;
using MahApps.Metro.Controls.Dialogs;

namespace QPAS
{
    public class FXTransactionsPageViewModel : ViewModelBase
    {
        public CollectionViewSource FXTransactions { get; set; }

        internal IDBContext Context;
        internal TradesRepository TradesRepository;

        public ICommand Delete { get; set; }

        public FXTransactionsPageViewModel(IDBContext context, IDataSourcer datasourcer, IDialogService dialogService)
            : base(dialogService)
        {
            Context = context;
            TradesRepository = new TradesRepository(Context, datasourcer);

            FXTransactions = new CollectionViewSource();
            FXTransactions.Source = Context.FXTransactions.Local;
            FXTransactions.View.SortDescriptions.Add(new SortDescription("DateTime", ListSortDirection.Descending));

            CreateCommands();
        }

        private void CreateCommands()
        {
            Delete = new RelayCommand<IList>(DeleteFxTransactions);
        }

        private async void DeleteFxTransactions(IList fxts)
        {
            if (fxts == null || fxts.Count == 0) return;

            var res = await DialogService.ShowMessageAsync(
                "Delete Order(s)",
                string.Format("Are you sure you want to delete {0} FX transaction(s)?", fxts.Count),
                MessageDialogStyle.AffirmativeAndNegative);

            if (res == MessageDialogResult.Affirmative)
            {
                foreach (FXTransaction fxt in fxts)
                {
                    if (fxt.Trade != null)
                    {
                        TradesRepository.RemoveFXTransaction(fxt.Trade, fxt);
                    }
                    Context.FXTransactions.Remove(fxt);
                }
                Context.SaveChanges();
            }
        }

        public override void Refresh()
        {
            Context.FXTransactions.Include(x => x.FXCurrency).OrderBy(x => x.DateTime).Load();

            FXTransactions.View.Refresh();
        }
    }
}