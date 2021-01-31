// -----------------------------------------------------------------------
// <copyright file="ScriptRunner.cs" company="">
// Copyright 2015 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

//Here's how this system works:
//Users write scripts derived from OrderScriptBase and TradeScriptBase
//We run them, and they return a List of OrderScriptAction/TradeScriptAction,
//which represent various operations on orders/trades.
//We then take those and either display them (for testing) or apply them to the data and save to db.

using EntityModel;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace QPAS.Scripting
{
    public class ScriptRunner : IScriptRunner
    {
        private TradeScriptActionExecutor _tradeScriptExecutor;
        private OrderScriptActionExecutor _orderScriptExecutor;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly IContextFactory _contextFactory;
        private readonly DataContainer _data;

        public ScriptRunner(IContextFactory contextFactory, TradesRepository tradesRepository, DataContainer data)
        {
            _tradeScriptExecutor = new TradeScriptActionExecutor(tradesRepository, data);
            _orderScriptExecutor = new OrderScriptActionExecutor(tradesRepository, data);
            _contextFactory = contextFactory;
            _data = data;
        }

        public async Task RunOrderScript(UserScript script, List<Order> orders)
        {
            if (script.Type != UserScriptType.OrderScript) throw new ArgumentException("Wrong type of script.");

            var actions = await GetOrderScriptActions(script, orders).ConfigureAwait(false);
            await ExecuteOrderActions(actions).ConfigureAwait(false);
        }

        public async Task RunTradeScript(UserScript script, List<Trade> trades = null)
        {
            if (script.Type != UserScriptType.TradeScript) throw new ArgumentException("Wrong type of script.");

            var actions = await GetTradeScriptActions(script, trades).ConfigureAwait(false);
            await ExecuteTradeActions(actions).ConfigureAwait(false);
        }

        private async Task ExecuteTradeActions(List<TradeScriptAction> actions)
        {
            foreach (var action in actions)
            {
                await _tradeScriptExecutor.Execute(action);
            }
        }

        private async Task ExecuteOrderActions(List<OrderScriptAction> actions)
        {
            foreach (var action in actions)
            {
                await _orderScriptExecutor.Execute(action);
            }
        }

        /// <summary>
        /// Run script and get the actions it generates
        /// </summary>
        /// <param name="script"></param>
        /// <param name="trades">If trades are not provided, all open trades are used.</param>
        /// <returns></returns>
        public async Task<List<TradeScriptAction>> GetTradeScriptActions(UserScript script, List<Trade> trades = null)
        {
            var scriptToExecute = script.Code + @"
                var script = new " + script.Name + @"(Data, Logger);
                return script.GenerateActions(OpenTrades);";

            var globals = new Globals { Data = _data, Logger = _logger, OpenTrades = trades ?? _data.Trades.Where(x => x.Open).ToList() };
            var options = ScriptOptions.Default
                .WithReferences(Assembly.GetExecutingAssembly());

            return await CSharpScript.EvaluateAsync<List<TradeScriptAction>>(scriptToExecute, options, globals).ConfigureAwait(false);
        }

        public async Task<List<OrderScriptAction>> GetOrderScriptActions(UserScript script, List<Order> orders)
        {
            var scriptToExecute = script.Code + @"
                var script = new " + script.Name + @"(Data, Logger);
                return script.GenerateActions(Orders);";

            var globals = new Globals { Data = _data, Orders = orders, Logger = _logger };
            var options = ScriptOptions.Default
                .WithReferences(Assembly.GetExecutingAssembly());

            return await CSharpScript.EvaluateAsync<List<OrderScriptAction>>(scriptToExecute, options, globals).ConfigureAwait(false);
        }
    }

    public class Globals
    {
        public DataContainer Data { get; set; }

        public List<Order> Orders { get; set; }
        public List<Trade> OpenTrades { get; set; }

        public ILogger Logger { get; set; }
    }
}