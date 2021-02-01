// -----------------------------------------------------------------------
// <copyright file="FXTransactionsPageViewModel.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using System.Collections;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace QPAS
{
    public class FXTransactionsPageViewModel : ViewModelBase
    {
        public CollectionViewSource FXTransactions { get; set; }

        private readonly IContextFactory _contextFactory;
        private readonly IMainViewModel _mainVm;

        public ICommand Delete { get; set; }
        public TradesRepository TradesRepository { get; }

        public FXTransactionsPageViewModel(IContextFactory contextFactory, IDataSourcer datasourcer, IDialogCoordinator dialogService, IAppSettings settings, DataContainer data, IMainViewModel mainVm)
            : base(dialogService)
        {
            _contextFactory = contextFactory;
            _mainVm = mainVm;

            FXTransactions = new CollectionViewSource();
            FXTransactions.Source = data.FXTransactions;
            FXTransactions.View.SortDescriptions.Add(new SortDescription("DateTime", ListSortDirection.Descending));

            TradesRepository = new TradesRepository(contextFactory, datasourcer, settings);

            CreateCommands();
        }

        private void CreateCommands()
        {
            Delete = ReactiveCommand.CreateFromTask<IList>(async x => await DeleteFxTransactions(x));
        }

        private async Task DeleteFxTransactions(IList fxts)
        {
            if (fxts == null || fxts.Count == 0) return;

            var res = await DialogService.ShowMessageAsync(_mainVm,
                "Delete Order(s)",
                string.Format("Are you sure you want to delete {0} FX transaction(s)?", fxts.Count),
                MessageDialogStyle.AffirmativeAndNegative);

            if (res != MessageDialogResult.Affirmative)
            {
                return;
            }

            using (var dbContext = _contextFactory.Get())
            {
                foreach (FXTransaction fxt in fxts)
                {
                    if (fxt.Trade != null)
                    {
                        await TradesRepository.RemoveFXTransaction(fxt.Trade, fxt);
                    }
                    dbContext.FXTransactions.Remove(fxt);
                }
                dbContext.SaveChanges();
            }
        }

        public override async Task Refresh()
        {

        }
    }
}