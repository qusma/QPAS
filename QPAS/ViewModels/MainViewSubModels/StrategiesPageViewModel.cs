using EntityModel;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Data.Entity;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace QPAS
{
    public class StrategiesPageViewModel : ViewModelBase
    {
        public CollectionViewSource StrategiesSource { get; set; }

        public MainViewModel Parent { get; set; }

        internal IDBContext Context;

        public ICommand Delete { get; set; }

        public StrategiesPageViewModel(IDBContext context, IDialogService dialogService, MainViewModel parent)
            : base(dialogService)
        {
            Context = context;
            Parent = parent;

            StrategiesSource = new CollectionViewSource();
            StrategiesSource.Source = Context.Strategies.Local;

            CreateCommands();
        }

        private void CreateCommands()
        {
            Delete = new RelayCommand<Strategy>(DeleteStrategy);
        }

        public override void Refresh()
        {
            Context.Strategies.Load();
        }

        private async void DeleteStrategy(Strategy strategy)
        {
            if (strategy == null) return;

            int tradesCount = Context.Trades.Count(x => x.StrategyID == strategy.ID);

            if (tradesCount > 0)
            {
                await DialogService.ShowMessageAsync("Cannot delete", string.Format("Can't delete this strategy, it still has {0} trades in it.", tradesCount));
                return;
            }

            MessageDialogResult result = await DialogService.ShowMessageAsync(
                "Delete strategy",
                String.Format("Are you sure you want to delete {0}?", strategy.Name),
                MessageDialogStyle.AffirmativeAndNegative);

            if (result == MessageDialogResult.Affirmative)
            {
                Context.Strategies.Remove(strategy);
                Context.SaveChanges();
            }
        }
    }
}