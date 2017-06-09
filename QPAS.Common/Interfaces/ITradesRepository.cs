using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using EntityModel;

namespace QPAS
{
    public interface ITradesRepository
    {
        void UpdateOpenTrades();
        void SetTags(List<Tag> tags, Trade trade);
        void AddOrder(Trade trade, Order order, bool updateStats = true);
        void AddOrders(Trade trade, IEnumerable<Order> order);
        void RemoveOrder(Trade trade, Order order);
        void AddCashTransaction(Trade trade, CashTransaction ct);
        void RemoveCashTransaction(Trade trade, CashTransaction ct);
        void AddFXTransaction(Trade trade, FXTransaction fxt);
        void RemoveFXTransaction(Trade trade, FXTransaction fxt);
        void UpdateStats(Trade trade, bool skipCollectionLoad = false);
        void SetClosingDate(Trade trade);
        void Reset(Trade trade);
        void Dispose();

        IQueryable<Trade> Get(
            Expression<Func<Trade, bool>> filter = null,
            Func<IQueryable<Trade>, IOrderedQueryable<Trade>> orderBy = null,
            string includeProperties = "");

        void Add(Trade entity);
        void Delete(Trade entityToDelete);
        void Save();
    }
}