using System.Collections.Generic;
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
    public class OrdersPageViewModel : ViewModelBase
    {
        public CollectionViewSource OrdersSource { get; set; }

        public ObservableCollection<Strategy> Strategies { get; set; }

        public MainViewModel Parent { get; set; }

        public Order SelectedOrder { get; set; }

        internal IDataSourcer Datasourcer;
        internal IDBContext Context;
        internal TradesRepository TradesRepository;
        internal ExecutionStatsGenerator ExecutionStatsGenerator;


        public ICommand Delete { get; set; }
        public ICommand CloneSelected { get; set; }
        public ICommand SetExecutionReportOrders { get; set; }

        public OrdersPageViewModel(IDBContext context, IDialogService dialogService, IDataSourcer datasourcer, MainViewModel parent)
            : base(dialogService)
        {
            Context = context;
            Parent = parent;
            Datasourcer = datasourcer;

            TradesRepository = new TradesRepository(Context, Datasourcer);

            OrdersSource = new CollectionViewSource();
            OrdersSource.Source = Context.Orders.Local;
            OrdersSource.SortDescriptions.Add(new SortDescription("TradeDate", ListSortDirection.Descending));

            ExecutionStatsGenerator = new ExecutionStatsGenerator(datasourcer);

            CreateCommands();
        }

        private void CreateCommands()
        {
            CloneSelected = new RelayCommand<int>(CloneOrder);
            Delete = new RelayCommand<IList>(DeleteOrders);
            SetExecutionReportOrders = new RelayCommand<IList>(SetExecReportOrders);
        }

        private void SetExecReportOrders(IList orders)
        {
            if (orders == null) return;
            ExecutionStatsGenerator.SetOrders(orders.Cast<Order>().ToList());
            var window = new ExecutionReportWindow(ExecutionStatsGenerator);
            window.Show();
        }

        private async void DeleteOrders(IList orders)
        {
            if (orders == null || orders.Count == 0) return;
            var selectedOrders = orders.Cast<Order>().ToList();

            var res = await DialogService.ShowMessageAsync(
                "Delete Order(s)",
                string.Format("Are you sure you want to delete {0} order(s)?", selectedOrders.Count),
                MessageDialogStyle.AffirmativeAndNegative);

            if (res == MessageDialogResult.Affirmative)
            {
                foreach (Order o in selectedOrders)
                {
                    //remove executions first
                    if(o.Executions != null)
                    {
                        List<Execution> toRemove = o.Executions.ToList();
                        foreach(Execution exec in toRemove)
                        {
                            Context.Executions.Remove(exec);
                        }
                        o.Executions.Clear();
                    }

                    //if the order belongs to a trade, remove it
                    if (o.Trade != null)
                    {
                        TradesRepository.RemoveOrder(o.Trade, o);
                    }

                    //finally delete the order
                    Context.Orders.Remove(o);
                }
                Context.SaveChanges();
            }
        }

        private async void CloneOrder(int size)
        {
            if (SelectedOrder == null) return;

            if (size <= 0) return;

            var res = await DialogService.ShowMessageAsync(
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

            Context.Orders.Add(cloneBuy);
            Context.Orders.Add(cloneSell);

            Context.SaveChanges();

            OrdersSource.View.Refresh();
        }

        public override void Refresh()
        {
            Context
                .Orders
                .OrderByDescending(z => z.TradeDate)
                .Include(x => x.Instrument)
                .Include(x => x.Currency)
                .Include(x => x.CommissionCurrency)
                .Include(x => x.Executions)
                .Include(x => x.Account)
                .Load();

            OrdersSource.View.Refresh();
        }
    }
}