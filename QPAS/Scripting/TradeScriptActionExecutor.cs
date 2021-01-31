using EntityModel;
using NLog;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace QPAS.Scripting
{
    internal class TradeScriptActionExecutor
    {
        protected IReadOnlyList<Tag> Tags;
        protected IReadOnlyList<Strategy> Strategies;
        protected Logger Logger = LogManager.GetCurrentClassLogger();
        internal TradesRepository TradesRepository { get; }

        internal TradeScriptActionExecutor(TradesRepository tradesRepository, DataContainer data)
        {
            TradesRepository = tradesRepository;
            Strategies = data.Strategies.ToList();
            Tags = data.Tags.ToList();
        }

        /// <summary>
        /// Add a tag to a trade.
        /// </summary>
        internal async Task SetTag(Trade trade, Tag tag)
        {
            if (trade.Tags == null)
            {
                trade.Tags = new ObservableCollection<Tag>();
            }

            trade.Tags.Add(tag);
            trade.TagStringUpdated();

            await TradesRepository.UpdateTrade(trade).ConfigureAwait(false);
        }

        /// <summary>
        /// Set a trade's strategy.
        /// </summary>
        internal async Task SetStrategy(Trade trade, Strategy strategy)
        {
            trade.Strategy = strategy;
            await TradesRepository.UpdateTrade(trade).ConfigureAwait(false);

            Logger.Log(LogLevel.Info, "User script executor set strategy of trade {0} to {1}", trade, strategy);
        }

        /// <summary>
        /// Close a trade.
        /// </summary>
        internal async Task CloseTrade(Trade trade)
        {
            if (!trade.Open)
            {
                Logger.Log(LogLevel.Info, "User script tried to close trade {0} but it was already closed", trade);
                return;
            }

            if (!trade.IsClosable())
            {
                Logger.Log(LogLevel.Info, "User scripttried to close trade {0} but the trade can not be closed", trade);
                return;
            }

            await TradesRepository.CloseTrades(new List<Trade> { trade }).ConfigureAwait(false);
        }

        internal async Task Execute(TradeScriptAction action)
        {
            if (action is CloseTrade ctAction)
            {
                await CloseTrade(ctAction.Trade).ConfigureAwait(false);
            }
            else if (action is SetTag stAction)
            {
                await SetTag(stAction.Trade, stAction.Tag).ConfigureAwait(false);
            }
            else if (action is SetStrategy ssAction)
            {
                await SetStrategy(ssAction.Trade, ssAction.Strategy).ConfigureAwait(false);
            }
        }
    }
}