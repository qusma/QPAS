// -----------------------------------------------------------------------
// <copyright file="OrderScriptBase.cs" company="">
// Copyright 2015 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using NLog;
using System.Collections.Generic;
using System.Linq;

namespace QPAS.Scripting
{
    public abstract class OrderScriptBase
    {
        protected List<Trade> OpenTrades;
        protected ILogger Logger;
        private List<OrderScriptAction> _actions = new List<OrderScriptAction>();

        protected OrderScriptBase(DataContainer data, ILogger logger)
        {
            OpenTrades = data.Trades.Where(x => x.Open).ToList();
            Logger = logger;
        }

        /// <summary>
        /// Create a new trade with a given name.
        /// Returns the created trade.
        /// </summary>
        protected Trade CreateTrade(string name)
        {
            var newTrade = new Trade { Name = name, Open = true };
            OpenTrades.Add(newTrade);
            _actions.Add(new CreateTrade(newTrade));

            Logger.Log(LogLevel.Info, "User script {0} created trade {1}", this.GetType().Name, name);

            return newTrade;
        }

        /// <summary>
        /// Add an order to a trade with a given ID.
        /// Returns true if the trade is found and the order successfully added.
        /// </summary>
        protected bool SetTrade(Order order, int tradeID)
        {
            var trade = OpenTrades.FirstOrDefault(x => x.ID == tradeID);

            return SetTrade(order, trade);
        }

        /// <summary>
        /// Add an order to a trade with a given name.
        /// Returns true if the trade is found and the order successfully added.
        /// </summary>
        protected bool SetTrade(Order order, string tradeName)
        {
            var trade = OpenTrades.FirstOrDefault(x => x.Name == tradeName);
            return SetTrade(order, trade);
        }

        /// <summary>
        /// Add an order to a trade.
        /// Returns true if the trade is found and the order successfully added.
        /// </summary>
        protected bool SetTrade(Order order, Trade trade)
        {
            if (trade == null)
            {
                Logger.Log(LogLevel.Warn, "User script {0} tried to add order {1} to a null trade", this.GetType().Name, order);
                return false;
            }

            if (!trade.Open)
            {
                Logger.Log(LogLevel.Warn, "User script {0} tried to add order {1} to a closed trade: {2}", this.GetType().Name, order, trade);
                return false;
            }

            _actions.Add(new SetTrade(order, trade));

            Logger.Log(LogLevel.Info, "User script {0} added order {1} to trade {2}", this.GetType().Name, order, trade);

            return true;
        }

        protected void Log(string message)
        {
            Logger.Log(LogLevel.Info, message);
        }

        public abstract void ProcessOrders(List<Order> orders);

        /// <summary>
        /// Runs the user-provided script and returns to specified actions
        /// </summary>
        /// <returns></returns>
        public List<OrderScriptAction> GenerateActions(List<Order> orders)
        {
            ProcessOrders(orders);
            return _actions;
        }
    }
}