// -----------------------------------------------------------------------
// <copyright file="ScriptLoader.cs" company="">
// Copyright 2015 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EntityModel;
using NLog;

namespace QPAS.Scripting
{
    public static class ScriptLoader
    {
        private static List<Type> _orderScriptTypes;
        private static List<Type> _tradeScriptTypes;
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public static List<OrderScriptBase> LoadOrderScripts(ITradesRepository repository)
        {
            return LoadUserScripts<OrderScriptBase>(_orderScriptTypes, repository);
        }

        public static List<TradeScriptBase> LoadTradeScripts(ITradesRepository tradesRepo, List<Strategy> strategies, List<Tag> tags)
        {
            return LoadUserScripts<TradeScriptBase>(_tradeScriptTypes, tradesRepo, tags, strategies);
        }

        public static void LoadUserScriptTypes()
        {
            var assembly = GetUserScriptAssembly();
            if (assembly == null) return;

            try
            {
                var types = assembly.GetExportedTypes();
                _orderScriptTypes = types.Where(x => x.BaseType == typeof(OrderScriptBase)).ToList();
                _tradeScriptTypes = types.Where(x => x.BaseType == typeof(TradeScriptBase)).ToList();
            }
            catch(Exception ex)
            {
                _logger.Log(LogLevel.Error, ex);
            }
        }

        private static List<T> LoadUserScripts<T>(List<Type> types, params object[] args)
        {
            if (types == null) return null;

            var scripts = new List<T>();
            foreach (Type type in types)
            {
                _logger.Log(LogLevel.Info, "Creating user script with type {0}", type.Name);

                scripts.Add((T)Activator.CreateInstance(type, args));
            }

            return scripts;
        }

        private static Assembly GetUserScriptAssembly()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "UserScripts.dll");

            Assembly assembly;
            try
            {
                assembly = Assembly.LoadFile(path);
            }
            catch (Exception ex)
            {
                var logger = LogManager.GetCurrentClassLogger();
                logger.Log(LogLevel.Error, ex);
                return null;
            }

            return assembly;
        }
    }
}
