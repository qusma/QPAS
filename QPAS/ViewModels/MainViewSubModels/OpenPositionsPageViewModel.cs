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
using System.Windows.Data;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace QPAS
{
    public class OpenPositionsPageViewModel : ViewModelBase
    {
        internal IDBContext Context;

        public CollectionViewSource OpenPositionsSource { get; private set; }

        public CollectionViewSource FXPositionsSource { get; private set; }

        public PlotModel UnrealizedPnLChartModel { get; private set; }

        /// <summary>
        /// Key: instrument symbol
        /// value: unrealized profit/loss
        /// </summary>
        public ObservableCollection<Tuple<string, decimal>> UnrealizedPnL { get; set; }

        public OpenPositionsPageViewModel(IDBContext context, IDialogService dialogService)
            : base(dialogService)
        {
            Context = context;
            UnrealizedPnL = new ObservableCollection<Tuple<string, decimal>>();

            OpenPositionsSource = new CollectionViewSource();
            OpenPositionsSource.Source = Context.OpenPositions.Local;

            FXPositionsSource = new CollectionViewSource();
            FXPositionsSource.Source = Context.FXPositions.Local;

            CreatePlotModel();
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

        public override void Refresh()
        {
            //Necessary hack, openpositions are deleted in another context when importing statements
            //so we need to detach and reload everything
            Context.OpenPositions.Local.ToList().ForEach(x =>
               {
                   Context.Entry(x).State = EntityState.Detached;
               });
            Context.OpenPositions.Include(x => x.Instrument).Include(x => x.Currency).Load();

            Context.FXPositions.Local.ToList().ForEach(x =>
            {
                Context.Entry(x).State = EntityState.Detached;
            });
            Context.FXPositions.Include(x => x.FXCurrency).Load();

            UpdateChartSeries();
        }

        private void UpdateChartSeries()
        {
            UnrealizedPnL.Clear();
            foreach (var tuple in Context
                                .OpenPositions
                                .Local
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