using EntityModel;
using System.Threading.Tasks;

namespace QPAS.Scripting
{
    internal class OrderScriptActionExecutor
    {
        private readonly DataContainer _data;

        internal OrderScriptActionExecutor(TradesRepository tradesRepository, DataContainer data)
        {
            TradesRepository = tradesRepository;
            _data = data;
        }

        internal TradesRepository TradesRepository { get; }

        private async Task CreateTrade(Trade trade)
        {
            await TradesRepository.Add(trade).ConfigureAwait(false);
            _data.Trades.Add(trade);
        }

        private async Task SetTrade(Order order, Trade trade)
        {
            await TradesRepository.AddOrder(trade, order, true).ConfigureAwait(false);
            await TradesRepository.UpdateTrade(trade).ConfigureAwait(false);
        }

        internal async Task Execute(OrderScriptAction action)
        {
            if (action is CreateTrade ctAction)
            {
                await CreateTrade(ctAction.Trade).ConfigureAwait(false);
            }
            else if (action is SetTrade stAction)
            {
                await SetTrade(stAction.Order, stAction.Trade).ConfigureAwait(false);
            }
        }
    }
}