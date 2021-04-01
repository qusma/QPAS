// -----------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.EntityFrameworkCore;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Serialization;
using Tag = EntityModel.Tag;

namespace QPAS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, IDisposable
    {
        public IDataSourcer DataSourcer;

        private Logger _logger = LogManager.GetCurrentClassLogger();

        public MainViewModel ViewModel
        {
            get { return viewModel; }
            set
            {
                viewModel = value;
            }
        }

        public DbContextFactory ContextFactory { get; private set; }
        public IAppSettings Settings { get; }

        /// <summary>
        /// Ridiculous hack to end the editing of a cell without commiting changes.
        /// </summary>
        private bool _tradesGridIsCellEditEnding;

        private MainViewModel viewModel;

        public void Dispose()
        {
            if (DataSourcer != null)
            {
                DataSourcer.Dispose();
                DataSourcer = null;
            }
        }

        public MainWindow(DataContainer data, IAppSettings settings, DbContextFactory contextFactory, IDataSourcer dataSourcer)
        {
            /////////////////////////////////////////////////////////
            InitializeComponent();
            /////////////////////////////////////////////////////////

            Settings = settings;
            ContextFactory = contextFactory;
            DataSourcer = dataSourcer;

            ViewModel = new MainViewModel(ContextFactory, DataSourcer, DialogCoordinator.Instance, Settings, data);
            this.DataContext = ViewModel;

            PopulateStatementMenus();

            //Restore column ordering, widths, and sorting
            LoadDataGridLayouts();

            //A hack to force the heavy stuff to load,
            //providing snappier navigation at the expense of longer startup time
#if !DEBUG
            TradesGrid.Measure(new Size(500, 500));

            OrdersGrid.Measure(new Size(500, 500));

            CashTransactionsGrid.Measure(new Size(500, 500)); //todo: remove this stuff?
#endif
            //hiding the tab headers
            Style s = new Style();
            s.Setters.Add(new Setter(VisibilityProperty, Visibility.Collapsed));
            MainTabCtl.ItemContainerStyle = s;

            //close the slash screen
            //App.Splash.LoadComplete();

            this.Show();

            ShowChangelog();
        }

        /// <summary>
        /// After we have loaded the IStatementParser/IStatementDownloader plugins,
        /// we populate the menus so the user can access them.
        /// </summary>
        private void PopulateStatementMenus()
        {
            foreach (string name in ViewModel.StatementHandler.DownloaderNames)
            {
                var btn = new MenuItem();
                btn.Header = name;
                btn.Command = ViewModel.LoadStatementFromWeb;
                btn.CommandParameter = name;
                LoadStatementFromWebBtn.Items.Add(btn);
            }

            foreach (string name in ViewModel.StatementHandler.ParserNames)
            {
                var btn = new MenuItem();
                btn.Header = name;
                btn.Command = ViewModel.LoadStatementFromFile;
                btn.CommandParameter = name;
                LoadStatementFromFileBtn.Items.Add(btn);
            }
        }

        private void ShowChangelog()
        {
            if (Utils.CheckAndSaveVersion())
            {
                var window = new ChangelogWindow();
                window.Show();
            }
        }

        private void BtnExit_ItemClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var item = (TreeViewItem)e.NewValue;
            if (item.HasItems) return; //one of the high-level items, nothing to do
            var pages = new List<string>
            {
                "OpenPositions",
                "Instruments",
                "Strategies",
                "Trades",
                "Orders",
                "CashTransactions",
                "FXTransactions",
                "Tags",
                "Benchmarks",
                "PerformanceOverview",
                "PerformanceReport",
                "ExecutionReport"
            };

            string pageName = (string)item.Tag;
            int pageIndex = pages.IndexOf(pageName);
            if (pageIndex >= 0)
                MainTabCtl.SelectedIndex = pageIndex;

            var selectedTab = (TabItem)MainTabCtl.SelectedItem;
            ViewModel.SelectedPageViewModel = selectedTab.DataContext as ViewModelBase;
            await ViewModel.RefreshCurrentPage();

            RefreshSelectedPage();
        }

        private void RefreshSelectedPage()
        {
            if (MainTabCtl.SelectedItem == null) return;
            var selectedTab = (TabItem)MainTabCtl.SelectedItem;
            string page = (string)selectedTab.Header;

            switch (page)
            {
                case "Trades":
                    RefreshTradesPage();
                    break;
            }
        }

        private void RefreshTradesPage()
        {
            //populate "Set Strategy" context submenu
            var tradesGridSetStrategySubMenu = (MenuItem)Resources["TradesGridSetStrategySubMenu"];
            tradesGridSetStrategySubMenu.Items.Clear();
            foreach (Strategy s in ViewModel.Data.Strategies.OrderBy(x => x.Name))
            {
                var menuItem = new MenuItem { Header = s.Name };
                menuItem.Click += SetStrategySubMenuItem_Click;
                tradesGridSetStrategySubMenu.Items.Add(menuItem);
            }

            //populate "Set Tag" context submenu
            var tradesGridSetTagSubMenu = (MenuItem)Resources["TradesGridSetTagSubMenu"];
            tradesGridSetTagSubMenu.Items.Clear();
            foreach (Tag s in ViewModel.Data.Tags.OrderBy(x => x.Name))
            {
                var menuItem = new MenuItem { Header = s.Name };
                menuItem.IsCheckable = true;
                menuItem.Click += TradesGridSetTagSubMenu_Click;
                tradesGridSetTagSubMenu.Items.Add(menuItem);
            }
        }

        private async void TradesGridSetTagSubMenu_Click(object sender, RoutedEventArgs e)
        {
            if (TradesGrid.SelectedItems == null) return;

            var sourceBtn = (MenuItem)e.Source;
            string tagName = (string)sourceBtn.Header;
            var tag = ViewModel.Data.Tags.First(x => x.Name == tagName);

            var selectedTrades = TradesGrid.SelectedItems.Cast<Trade>().ToList();
            bool add = sourceBtn.IsChecked;

            foreach (var trade in selectedTrades)
            {
                await ViewModel.TradesPageViewModel.TradesRepository.SetTag(trade, tag, true);
            }
        }

        private void TradesGridSetTagMenu_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            //multiple selection of trades means that sometimes only some
            //of the trades have a tag selected.
            //It's not clear what to do in those cases, so we just disable
            //the tags that are not either enabled or disabled for ALL trades
            int tradesCount = TradesGrid.SelectedItems.Count;
            List<Trade> selectedTrades = TradesGrid.SelectedItems.Cast<Trade>().ToList();

            var tradesGridSetTagSubMenu = (MenuItem)Resources["TradesGridSetTagSubMenu"];
            foreach (MenuItem item in tradesGridSetTagSubMenu.Items)
            {
                string tagName = (string)item.Header;
                int matchedCount = selectedTrades.Count(x => x.Tags != null && x.Tags.Any(y => y.Name == tagName));
                item.IsEnabled = matchedCount == 0 || matchedCount == tradesCount;
                item.IsChecked = matchedCount == tradesCount;
            }
        }

        /// <summary>
        /// Trade grid context menu to set a trade's strategy
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SetStrategySubMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (TradesGrid.SelectedItems == null) return;

            string strategyName = (string)((MenuItem)e.Source).Header;
            var strategy = ViewModel.Data.Strategies.FirstOrDefault(x => x.Name == strategyName);
            if (strategy == null) return;

            using (var dbContext = ContextFactory.Get())
            {
                var dbStrategy = await dbContext.Strategies.FirstAsync(x => x.ID == strategy.ID);
                foreach (Trade t in TradesGrid.SelectedItems)
                {
                    var dbTrade = await dbContext.Trades.Include(x => x.Strategy).FirstAsync(x => x.ID == t.ID);
                    dbTrade.Strategy = dbStrategy;

                    t.Strategy = strategy;
                }

                dbContext.SaveChanges();
            }
        }

        /// <summary>
        /// This method works for all datagrids. It's a bit hacky, but it works.
        /// There's no event for _after_ the edits have been commited to the ItemsSource,
        /// so we have to delay the SaveChanges() call.
        /// </summary>
        private void DataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            var dataGrid = (DataGrid)sender;
            if (e.EditAction == DataGridEditAction.Commit)
            {
                var view = (ListCollectionView)CollectionViewSource.GetDefaultView(dataGrid.ItemsSource);
                if (view.IsAddingNew)
                {
                    // This callback will be called after the CollectionView
                    // has pushed the changes back to the DataGrid.ItemSource.
                    Dispatcher.BeginInvoke(new DispatcherOperationCallback(param =>
                    {
                        using (var dbContext = ContextFactory.Get())
                        {
                            dbContext.Entry(e.Row.Item).State = EntityState.Added;
                            dbContext.SaveChanges();
                        }

                        return null;
                    }), DispatcherPriority.Background, new object[] { null });
                }
                else if (view.IsEditingItem)
                {
                    // This callback will be called after the CollectionView
                    // has pushed the changes back to the DataGrid.ItemSource.
                    Dispatcher.BeginInvoke(new DispatcherOperationCallback(param =>
                    {
                        if (e.Row.Item is Trade)
                        {
                            //needs special handling
                            ViewModel.TradesPageViewModel.TradesRepository.UpdateTrade((Trade)e.Row.Item).Wait();
                        }
                        else
                        {
                            using (var dbContext = ContextFactory.Get())
                            {
                                dbContext.Entry(e.Row.Item).State = EntityState.Modified;
                                dbContext.SaveChanges();
                            }
                        }
                        return null;
                    }), DispatcherPriority.Background, new object[] { null });
                }
            }
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveDataGridLayouts();
            SettingsUtils.SaveSettings(ViewModel.Settings);
            Dispose();
        }

        private void TradesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //the stuff here is to make sure the click is in a row and not elsewher on the grid
            if (e.LeftButton != MouseButtonState.Pressed) return;
            var result = VisualTreeHelper.HitTest(TradesGrid, e.GetPosition(TradesGrid));
            var cell = Utils.FindVisualParent<DataGridCell>(result.VisualHit);
            if (cell == null) return;

            if (TradesGrid.SelectedItems == null || TradesGrid.SelectedItems.Count != 1) return;
            var tradeVm = new TradeViewModel((Trade)TradesGrid.SelectedItem, ContextFactory, DataSourcer, Settings);
            var tradeWindow = new TradeWindow(tradeVm, ContextFactory, ViewModel.TradesPageViewModel.TradesRepository);
            tradeWindow.ShowDialog();
        }

        private void AboutBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = new AboutWindow();
            window.ShowDialog();
        }

        private void DocumentationBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "https://github.com/qusma/QPAS/wiki",
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        private void SettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = new SettingsWindow(Settings, ContextFactory);
            window.Show();
        }

        /// <summary>
        /// Here we launch the custom tag editor in a popup
        /// </summary>
        private void TradesGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if ((string)e.Column.Header == "Tags")
            {
                //make sure the popup isn't too big, otherwise items can be hidden in small resolutions
                TagPickerPopup.Height = Math.Min(600, TradesGrid.ActualHeight - 100);

                //Fill it
                TagPickerPopupListBox.ItemsSource = ViewModel.Data
                    .Tags
                    .OrderBy(x => x.Name)
                    .ToList()
                    .Select(
                        x => new CheckListItem<Tag>(x))
                    .ToList();

                var trade = (Trade)TradesGrid.SelectedItem;

                if (trade.Tags != null)
                {
                    foreach (CheckListItem<Tag> item in TagPickerPopupListBox.Items)
                    {
                        item.IsChecked = trade.Tags.Contains(item.Item);
                    }
                }

                //and open it at the right position
                TagPickerPopup.PlacementTarget = TradesGrid.GetCell(e.Row.GetIndex(), e.Column.DisplayIndex);
                TagPickerPopup.IsOpen = true;
            }
        }

        private async void TradesGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (_tradesGridIsCellEditEnding) return;

            if ((string)e.Column.Header == "Tags")
            {
                var trade = (Trade)TradesGrid.SelectedItem;

                if (e.EditAction == DataGridEditAction.Commit)
                {
                    //save the new tag configuration
                    foreach (CheckListItem<Tag> item in TagPickerPopupListBox.Items)
                    {
                        if (item.IsChecked && !trade.Tags.Contains(item.Item))
                        {
                            trade.Tags.Add(item.Item);
                        }
                        else if (!item.IsChecked && trade.Tags.Contains(item.Item))
                        {
                            trade.Tags.Remove(item.Item);
                        }
                    }

                    trade.TagStringUpdated();
                }

                TagPickerPopup.IsOpen = false;
            }
            else if ((string)e.Column.Header == "Open")
            {
                var trade = (Trade)TradesGrid.SelectedItem;

                bool originalOpen = trade.Open;
                bool? newOpen = ((CheckBox)e.EditingElement).IsChecked;

                if (newOpen.HasValue && newOpen.Value != originalOpen)
                {
                    //The user has opened or closed the trade,
                    //so we do a stats update to make sure the numbers are right
                    //and set the proper closing time

                    //if we're closing the trade, make sure it's closable first
                    if (newOpen.Value == false && !trade.IsClosable())
                    {
                        e.Cancel = true;
                        _tradesGridIsCellEditEnding = true;
                        ((DataGrid)sender).CancelEdit(DataGridEditingUnit.Cell);
                        _tradesGridIsCellEditEnding = false;
                        return;
                    }

                    await Task.Run(async () =>
                    {
                        await ViewModel.TradesPageViewModel.ChangeOpenState(newOpen.Value, trade);
                    });
                }
            }
        }

        //The following two methods are an extremely dirty hack. The datagrid likes to steal focus
        //from the popup and close it. So we have to intercept click events first and prevent that from happening.
        private void TagPickerPopupCheckBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var checkBox = (CheckBox)sender;
            checkBox.IsChecked = checkBox.IsChecked.HasValue ? !checkBox.IsChecked.Value : true;
            e.Handled = true;
        }

        private void TagPickerPopupTextBlock_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void OrdersGridTradePickerPopup_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var checkBox = (CheckBox)sender;
            bool? previousValue = checkBox.IsChecked;
            foreach (CheckListItem<Trade> item in TradePickerListBox.Items)
            {
                item.IsChecked = false;
            }
            checkBox.IsChecked = previousValue.HasValue ? !previousValue.Value : true;
            e.Handled = true;
        }

        private void TradePickerNewTradeTextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            TradePickerNewTradeTextBox.Focus();
            e.Handled = true;
            //this is an even dirtier hack than the 3 above, because we have to give focus to the textbox
            //no matter what. And that closes the popup. So we re-open it.
            //The issue might be solvable by baking everything in the popup into a single usercontrol
            OrdersGridTradePickerPopup.IsOpen = true;
        }

        private async void TradePickerNewTradeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !String.IsNullOrEmpty(TradePickerNewTradeTextBox.Text))
            {
                var selectedOrder = (Order)OrdersGrid.SelectedItem;
                var newtrade = await ViewModel.TradesPageViewModel.CreateTrade(TradePickerNewTradeTextBox.Text);
                newtrade.Open = true;

                await ViewModel.TradesPageViewModel.AddOrders(newtrade, new List<Order> { selectedOrder });
                TradePickerNewTradeTextBox.Text = "";
                OrdersGridTradePickerPopup.IsOpen = false;
                OrdersGrid.CommitEdit();
            }
        }

        private void OrdersGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if ((string)e.Column.Header == "Trade")
            {
                //make sure the popup isn't too big, otherwise items can be hidden in small resolutions
                OrdersGridTradePickerPopup.Height = Math.Min(600, OrdersGrid.ActualHeight - 100);

                var order = (Order)OrdersGrid.SelectedItem;
                //if this order belongs to a closed trade, disallow editing
                if (order.Trade != null && order.Trade.Open == false)
                {
                    e.Cancel = true;
                    return;
                }

                //Fill it
                TradePickerListBox.ItemsSource = ViewModel.Data
                    .Trades
                    .Where(x => x.Open)
                    .OrderBy(x => x.Name)
                    .ToList()
                    .Select(
                        x => new CheckListItem<Trade>(x))
                    .ToList();

                //make sure the currently selected trade is checked
                if (order.Trade != null)
                {
                    foreach (CheckListItem<Trade> item in TradePickerListBox.Items)
                    {
                        item.IsChecked = order.Trade.ID == item.Item.ID;
                    }
                }

                //and open it at the right position
                OrdersGridTradePickerPopup.PlacementTarget = OrdersGrid.GetCell(e.Row.GetIndex(), e.Column.DisplayIndex);
                OrdersGridTradePickerPopup.IsOpen = true;
            }
        }

        /// <summary>
        /// Disable the virtual trades item in the orders grid context menu if more than one order is selected.
        /// Then populate the set trade menu
        /// </summary>
        private void OrdersGridContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            var menu = (ContextMenu)e.Source;
            var virtualTradesMenuItem = menu.Items.Cast<MenuItem>().FirstOrDefault(x => x.Name == "VirtualTradesMenuItem");

            if (virtualTradesMenuItem == null) return;

            if (OrdersGrid.SelectedItems == null || OrdersGrid.SelectedItems.Count > 1)
            {
                virtualTradesMenuItem.IsEnabled = false;
            }
            else
            {
                virtualTradesMenuItem.IsEnabled = true;
            }

            //populate "Set Trade" context submenu
            var ordersGridSetTradeSubMenu = (MenuItem)Resources["OrdersGridSetTradeSubMenu"];
            ordersGridSetTradeSubMenu.Items.Clear();
            foreach (Trade t in ViewModel.Data.Trades.Where(x => x.Open).OrderBy(x => x.Name))
            {
                var menuItem = new MenuItem { Header = t.Name, Tag = t.ID };
                menuItem.Click += OrdersGridSetTradeSubMenuItem_Click;
                ordersGridSetTradeSubMenu.Items.Add(menuItem);
            }

            //Add an item to create a new trade
            ordersGridSetTradeSubMenu.Items.Add(new Separator());
            var ordersContextMenuNewTradeItem = (MenuItem)Resources["OrdersContextMenuNewTradeItem"];
            ordersGridSetTradeSubMenu.Items.Add(ordersContextMenuNewTradeItem);
        }

        private async void OrdersGridSetTradeSubMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (OrdersGrid.SelectedItems == null) return;
            int tradeID = (int)((MenuItem)e.Source).Tag;
            var trade = ViewModel.Data.Trades.FirstOrDefault(x => x.Open && x.ID == tradeID);

            if (trade == null) return;

            var selectedOrders = new List<Order>(OrdersGrid.SelectedItems.Cast<Order>());
            await ViewModel.TradesPageViewModel.AddOrders(trade, selectedOrders.ToList());
        }

        private async void OrdersGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            //here we set the trade selected in the picker popup
            if ((string)e.Column.Header == "Trade")
            {
                if (e.EditAction == DataGridEditAction.Commit)
                {
                    var order = (Order)OrdersGrid.SelectedItem;
                    var items = TradePickerListBox.Items.Cast<CheckListItem<Trade>>().ToList();
                    var selectedTrade = items.FirstOrDefault(x => x.IsChecked);

                    if (selectedTrade == null)
                    {
                        await ViewModel.TradesPageViewModel.RemoveOrder(order.Trade, order);
                    }
                    else
                    {
                        await ViewModel.TradesPageViewModel.AddOrders(selectedTrade.Item, new List<Order> { order });
                    }
                }
                OrdersGridTradePickerPopup.IsOpen = false;
            }
        }

        private void VirtualTradeSizeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            if (OrdersGrid.SelectedItems == null || OrdersGrid.SelectedItems.Count > 1) return;

            var virtualTradeSizeTextBox = (TextBox)sender;

            int size;
            if (!Int32.TryParse(virtualTradeSizeTextBox.Text, out size)) return;
            ViewModel.OrdersPageViewModel.CloneSelected.Execute(size);

            virtualTradeSizeTextBox.Text = "";
        }

        private void CashTransactionsGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if ((string)e.Column.Header == "Trade")
            {
                var ct = (CashTransaction)CashTransactionsGrid.SelectedItem;
                //if this cash transaction belongs to a closed trade, disallow editing
                if (ct.Trade != null && ct.Trade.Open == false)
                {
                    e.Cancel = true;
                    return;
                }

                //Fill it
                CtGridTradePickerListBox.ItemsSource = ViewModel.Data
                    .Trades
                    .Where(x => x.Open)
                    .OrderBy(x => x.Name)
                    .ToList()
                    .Select(
                        x => new CheckListItem<Trade>(x))
                    .ToList();

                //make sure the currently selected trade is checked
                if (ct.Trade != null)
                {
                    foreach (CheckListItem<Trade> item in CtGridTradePickerListBox.Items)
                    {
                        item.IsChecked = ct.Trade.ID == item.Item.ID;
                    }
                }

                //and open it at the right position
                CashTransactionsGridTradePickerPopup.PlacementTarget = CashTransactionsGrid.GetCell(e.Row.GetIndex(), e.Column.DisplayIndex);
                CashTransactionsGridTradePickerPopup.IsOpen = true;
            }
        }

        private async void CashTransactionsGrid_OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            //here we set the trade selected in the picker popup
            if ((string)e.Column.Header == "Trade")
            {
                if (e.EditAction == DataGridEditAction.Commit)
                {
                    var ct = (CashTransaction)CashTransactionsGrid.SelectedItem;
                    var items = CtGridTradePickerListBox.Items.Cast<CheckListItem<Trade>>().ToList();
                    var selectedTrade = items.FirstOrDefault(x => x.IsChecked);

                    if (selectedTrade == null)
                    {
                        await ViewModel.TradesPageViewModel.RemoveCashTransactionsFromTrade(ct.Trade, new List<CashTransaction> { ct });
                    }
                    else
                    {
                        await ViewModel.TradesPageViewModel.AddCashTransactionsToTrade(selectedTrade.Item, new List<CashTransaction> { ct });
                    }
                }

                CashTransactionsGridTradePickerPopup.IsOpen = false;
            }
        }

        private void FXTransactionsGrid_OnBeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if ((string)e.Column.Header == "Trade")
            {
                var fxt = (FXTransaction)FXTransactionsGrid.SelectedItem;
                //if this fx transaction belongs to a closed trade, disallow editing
                if (fxt.Trade != null && fxt.Trade.Open == false)
                {
                    e.Cancel = true;
                    return;
                }

                //Fill it
                FxGridTradePickerListBox.ItemsSource = ViewModel.Data
                    .Trades
                    .Where(x => x.Open)
                    .OrderBy(x => x.Name)
                    .ToList()
                    .Select(
                        x => new CheckListItem<Trade>(x))
                    .ToList();

                //make sure the currently selected trade is checked
                if (fxt.Trade != null)
                {
                    foreach (CheckListItem<Trade> item in FxGridTradePickerListBox.Items)
                    {
                        item.IsChecked = fxt.Trade.ID == item.Item.ID;
                    }
                }

                //and open it at the right position
                FxTransactionsGridTradePickerPopup.PlacementTarget = FXTransactionsGrid.GetCell(e.Row.GetIndex(), e.Column.DisplayIndex);
                FxTransactionsGridTradePickerPopup.IsOpen = true;
            }
        }

        private async void FXTransactionsGrid_OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            //here we set the trade selected in the picker popup
            if ((string)e.Column.Header == "Trade")
            {
                if (e.EditAction == DataGridEditAction.Commit)
                {
                    var fxt = (FXTransaction)FXTransactionsGrid.SelectedItem;
                    var items = FxGridTradePickerListBox.Items.Cast<CheckListItem<Trade>>().ToList();
                    var selectedTrade = items.FirstOrDefault(x => x.IsChecked);

                    if (selectedTrade == null)
                    {
                        await ViewModel.TradesPageViewModel.RemoveFxTransactionFromTrade(fxt.Trade, fxt);
                    }
                    else
                    {
                        await ViewModel.TradesPageViewModel.AddFxTransactionToTrade(selectedTrade.Item, fxt);
                    }
                }

                FxTransactionsGridTradePickerPopup.IsOpen = false;
            }
        }

        private void CashTransactionsGridContextMenu_OnOpened(object sender, RoutedEventArgs e)
        {
            //populate "Set Trade" context submenu
            var ctGridSetTradeSubMenu = (MenuItem)Resources["CashTransactionsGridSetTradeSubMenu"];
            ctGridSetTradeSubMenu.Items.Clear();
            foreach (Trade t in ViewModel.Data.Trades.Where(x => x.Open).OrderBy(x => x.Name))
            {
                var menuItem = new MenuItem { Header = t.Name, Tag = t.ID };
                menuItem.Click += CashTransactionsGridSetTradeSubMenuItem_Click;
                ctGridSetTradeSubMenu.Items.Add(menuItem);
            }
        }

        private async void CashTransactionsGridSetTradeSubMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (CashTransactionsGrid.SelectedItems == null) return;
            int tradeID = (int)((MenuItem)e.Source).Tag;
            var trade = ViewModel.Data.Trades.FirstOrDefault(x => x.Open && x.ID == tradeID);

            if (trade == null) return;

            await ViewModel.TradesPageViewModel.AddCashTransactionsToTrade(trade, CashTransactionsGrid.SelectedItems.Cast<CashTransaction>());
        }

        private void FxTransactionsGridContextMenu_OnOpened(object sender, RoutedEventArgs e)
        {
            //populate "Set Trade" context submenu
            var fxtGridSetTradeSubMenu = (MenuItem)Resources["FxTransactionsGridSetTradeSubMenu"];
            fxtGridSetTradeSubMenu.Items.Clear();
            foreach (Trade t in ViewModel.Data.Trades.Where(x => x.Open).OrderBy(x => x.Name))
            {
                var menuItem = new MenuItem { Header = t.Name, Tag = t.ID };
                menuItem.Click += FxTransactionsGridSetTradeSubMenuItem_Click;
                fxtGridSetTradeSubMenu.Items.Add(menuItem);
            }
        }

        private async void FxTransactionsGridSetTradeSubMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (FXTransactionsGrid.SelectedItems == null) return;
            int tradeID = (int)((MenuItem)e.Source).Tag;
            var trade = ViewModel.Data.Trades.FirstOrDefault(x => x.Open && x.ID == tradeID);

            if (trade == null) return;

            foreach (FXTransaction fxt in FXTransactionsGrid.SelectedItems)
            {
                await ViewModel.TradesPageViewModel.AddFxTransactionToTrade(trade, fxt);
            }
        }

        /// <summary>
        /// When the Space key is pressed, toggle checked status on selected items.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckItemListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Space) return;

            var listbox = (ListBox)sender;
            foreach (ICheckListItem item in listbox.SelectedItems)
            {
                item.IsChecked = !item.IsChecked;
            }
            e.Handled = true;
        }

        private void TradesGridContextMenuCopyTagsBtn_Click(object sender, RoutedEventArgs e)
        {
            //Copies a list of tags from the selected trade to the clipboard
            DataFormat dataFormat = DataFormats.GetDataFormat(typeof(List<int>).FullName);

            IDataObject dataObject = new DataObject();

            var selectedTrade = (Trade)TradesGrid.SelectedItem;
            if (selectedTrade.Tags == null) return;

            List<int> dataToCopy = selectedTrade.Tags.Select(x => x.ID).ToList();
            dataObject.SetData(dataFormat.Name, dataToCopy, false);

            Clipboard.SetDataObject(dataObject, false);
        }

        private async void TradesGridContextMenuPasteTagsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (TradesGrid.SelectedItems == null || TradesGrid.SelectedItems.Count == 0) return;
            List<int> tagIDs = Utils.GetDataFromClipboard<List<int>>();
            List<Tag> tags = tagIDs.Select(id => ViewModel.Data.Tags.FirstOrDefault(x => x.ID == id)).Where(tag => tag != null).ToList();

            var trades = TradesGrid.SelectedItems.Cast<Trade>();

            foreach (Trade trade in trades)
            {
                trade.Tags.Clear();
                trade.Tags.AddRange(tags);
                trade.TagStringUpdated();
            }

            await ViewModel.TradesPageViewModel.TradesRepository.UpdateTrade(trades: trades);
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            //check if the clipboard has a list of tags, if not, disable the tag paste button
            var menu = (ContextMenu)sender;
            var pasteBtn = menu.Items.Cast<FrameworkElement>().First(x => x.Name == "TradesGridContextMenuPasteTagsBtn");

            List<int> tagIDs = Utils.GetDataFromClipboard<List<int>>();
            pasteBtn.IsEnabled = tagIDs != null;
        }

        private void InstrumentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource == this.InstrumentsGrid)
            {
                //the selectionchanged event is fired by the combobox when you scroll for some reason
                //this check is needed to stop that from happening
                ViewModel.InstrumentsPageViewModel.UpdateChartCommand.Execute(null);
            }
        }

        private async void OrdersContextMenuNewTradeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                //close the context menu
                var ordersGridContextMenu = (ContextMenu)Resources["OrdersGridContextMenu"];
                var ordersContextMenuNewTradeTextBox = (TextBox)sender;
                ordersGridContextMenu.IsOpen = false;

                if (String.IsNullOrEmpty(ordersContextMenuNewTradeTextBox.Text)) return;

                List<Order> selectedOrders = OrdersGrid.SelectedItems.Cast<Order>().ToList();

                var newTrade = await ViewModel.TradesPageViewModel.CreateTrade(ordersContextMenuNewTradeTextBox.Text);
                if (newTrade != null)
                    await ViewModel.TradesPageViewModel.AddOrders(newTrade, selectedOrders);

                ordersContextMenuNewTradeTextBox.Text = "";
            }
        }

        private Dictionary<string, DataGrid> GetDataGrids()
        {
            return new Dictionary<string, DataGrid>
            {
                { "TradesGrid", this.TradesGrid },
                { "OpenPositionsGrid", this.OpenPositionsGrid},
                { "InstrumentsGrid", this.InstrumentsGrid },
                { "StrategiesGrid", this.StrategiesGrid },
                { "OrdersGrid", this.OrdersGrid },
                { "CashTransactionsGrid", this.CashTransactionsGrid },
                { "FXTransactionsGrid", this.FXTransactionsGrid },
                { "TagsGrid", this.TagsGrid }
            };
        }

        private void SaveDataGridLayouts()
        {
            Dictionary<string, DataGrid> grids = GetDataGrids();
            var settings =
                grids
                .Select(x => new SerializableKvp<string, string>(x.Key, x.Value.SerializeLayout()))
                .ToList();

            var serializer = new XmlSerializer(typeof(List<SerializableKvp<string, string>>));
            using (var sw = new StringWriter())
            {
                serializer.Serialize(sw, settings);
                ViewModel.Settings.DataGridLayout = sw.ToString();
            }
        }

        private void LoadDataGridLayouts()
        {
            if (String.IsNullOrEmpty(ViewModel.Settings.DataGridLayout)) return;

            try
            {
                Dictionary<string, DataGrid> grids = GetDataGrids();
                Dictionary<string, string> settings;
                var serializer = new XmlSerializer(typeof(List<SerializableKvp<string, string>>));
                using (var sw = new StringReader(ViewModel.Settings.DataGridLayout))
                {
                    settings = ((List<SerializableKvp<string, string>>)serializer.Deserialize(sw)).ToDictionary(x => x.Key, x => x.Value);
                }

                foreach (var kvp in grids)
                {
                    if (!settings.ContainsKey(kvp.Key)) continue;

                    kvp.Value.DeserializeLayout(settings[kvp.Key]);
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "Could not load datagrid layout. Exception: " + ex);
            }
        }

        /// <summary>
        /// Starts the user script editor and exits this program.
        /// Can't compile the user scripts library while this is running.
        /// </summary>
        private void ScriptsBtn_Click(object sender, RoutedEventArgs e)
        {
            var scriptingWindow = new ScriptingWindow(ContextFactory, ViewModel.ScriptRunner, ViewModel.Data);
            scriptingWindow.Show();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await this.ShowMessageAsync("test", "test");
        }
    }
}