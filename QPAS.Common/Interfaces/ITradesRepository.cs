using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using EntityModel;

namespace QPAS
{
    public interface ITradesRepository
    {
        Task UpdateOpenTrades();
        void SetTags(List<Tag> tags, Trade trade);
        Task AddOrder(Trade trade, Order order, bool updateStats = true);
        Task AddOrders(Trade trade, IEnumerable<Order> order);
        Task RemoveOrder(Trade trade, Order order);
        Task AddCashTransaction(Trade trade, CashTransaction ct);
        Task RemoveCashTransaction(Trade trade, CashTransaction ct);
        Task AddFXTransaction(Trade trade, FXTransaction fxt);
        Task RemoveFXTransaction(Trade trade, FXTransaction fxt);
        Task UpdateStats(Trade trade, bool skipCollectionLoad = false);
        void SetClosingDate(Trade trade);
        Task Reset(Trade trade);
        void Dispose();

        IQueryable<Trade> Get(
            Expression<Func<Trade, bool>> filter = null,
            Func<IQueryable<Trade>, IOrderedQueryable<Trade>> orderBy = null,
            string includeProperties = "");

        void Add(Trade entity);
        void Delete(Trade entityToDelete);
        Task Save();
    }
}