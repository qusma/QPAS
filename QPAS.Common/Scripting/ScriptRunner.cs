// -----------------------------------------------------------------------
// <copyright file="ScriptRunner.cs" company="">
// Copyright 2015 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using EntityModel;
using NLog;

namespace QPAS.Scripting
{
    public class ScriptRunner
    {
        private readonly ITradesRepository _repository;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public ScriptRunner(ITradesRepository repository)
        {
            _repository = repository;
        }

        public void RunOrderScripts(List<Order> orders, IDBContext context)
        {
            var scripts = ScriptLoader.LoadOrderScripts(_repository);
            if (scripts == null) return;

            foreach(OrderScriptBase script in scripts)
            {
                try
                {
                    script.ProcessOrders(orders);
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, "User script {0} generated an exception: ", script.GetType().Name);
                    _logger.Log(LogLevel.Error, ex);
                }
            }

            context.SaveChanges();
        }

        public void RunTradeScripts(List<Trade> trades, List<Strategy> strategies, List<Tag> tags, IDBContext context)
        {
            var scripts = ScriptLoader.LoadTradeScripts(_repository, strategies, tags);
            if (scripts == null) return;

            foreach (TradeScriptBase script in scripts)
            {
                try
                {
                    script.ProcessTrades(trades);
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, "User script {0} generated an exception: ", script.GetType().Name);
                    _logger.Log(LogLevel.Error, ex);
                }
            }

            context.SaveChanges();
        }
    }
}
