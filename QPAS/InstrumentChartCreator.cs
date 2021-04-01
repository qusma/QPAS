// -----------------------------------------------------------------------
// <copyright file="ChartingUtils.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using QDMS;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QPAS
{
    /// <summary>
    /// A bunch of stuff needed to create the instruments page chart
    /// </summary>
    public static class InstrumentChartCreator
    {
        /// <summary>
        /// Set up the basic layout and series of the instruments chart.
        /// </summary>
        public static PlotModel InitializePlotModel()
        {
            var model = new PlotModel { Title = "" };

            var axisColor = OxyColor.FromRgb(227, 227, 227);
            var gridLineColor = OxyColor.FromRgb(81, 81, 81);

            //title
            model.TitleColor = OxyColors.DodgerBlue;
            model.TitleFont = "Segoe UI Semibold";

            //Legend
            model.LegendFont = "Segoe UI Semibold";
            model.LegendTextColor = axisColor;
            model.LegendBorder = gridLineColor;

            //plot border
            model.PlotAreaBorderColor = axisColor;

            //axes
            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Right,
                TextColor = axisColor,
                AxislineColor = axisColor,
                TicklineColor = axisColor,
                MajorGridlineColor = gridLineColor,
                MajorGridlineStyle = LineStyle.Dash,
                MinorGridlineColor = gridLineColor,
                MinorGridlineStyle = LineStyle.Dash,
                Font = "Segoe UI Semibold",
                AbsoluteMinimum = 0
            });

            model.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                TextColor = axisColor,
                AxislineColor = axisColor,
                TicklineColor = axisColor,
                MajorGridlineColor = gridLineColor,
                MinorTickSize = 0,
                Font = "Segoe UI Semibold"
            });

            //prices
            model.Series.Add(new CandleStickSeries
            {
                CandleWidth = .4,
                Color = OxyColors.DimGray,
                IncreasingColor = OxyColor.FromRgb(0, 192, 0),
                DecreasingColor = OxyColor.FromRgb(255, 0, 0),
                StrokeThickness = 1
            });

            //buys and sells (markers)
            model.Series.Add(new ScatterSeries
            {
                Title = "Buys",
                MarkerFill = OxyColors.DodgerBlue,
                MarkerType = MarkerType.Triangle,
                MarkerSize = 3
            });

            model.Series.Add(new ScatterSeries
            {
                Title = "Sells",
                MarkerFill = OxyColor.FromRgb(222, 222, 222),
                MarkerType = MarkerType.Circle,
                MarkerSize = 3
            });

            return model;
        }

        /// <summary>
        /// Creates an annotation with an arrow pointing at the trade.
        /// </summary>
        /// <param name="date">The date of the trade</param>
        /// <param name="price">The price</param>
        /// <param name="quantity">The quantity</param>
        /// <param name="showSize">Should the annotation text include the quantity traded or not?</param>
        /// <returns></returns>
        private static ArrowAnnotation CreateAnnotation(DateTime date, decimal price, decimal quantity, bool showSize)
        {
            string annotationFormat = showSize ? "{0:+#;-#}\n{1:0.00}" : "{1:0.00}";
            bool buying = quantity > 0;

            var annotation = new ArrowAnnotation
            {
                Text = string.Format(annotationFormat, quantity, price),
                TextPosition = new DataPoint(DateTimeAxis.ToDouble(date), (double)price * (buying ? 0.985 : 1.015)),
                TextHorizontalAlignment = OxyPlot.HorizontalAlignment.Center,
                TextVerticalAlignment = buying ? OxyPlot.VerticalAlignment.Top : OxyPlot.VerticalAlignment.Bottom,
                Font = "Segoe UI",
                TextColor = OxyColor.FromRgb(227, 227, 227),
                Color = buying ? OxyColors.DodgerBlue : OxyColor.FromRgb(222, 222, 222),
                EndPoint = new DataPoint(DateTimeAxis.ToDouble(date.Date), (double)price),
                StartPoint = new DataPoint(DateTimeAxis.ToDouble(date.Date), (double)price * (buying ? 0.99 : 1.01)),
                HeadLength = 3,
                HeadWidth = 3
            };
            return annotation;
        }

        public static void AddCandlesticks(List<OHLCBar> data, PlotModel model, int daysToShow = 120)
        {
            if (data == null || data.Count == 0) return;

            var priceSeries = (CandleStickSeries)model.Series[0];
            priceSeries.Items.Clear();

            int j = 0;
            decimal minPrice = decimal.MaxValue, maxPrice = 0;
            foreach (OHLCBar bar in data)
            {
                priceSeries.Items.Add(new HighLowItem(DateTimeAxis.ToDouble(bar.DT), (double)bar.Low, (double)bar.High, (double)bar.Open, (double)bar.Close));
                j++;
                if (j > data.Count - daysToShow)
                {
                    minPrice = Math.Min(minPrice, bar.Low);
                    maxPrice = Math.Max(maxPrice, bar.High);
                }
            }

            //Y axis min/max
            model.Axes[0].Zoom((double)minPrice * 0.98, (double)maxPrice * 1.02);

            //time axis min/max
            model.Axes[1].Zoom(
                DateTimeAxis.ToDouble(data[Math.Max(data.Count - daysToShow, 0)].DT),
                DateTimeAxis.ToDouble(data.Last().DT.AddDays(3)));
        }

        public static void AddTransactionScatterPoints(IEnumerable<Tuple<DateTime, decimal, int>> groupedOrders, PlotModel model, bool addAnnotations, bool showSize)
        {
            model.Annotations.Clear();

            var buySeries = (ScatterSeries)model.Series[1];
            buySeries.Points.Clear();
            var sellSeries = (ScatterSeries)model.Series[2];
            sellSeries.Points.Clear();

            foreach (Tuple<DateTime, decimal, int> o in groupedOrders)
            {
                DateTime date = o.Item1;
                decimal price = o.Item2;
                int quantity = o.Item3;

                ScatterPoint point = new ScatterPoint(DateTimeAxis.ToDouble(date), (double)price);

                if (quantity > 0)
                {
                    buySeries.Points.Add(point);
                }
                else
                {
                    sellSeries.Points.Add(point);
                }

                if (addAnnotations)
                {
                    model.Annotations.Add(CreateAnnotation(date, price, quantity, showSize));
                }
            }
        }

        private static void AddProfitLossLine(DateTime entryDate, DateTime exitDate, decimal entryPrice, decimal exitPrice, int quantity, PlotModel model)
        {
            bool profitable = Math.Sign(quantity) == Math.Sign(exitPrice - entryPrice);

            var series = new LineSeries
            {
                LineStyle = LineStyle.Dot,
                Color = profitable ? OxyColors.LimeGreen : OxyColors.Red,
            };
            series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(entryDate), (double)entryPrice));
            series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(exitDate), (double)exitPrice));

            model.Series.Add(series);
        }

        /// <summary>
        /// This method is a complete mess...refactor at some point.
        /// </summary>
        public static void AddProfitLossLines(IEnumerable<Tuple<DateTime, decimal, int>> groupedOrders, PlotModel model)
        {
            int currentPosition = 0;

            LinkedList<decimal> openPrices = new LinkedList<decimal>();
            LinkedList<int> quantities = new LinkedList<int>();
            LinkedList<DateTime> entryDates = new LinkedList<DateTime>();

            foreach (var o in groupedOrders)
            {
                DateTime date = o.Item1;
                decimal price = o.Item2;
                int quantity = o.Item3;

                if (currentPosition == 0 || //we are opening a trade
                    Math.Sign(currentPosition) == Math.Sign(quantity))  //or adding to a position
                {
                    currentPosition += quantity;
                    quantities.AddLast(quantity);
                    openPrices.AddLast(price);
                    entryDates.AddLast(date);
                }
                else //we are closing a position
                {
                    if (Math.Abs(currentPosition) == Math.Abs(quantity)) //if it's the same quantity we simply close the trade(s)
                    {
                        //it may be that multiple entries are being closed, some may be profitable and others not, each one gets its own series
                        while (quantities.Count > 0)
                        {
                            AddProfitLossLine(entryDates.Dequeue(), date,
                                openPrices.Dequeue(), price, quantities.Dequeue(), model);
                        }
                        currentPosition = 0;
                    }
                    else //we are closing the trade and opening a new one at the same time OR only partially closing a trade
                    {
                        if (Math.Abs(currentPosition) < Math.Abs(quantity)) //closing the position and opening a new one
                        {
                            currentPosition = currentPosition + quantity;
                            while (quantities.Count > 0)
                            {
                                AddProfitLossLine(entryDates.Dequeue(), date,
                                    openPrices.Dequeue(), price, quantities.Dequeue(), model);
                            }
                            quantities.AddLast(currentPosition);
                            openPrices.AddLast(price);
                            entryDates.AddLast(date);
                        }
                        else //only partially closing a position
                        {
                            int quantityClosing = quantity;
                            currentPosition += quantityClosing;
                            while (Math.Abs(quantityClosing) > 0) //it may need a partial close, or multiple trade closes to get rid of this quantity
                            {
                                if (Math.Abs(quantityClosing) > Math.Abs(quantities.First())) //the quantity we have to close is larger than the top trade
                                {
                                    int tmpQuantity = quantities.Dequeue();
                                    AddProfitLossLine(entryDates.Dequeue(), date,
                                        openPrices.Dequeue(), price, tmpQuantity, model);
                                    quantityClosing += tmpQuantity;
                                }
                                else //the only possibility left is that the quantity we're closing is smaller than the top trade.
                                {
                                    int tmpQuantity = quantities.Dequeue(); //Decrease quantity value...bit of a hack but it's ok
                                    tmpQuantity += quantityClosing;
                                    quantities.AddFirst(tmpQuantity);
                                    AddProfitLossLine(entryDates.First(), date,
                                        openPrices.First(), price, tmpQuantity, model);
                                    quantityClosing = 0;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}