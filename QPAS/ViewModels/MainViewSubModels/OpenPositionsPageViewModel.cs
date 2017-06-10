// -----------------------------------------------------------------------
// <copyright file="OpenPositionsPageViewModel.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using System;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using MahApps.Metro.Controls.Dialogs;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using ReactiveUI;

namespace QPAS
{
    public class OpenPositionsPageViewModel : ViewModelBase
    {
        private Account _selectedAccount;

        public ObservableCollection<OpenPosition> OpenPositions { get; }

        public ObservableCollection<FXPosition> FXPositions { get; }

        public ObservableCollection<Account> Accounts { get; }

        public Account SelectedAccount
        {
            get => _selectedAccount;
            set => this.RaiseAndSetIfChanged(ref _selectedAccount, value);
        }

        public PlotModel UnrealizedPnLChartModel { get; private set; }

        /// <summary>
        /// Key: instrument symbol
        /// value: unrealized profit/loss
        /// </summary>
        public ObservableCollection<Tuple<string, decimal>> UnrealizedPnL { get; set; }

        public OpenPositionsPageViewModel(IDBContext context, IDialogCoordinator dialogService)
            : base(dialogService)
        {
            UnrealizedPnL = new ObservableCollection<Tuple<string, decimal>>();

            OpenPositions = new ObservableCollection<OpenPosition>();
            FXPositions = new ObservableCollection<FXPosition>();
            Accounts = new ObservableCollection<Account>();
            Accounts.Add(new Account { ID = -1, AccountId = "All" });

            CreatePlotModel();

            SelectedAccount = Accounts.First();
            this.WhenAnyValue(x => x.SelectedAccount).Subscribe(async _ => await Refresh().ConfigureAwait(true));
        }

        private void CreatePlotModel()
        {
            UnrealizedPnLChartModel = new PlotModel { Title = "Unrealized Profit/Loss" };

            var linearAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "c0",
                MajorGridlineStyle = LineStyle.Dash
            };
            
            UnrealizedPnLChartModel.Axes.Add(linearAxis);

            var categoryAxis = new CategoryAxis
            {
                Position = AxisPosition.Left,
                MinorStep = 1,
                ItemsSource = UnrealizedPnL,
                LabelField = "Item1",
                GapWidth = 0.5,
                Minimum = -1
            };
            UnrealizedPnLChartModel.Axes.Add(categoryAxis);

            var series = new BarSeries
            {
                FillColor = OxyColors.DodgerBlue,
                StrokeColor = OxyColor.FromRgb(67, 110, 160),
                StrokeThickness = 1,
                ItemsSource = UnrealizedPnL,
                ValueField = "Item2",
                LabelFormatString = "{0:c0}",
                LabelPlacement = LabelPlacement.Inside
            };

            UnrealizedPnLChartModel.Series.Add(series);
            
            UnrealizedPnLChartModel.InvalidatePlot(true);
        }

        public override async Task Refresh()
        {
            //Necessary hack, openpositions are deleted in another context when importing statements
            //so we need to detach and reload everything
            OpenPositions.Clear();
            FXPositions.Clear();
            
            using (var context = new DBContext())
            {
                if (SelectedAccount.AccountId == "All")
                {
                    OpenPositions.AddRange(await context.OpenPositions.Include(x => x.Instrument).Include(x => x.Currency).ToListAsync().ConfigureAwait(true));
                    FXPositions.AddRange(await context.FXPositions.Include(x => x.FXCurrency).ToListAsync().ConfigureAwait(true));
                }
                else if (SelectedAccount != null)
                {
                    OpenPositions.AddRange(await context.OpenPositions.Where(x => x.AccountID == SelectedAccount.ID).Include(x => x.Instrument).Include(x => x.Currency).ToListAsync().ConfigureAwait(true));
                    FXPositions.AddRange(await context.FXPositions.Where(x => x.AccountID == SelectedAccount.ID).Include(x => x.FXCurrency).ToListAsync().ConfigureAwait(true));
                }

                //Add any accounts that exist in the db but are missing here
                var tmpAccounts = context.Accounts.ToList();
                var newAccounts = tmpAccounts.Except(Accounts, new LambdaEqualityComparer<Account>((x, y) => x.ID == y.ID));
                Accounts.AddRange(newAccounts);
            }

            UpdateChartSeries();
        }

        private void UpdateChartSeries()
        {
            if (SelectedAccount == null) return;

            UnrealizedPnL.Clear();
            foreach (var tuple in OpenPositions
                                .Where(x => x.Instrument != null)
                                .OrderBy(x => x.UnrealizedPnL)
                                .Select(x => new Tuple<string, decimal>(x.Instrument.Symbol, x.UnrealizedPnL)))
            {
                UnrealizedPnL.Add(tuple);
            }

            UnrealizedPnLChartModel.Axes[1].Maximum = UnrealizedPnL.Count;

            UnrealizedPnLChartModel.InvalidatePlot(true);
        }
    }
}