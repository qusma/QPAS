// -----------------------------------------------------------------------
// <copyright file="TradeFiltering.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QPAS
{
    public static class TradeFiltering
    {
        /// <summary>
        /// Returns all trades that have any of the tags provided
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static List<Trade> FilterByTags(List<Tag> tags, IEnumerable<Trade> trades)
        {
            if (tags == null) throw new ArgumentNullException("tags");
            if (trades == null) throw new ArgumentNullException("trades");

            return trades.Where(x => x.Tags.Intersect(tags).Any()).ToList();
        }

        public static List<Trade> FilterByStrategies(List<Strategy> strategies, IEnumerable<Trade> trades)
        {
            if (strategies == null) throw new ArgumentNullException("strategies");
            if (trades == null) throw new ArgumentNullException("trades");

            return trades.Where(x => strategies.Contains(x.Strategy)).ToList();
        }

        public static List<Trade> Filter(List<Tag> selectedTags, List<Strategy> selectedStrategies, List<Instrument> selectedInstruments, IEnumerable<Trade> allTrades, TradeFilterSettings settings)
        {
            //filter dates
            var trades = allTrades
                .Where(x => x.DateClosed == null || x.DateClosed > settings.From)
                .Where(x => x.DateOpened < settings.To)
                .ToList();

            //filter open if required
            if (settings.ClosedTradesOnly)
            {
                trades = trades.Where(x => !x.Open).ToList();
            }

            //filter by strategy
            switch (settings.StrategyFilterMethod)
            {
                case FilterMethod.Any:
                    trades = trades.Where(x => selectedStrategies.Contains(x.Strategy)).ToList();
                    break;

                case FilterMethod.Exclude:
                    trades = trades.Where(x => !selectedStrategies.Contains(x.Strategy)).ToList();
                    break;
            }

            //filter by tags
            switch (settings.TagFilterMethod)
            {
                case FilterMethod.Any:
                    trades = trades.Where(x => selectedTags.Intersect(x.Tags).Any()).ToList();
                    break;

                case FilterMethod.All:
                    trades = trades.Where(x => selectedTags.Intersect(x.Tags).Count() == selectedTags.Count).ToList();
                    break;

                case FilterMethod.Exclude:
                    trades = trades.Where(x => !selectedTags.Intersect(x.Tags).Any()).ToList();
                    break;
            }

            List<int> selectedInstrumentIDs = selectedInstruments.Select(x => x.ID).ToList();

            //filter by instrument
            switch (settings.InstrumentFilterMethod)
            {
                case FilterMethod.Any:
                    trades = trades.Where(x =>
                        x.Orders.Select(y => y.InstrumentID).Intersect(selectedInstrumentIDs).Any() ||
                        x.CashTransactions.Where(y => y.InstrumentID.HasValue).Select(y => y.InstrumentID.Value).Intersect(selectedInstrumentIDs).Any())
                        .ToList();
                    break;

                case FilterMethod.All:
                    trades = trades.Where(x =>
                        x.Orders.Select(y => y.InstrumentID).Intersect(selectedInstrumentIDs).Union(
                        x.CashTransactions.Where(y => y.InstrumentID.HasValue).Select(y => y.InstrumentID.Value)).Count()
                            == selectedInstrumentIDs.Count)
                        .ToList();
                    break;

                case FilterMethod.Exclude:
                    trades = trades.Where(x =>
                        !x.Orders.Select(y => y.InstrumentID).Intersect(selectedInstrumentIDs).Any() &&
                        !x.CashTransactions.Where(y => y.InstrumentID.HasValue).Select(y => y.InstrumentID.Value).Intersect(selectedInstrumentIDs).Any())
                        .ToList();
                    break;
            }

            return trades.ToList();
        }
    }
}