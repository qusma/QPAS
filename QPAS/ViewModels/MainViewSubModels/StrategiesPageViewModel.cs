using EntityModel;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace QPAS
{
    public class StrategiesPageViewModel : ViewModelBase
    {
        public CollectionViewSource StrategiesSource { get; set; }

        public MainViewModel Parent { get; set; }

        private readonly IContextFactory contextFactory;
        private readonly DataContainer data;

        public ICommand Delete { get; set; }

        public StrategiesPageViewModel(IContextFactory contextFactory, IDialogCoordinator dialogService, DataContainer data, MainViewModel parent)
            : base(dialogService)
        {
            Parent = parent;

            StrategiesSource = new CollectionViewSource();
            StrategiesSource.Source = data.Strategies;

            CreateCommands();
            this.contextFactory = contextFactory;
            this.data = data;
        }

        private void CreateCommands()
        {
            Delete = ReactiveCommand.CreateFromTask<Strategy>(async x => await DeleteStrategy(x));
        }

        public override async Task Refresh()
        {
        }

        private async Task DeleteStrategy(Strategy strategy)
        {
            if (strategy == null) return;


            int tradesCount = data.Trades.Count(x => x.StrategyID == strategy.ID);

            if (tradesCount > 0)
            {
                await DialogService.ShowMessageAsync(Parent,
                    "Cannot delete", string.Format("Can't delete this strategy, it still has {0} trades in it.", tradesCount));
                return;
            }

            MessageDialogResult result = await DialogService.ShowMessageAsync(Parent,
                "Delete strategy",
                String.Format("Are you sure you want to delete {0}?", strategy.Name),
                MessageDialogStyle.AffirmativeAndNegative);

            if (result != MessageDialogResult.Affirmative) return;

            using (var dbContext = contextFactory.Get())
            {
                dbContext.Strategies.Remove(strategy);
                dbContext.SaveChanges();

                data.Strategies.Remove(strategy);
            }
        }
    }
}