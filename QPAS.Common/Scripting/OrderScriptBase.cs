// -----------------------------------------------------------------------
// <copyright file="OrderScriptBase.cs" company="">
// Copyright 2015 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using EntityModel;
using NLog;

namespace QPAS.Scripting
{
    public abstract class OrderScriptBase
    {
        protected List<Trade> OpenTrades;
        protected ITradesRepository TradesRepository;
        protected Logger Logger = LogManager.GetCurrentClassLogger();

        protected OrderScriptBase(ITradesRepository tradesRepository)
        {
            OpenTrades = tradesRepository.Get(x => x.Open).ToList();
            TradesRepository = tradesRepository;
        }

        /// <summary>
        /// Create a new trade with a given name.
        /// Returns the created trade.
        /// </summary>
        protected Trade CreateTrade(string name)
        {
            var trade = new Trade() { Name = name, Open = true, Tags = new List<Tag>() };
            TradesRepository.Add(trade);
            OpenTrades.Add(trade);

            Logger.Log(LogLevel.Info, "User script {0} created trade {1}", this.GetType().Name, trade);

            return trade;
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
            if(trade == null)
            {
                Logger.Log(LogLevel.Warn, "User script {0} tried to add order {1} to a null trade", this.GetType().Name, order);
                return false;
            }

            if(!trade.Open)
            {
                Logger.Log(LogLevel.Warn, "User script {0} tried to add order {1} to a closed trade: {2}", this.GetType().Name, order, trade);
                return false;
            }

            TradesRepository.AddOrder(trade, order);
            
            Logger.Log(LogLevel.Info, "User script {0} added order {1} to trade {2}", this.GetType().Name, order, trade);

            return true;
        }

        public abstract void ProcessOrders(List<Order> orders);
    }
}
