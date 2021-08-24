using EntityModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QPAS
{
    public interface ITradesRepository
    {
        Task Add(Trade trade);
        Task AddCashTransaction(Trade trade, CashTransaction ct, bool updateStats = true, bool save = true);
        Task AddCashTransactions(Trade trade, IEnumerable<CashTransaction> cashTransactions);
        Task AddFXTransaction(Trade trade, FXTransaction fxt);
        Task AddOrder(Trade trade, Order order, bool updateStats = true);
        Task AddOrders(Trade trade, IEnumerable<Order> orders);
        Task AddTags(Trade trade, List<Tag> tags);
        Task RemoveCashTransaction(Trade trade, CashTransaction ct, bool save = true);
        Task RemoveFXTransaction(Trade trade, FXTransaction fxt);
        Task RemoveOrder(Trade trade, Order order);
        Task Reset(Trade trade);
        void SetClosingDate(Trade trade);
        Task SetTag(Trade trade, Tag tag, bool add);
        Task UpdateOpenTrades(DataContainer data);
        Task UpdateStats(Trade trade);
        Task UpdateTrade(Trade trade = null, IEnumerable<Trade> trades = null);
    }
}