// -----------------------------------------------------------------------
// <copyright file="TradeFiltering.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using EntityModel;

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
        public static List<int> FilterByTags(List<Tag> tags, IDBContext context)
        {
            if (tags == null) throw new ArgumentNullException("tags");
            if (context == null) throw new ArgumentNullException("context");

            var trades = context.Trades.Include(x => x.Tags).ToList();
            return trades.Where(x => x.Tags.Intersect(tags).Any()).Select(x => x.ID).ToList();
        }

        public static List<int> FilterByStrategies(List<Strategy> strategies, IDBContext context)
        {
            if (strategies == null) throw new ArgumentNullException("strategies");
            if (context == null) throw new ArgumentNullException("context");

            var trades = context.Trades.Include(x => x.Strategy).ToList();
            return trades.Where(x => strategies.Contains(x.Strategy)).Select(x => x.ID).ToList();
        }

        public static List<Trade> Filter(List<Tag> selectedTags, List<Strategy> selectedStrategies, List<Instrument> selectedInstruments, IDBContext context, TradeFilterSettings settings)
        {
            if (context == null) throw new ArgumentNullException("context");

            //filter dates
            var trades = context
                .Trades
                .Include(x => x.Tags)
                .Include(x => x.Strategy)
                .Include(x => x.Orders)
                .Include(x => x.CashTransactions)
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
