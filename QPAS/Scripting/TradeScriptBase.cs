// -----------------------------------------------------------------------
// <copyright file="TradeScriptBase.cs" company="">
// Copyright 2015 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using NLog;
using System.Collections.Generic;
using System.Linq;

namespace QPAS.Scripting
{
    public abstract class TradeScriptBase
    {
        protected IReadOnlyList<Tag> Tags;
        protected IReadOnlyList<Strategy> Strategies;
        protected ILogger Logger;
        private List<TradeScriptAction> _actions = new List<TradeScriptAction>();

        protected TradeScriptBase(DataContainer data, ILogger logger)
        {
            Strategies = data.Strategies.ToList();
            Tags = data.Tags.ToList();
            Logger = logger;
        }

        /// <summary>
        /// Add a tag to a trade.
        /// </summary>
        protected void SetTag(Trade trade, string tagName)
        {
            var tag = Tags.FirstOrDefault(x => x.Name == tagName);
            if (tag == null)
            {
                Logger.Log(LogLevel.Info, "User script {0} tried to add tag {1} to trade {2}, but it was not found", this.GetType().Name, tagName, trade);
                return;
            }

            SetTag(trade, tag);
        }

        /// <summary>
        /// Add a tag to a trade.
        /// </summary>
        protected void SetTag(Trade trade, Tag tag)
        {
            if (tag == null) return;
            if (tag.ID == 0) return;
            if (trade.Tags != null && trade.Tags.Contains(tag)) return;

            _actions.Add(new SetTag(trade, tag));

            Logger.Log(LogLevel.Info, "User script {0} added tag {1} to trade {2}", this.GetType().Name, tag, trade);
        }

        /// <summary>
        /// Set a trade's strategy.
        /// </summary>
        protected void SetStrategy(Trade trade, string strategyName)
        {
            var strategy = Strategies.FirstOrDefault(x => x.Name == strategyName);
            if (strategy == null)
            {
                Logger.Log(LogLevel.Warn, "User script {0} tried to set strategy of trade {1} to {2}, but it was not found.", this.GetType().Name, trade, strategyName);
                return;
            }

            SetStrategy(trade, strategy);
        }

        /// <summary>
        /// Set a trade's strategy.
        /// </summary>
        protected void SetStrategy(Trade trade, Strategy strategy)
        {
            _actions.Add(new SetStrategy(trade, strategy));

            Logger.Log(LogLevel.Info, "User script {0} set strategy of trade {1} to {2}", this.GetType().Name, trade, strategy);
        }

        /// <summary>
        /// Close a trade.
        /// </summary>
        protected void CloseTrade(Trade trade)
        {
            if (!trade.Open)
            {
                Logger.Log(LogLevel.Info, "User script {0} tried to close trade {1} but it was already closed", this.GetType().Name, trade);
                return;
            }

            if (!trade.IsClosable())
            {
                Logger.Log(LogLevel.Info, "User script {0} tried to close trade {1} but the trade can not be closed", this.GetType().Name, trade);
                return;
            }

            _actions.Add(new CloseTrade(trade));

            Logger.Log(LogLevel.Info, "User script {0} closed trade {1}", this.GetType().Name, trade);
        }

        protected void Log(string message)
        {
            Logger.Log(LogLevel.Info, message);
        }

        /// <summary>
        /// The implementation of this is the script that runs.
        /// </summary>
        /// <param name="openTrades">A list of all open trades.</param>
        public abstract void ProcessTrades(List<Trade> openTrades);

        /// <summary>
        /// Runs the user-provided script and returns to specified actions
        /// </summary>
        /// <returns></returns>
        public List<TradeScriptAction> GenerateActions(List<Trade> openTrades)
        {
            ProcessTrades(openTrades);
            return _actions;
        }
    }
}