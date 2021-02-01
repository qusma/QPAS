using EntityModel;
using MahApps.Metro.Controls.Dialogs;
using NLog;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace QPAS
{
    public class OrdersPageViewModel : ViewModelBase
    {
        public CollectionViewSource OrdersSource { get; set; }

        public ObservableCollection<Strategy> Strategies { get; set; }

        public IMainViewModel Parent { get; set; }

        public Order SelectedOrder { get; set; }

        private readonly IContextFactory _contextFactory;
        internal IDataSourcer Datasourcer;
        private readonly DataContainer _data;
        private readonly IScriptRunner _scriptRunner;
        private readonly TradesRepository TradesRepository;
        private Logger _logger = LogManager.GetCurrentClassLogger();
        internal ExecutionStatsGenerator ExecutionStatsGenerator;


        public ICommand Delete { get; private set; }
        public ICommand CloneSelected { get; private set; }
        public ICommand SetExecutionReportOrders { get; private set; }
        public ICommand RunScripts { get; private set; }

        public OrdersPageViewModel(IContextFactory contextFactory, IDialogCoordinator dialogService, IDataSourcer datasourcer, IAppSettings settings, DataContainer data, IScriptRunner scriptRunner, IMainViewModel parent)
            : base(dialogService)
        {
            Parent = parent;
            _contextFactory = contextFactory;
            Datasourcer = datasourcer;
            _data = data;
            _scriptRunner = scriptRunner;
            TradesRepository = new TradesRepository(contextFactory, datasourcer, settings);

            OrdersSource = new CollectionViewSource();
            OrdersSource.Source = data.Orders;
            OrdersSource.SortDescriptions.Add(new SortDescription("TradeDate", ListSortDirection.Descending));

            ExecutionStatsGenerator = new ExecutionStatsGenerator(datasourcer);

            CreateCommands();
        }

        private void CreateCommands()
        {
            CloneSelected = new RelayCommand<int>(SplitIntoVirtualOrders);
            Delete = ReactiveCommand.CreateFromTask<IList>(async x => await DeleteOrders(x));
            SetExecutionReportOrders = new RelayCommand<IList>(SetExecReportOrders);
            RunScripts = ReactiveCommand.CreateFromTask<IList>(async x => await RunUserScripts(x));
        }

        private async Task RunUserScripts(IList orders)
        {
            if (orders == null || orders.Count == 0) return;

            List<UserScript> scripts;
            using (var dbContext = _contextFactory.Get())
            {
                scripts = dbContext.UserScripts.Where(x => x.Type == UserScriptType.OrderScript).ToList();
            }

            foreach (var script in scripts)
            {
                try
                {
                    await _scriptRunner.RunOrderScript(script, orders.Cast<Order>().ToList()).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, "User script {0} generated an exception: ", script.Name);
                    _logger.Log(LogLevel.Error, ex);
                    await DialogService.ShowMessageAsync(Parent, "Error", $"User script {script.Name} generated an exception: {ex.Message}. See log for more details.");
                }
            }
        }

        private void SetExecReportOrders(IList orders)
        {
            if (orders == null) return;
            ExecutionStatsGenerator.SetOrders(orders.Cast<Order>().ToList());
            var window = new ExecutionReportWindow(ExecutionStatsGenerator);
            window.Show();
        }

        private async Task DeleteOrders(IList orders)
        {
            if (orders == null || orders.Count == 0) return;
            var selectedOrders = orders.Cast<Order>().ToList();

            var res = await DialogService.ShowMessageAsync(Parent,
                "Delete Order(s)",
                string.Format("Are you sure you want to delete {0} order(s)?", selectedOrders.Count),
                MessageDialogStyle.AffirmativeAndNegative);

            if (res != MessageDialogResult.Affirmative) return;

            //if the order belongs to a trade, remove it
            foreach (Order o in selectedOrders)
            {
                if (o.Trade != null)
                {
                    await TradesRepository.RemoveOrder(o.Trade, o);
                }
            }


            using (var dbContext = _contextFactory.Get())
            {
                foreach (Order o in selectedOrders)
                {
                    //remove executions first
                    if (o.Executions != null)
                    {
                        List<Execution> toRemove = o.Executions.ToList();
                        foreach (Execution exec in toRemove)
                        {
                            dbContext.Executions.Remove(exec);
                        }
                        o.Executions.Clear();
                    }

                    //finally delete the order
                    dbContext.Orders.Remove(o);
                }
                await dbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Splits one order into two different virtual orders with a given share size. 
        /// Used when one order needs to be assigned to two trades.
        /// </summary>
        /// <param name="size"></param>
        private async void SplitIntoVirtualOrders(int size)
        {
            if (SelectedOrder == null) return;

            if (size <= 0) return;

            var res = await DialogService.ShowMessageAsync(Parent,
                "Virtual Order Creation",
                string.Format("Are you sure you want to create virtual orders for {0} shares?", size),
                MessageDialogStyle.AffirmativeAndNegative);

            if (res != MessageDialogResult.Affirmative) return;

            var originalOrder = SelectedOrder;

            var cloneBuy = (Order)originalOrder.Clone();
            var cloneSell = (Order)originalOrder.Clone();
            cloneBuy.Quantity = size;
            cloneSell.Quantity = -size;
            cloneBuy.Taxes = 0;
            cloneSell.Taxes = 0;
            cloneBuy.Commission = 0;
            cloneSell.Commission = 0;
            cloneBuy.Notes = "";
            cloneSell.Notes = "";
            cloneBuy.BuySell = "BUY";
            cloneSell.BuySell = "SELL";
            cloneBuy.Trade = null;
            cloneSell.Trade = null;
            cloneBuy.FIFORealizedPnL = 0;
            cloneSell.FIFORealizedPnL = 0;
            cloneBuy.IBOrderID = null;
            cloneSell.IBOrderID = null;
            cloneBuy.TradeMoney = size * originalOrder.Price;
            cloneSell.TradeMoney = -size * originalOrder.Price;
            cloneBuy.Proceeds = -size * originalOrder.Price;
            cloneSell.Proceeds = size * originalOrder.Price;
            cloneBuy.CostBasis = size * originalOrder.Price;
            cloneSell.CostBasis = -size * originalOrder.Price;
            cloneBuy.NetCash = -size * originalOrder.Price;
            cloneSell.NetCash = size * originalOrder.Price;
            cloneBuy.MTMPnL = size * (originalOrder.Price - originalOrder.ClosePrice);
            cloneSell.MTMPnL = -size * (originalOrder.Price - originalOrder.ClosePrice);

            using (var dbContext = _contextFactory.Get())
            {
                dbContext.Orders.Add(cloneBuy);
                dbContext.Orders.Add(cloneSell);

                dbContext.SaveChanges();

                _data.Orders.Add(cloneBuy);
                _data.Orders.Add(cloneSell);
            }
            OrdersSource.View.Refresh();
        }

        public override async Task Refresh()
        {
        }
    }
}