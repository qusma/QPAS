// -----------------------------------------------------------------------
// <copyright file="ScriptRunner.cs" company="">
// Copyright 2015 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using QPAS.Scripting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QPAS
{
    public interface IScriptRunner
    {
        Task<List<OrderScriptAction>> GetOrderScriptActions(UserScript script, List<Order> orders);
        Task<List<TradeScriptAction>> GetTradeScriptActions(UserScript script, List<Trade> trades = null);
        Task RunOrderScript(UserScript script, List<Order> orders);
        Task RunTradeScript(UserScript script, List<Trade> trades = null);
    }
}