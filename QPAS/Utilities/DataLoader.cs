using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace QPAS
{
    internal static class DataLoader
    {
        //at some point we may want to populate a bunch of this stuff manually and turn off the change tracking (which also disables using the same instances across queries)
        internal static async Task<DataContainer> LoadData(IContextFactory contextFactory)
        {
            var data = new DataContainer();

            using (var context = contextFactory.Get())
            {
                data.Accounts.AddRange(await context
                    .Accounts
                    .ToListAsync());

                data.Strategies.AddRange(await context
                    .Strategies
                    .ToListAsync());

                data.Instruments.AddRange(await context
                    .Instruments
                    .ToListAsync());

                data.Currencies.AddRange(await context
                    .Currencies
                    .ToListAsync());

                data.Trades.AddRange(await context.Trades
                    .Include(x => x.Strategy)
                    .Include(x => x.Orders)
                    .Include(x => x.Tags)
                    .Include("Orders.Executions")
                    .Include("Orders.Instrument")
                    .Include("Orders.Currency")
                    .Include(x => x.CashTransactions)
                    .Include("CashTransactions.Instrument")
                    .Include("CashTransactions.Currency")
                    .Include(x => x.FXTransactions)
                    .Include("FXTransactions.FXCurrency")
                    .Include("FXTransactions.FunctionalCurrency")
                    .OrderByDescending(x => x.DateOpened)
                    .ToListAsync());

                data.Tags.AddRange(await context
                    .Tags
                    .ToListAsync());

                data.CashTransactions.AddRange(await context
                    .CashTransactions
                    .Include(x => x.Trade)
                    .Include(x => x.Instrument)
                    .Include(x => x.Account)
                    .OrderByDescending(x => x.TransactionDate)
                    .ToListAsync());

                data.FXTransactions.AddRange(await context
                    .FXTransactions
                    .Include(x => x.Trade)
                    .Include(x => x.FXCurrency)
                    .Include(x => x.Account)
                    .OrderByDescending(x => x.DateTime)
                    .ToListAsync());

                data.Orders.AddRange(await context
                    .Orders
                    .OrderByDescending(z => z.TradeDate)
                    .Include(x => x.Instrument)
                    .Include(x => x.Currency)
                    .Include(x => x.CommissionCurrency)
                    .Include(x => x.Executions)
                    .Include(x => x.Account)
                    .Include(x => x.Trade)
                    .ToListAsync());

                data.EquitySummaries.AddRange(await context
                    .EquitySummaries
                    .OrderByDescending(x => x.Date)
                    .Include(x => x.Account)
                    .ToListAsync());

                data.Benchmarks.AddRange(await context.Benchmarks
                    .Include(x => x.Components)
                    .ToListAsync());

                data.FXRates.AddRange(await context.FXRates
                    .Include(x => x.FromCurrency)
                    .Include(x => x.ToCurrency)
                    .ToListAsync());

                data.DatasourcePreferences.AddRange(await context.DatasourcePreferences
                    .ToListAsync());
            }

            return data;
        }
    }
}
