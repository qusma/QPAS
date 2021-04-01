using EntityModel;
using NLog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace QPAS
{
    internal static class DataImporter
    {
        internal static async Task Import(DataContainer existingData, DataContainer newData, string accountId, IContextFactory contextFactory, TradesRepository tradesRepo)
        {
            using (var dbContext = contextFactory.Get())
            {
                //Accounts
                foreach (var newAccount in newData.Accounts)
                {
                    if (!existingData.Accounts.Any(x => x.AccountId == newAccount.AccountId))
                    {
                        dbContext.Accounts.Add(newAccount);
                        existingData.Accounts.Add(newAccount);
                    }
                    else
                    {
                        dbContext.Attach(existingData.Accounts.First(x => x.AccountId == newAccount.AccountId));
                    }
                }

                var selectedAccount = existingData.Accounts.First(x => x.AccountId == accountId);

                //then we have to attach the existing currencies to prevent attempts at re-insertion
                foreach (var currency in existingData.Currencies)
                {
                    dbContext.Attach(currency);
                }

                var currencyDict = existingData.Currencies.ToDictionary(x => x.Name, x => x);

                //attach existing instruments
                foreach (var instrument in existingData.Instruments)
                {
                    dbContext.Attach(instrument);
                }

                //then add new instruments
                foreach (var newInstrument in newData.Instruments)
                {
                    if (!existingData.Instruments.Any(x => x.ConID == newInstrument.ConID))
                    {
                        dbContext.Instruments.Add(newInstrument);
                        existingData.Instruments.Add(newInstrument);
                    }
                }

                await dbContext.SaveChangesAsync().ConfigureAwait(false);

                //Cash Transactions
                var latestExistingCashTransaction = dbContext.CashTransactions.Where(x => x.AccountID == selectedAccount.ID).OrderByDescending(x => x.TransactionDate).FirstOrDefault();
                var latestDate = latestExistingCashTransaction == null ? new DateTime(1970, 1, 1) : latestExistingCashTransaction.TransactionDate;

                foreach (var newCashTransaction in newData.CashTransactions)
                {
                    if (newCashTransaction.TransactionDate <= latestDate) continue;

                    newCashTransaction.Account = selectedAccount;
                    newCashTransaction.Currency = currencyDict[newCashTransaction.CurrencyString];
                    newCashTransaction.Instrument = existingData.Instruments.FirstOrDefault(x => x.ConID == newCashTransaction.ConID);

                    dbContext.CashTransactions.Add(newCashTransaction);
                    existingData.CashTransactions.Add(newCashTransaction);
                }

                //Orders + Executions
                DateTime lastDate = dbContext.Orders.Any(x => x.AccountID == selectedAccount.ID)
                    ? dbContext.Orders.Where(x => x.AccountID == selectedAccount.ID).Max(x => x.TradeDate)
                    : new DateTime(1970, 1, 1);

                foreach (var newOrder in newData.Orders)
                {
                    if (newOrder.TradeDate <= lastDate) continue;

                    if (newOrder.AssetCategory == AssetClass.Cash)
                    {
                        //These are currency trades. But currencies aren't provided in the SecuritiesInfos
                        //So we have to hack around it and add the currency as an instrument "manually" if it's not in yet
                        newOrder.Instrument = TryAddAndGetCurrencyInstrument(newOrder, existingData, dbContext);
                    }
                    else
                    {
                        newOrder.Instrument = existingData.Instruments.FirstOrDefault(x => x.ConID == newOrder.ConID);
                    }

                    newOrder.Account = selectedAccount;
                    newOrder.Currency = currencyDict[newOrder.CurrencyString];
                    newOrder.CommissionCurrency = currencyDict[newOrder.CommissionCurrencyString];

                    //then the executions in that order
                    foreach (var exec in newOrder.Executions)
                    {
                        exec.Account = selectedAccount;
                        exec.Instrument = existingData.Instruments.FirstOrDefault(x => x.ConID == exec.ConID);
                        exec.Currency = currencyDict[exec.CurrencyString];
                        exec.CommissionCurrency = currencyDict[exec.CommissionCurrencyString];
                    }

                    //and finally add
                    existingData.Orders.Add(newOrder);
                    existingData.Executions.AddRange(newOrder.Executions);
                    dbContext.Orders.Add(newOrder);
                }

                //Equity Summaries
                foreach (var eq in newData.EquitySummaries)
                {
                    eq.Account = selectedAccount;

                    if (existingData.EquitySummaries.Count(x => x.Date == eq.Date && x.AccountID == selectedAccount.ID) == 0)
                    {
                        dbContext.EquitySummaries.Add(eq);
                        existingData.EquitySummaries.Add(eq);
                    }
                }

                //Open Positions
                //start by deleting the old ones
                var toRemove = dbContext.OpenPositions.Where(x => x.AccountID == selectedAccount.ID).ToList();
                dbContext.OpenPositions.RemoveRange(toRemove);

                //then add the new ones
                foreach (var op in newData.OpenPositions)
                {
                    op.Account = selectedAccount;
                    op.Instrument = existingData.Instruments.FirstOrDefault(x => x.ConID == op.ConID);
                    op.Currency = currencyDict[op.CurrencyString];

                    dbContext.OpenPositions.Add(op);
                }

                //FX Rates
                foreach (var fxRate in newData.FXRates)
                {
                    fxRate.FromCurrency = currencyDict[fxRate.FromCurrencyString];
                    fxRate.ToCurrency = currencyDict[fxRate.ToCurrencyString];

                    if (!existingData.FXRates.Any(x =>
                            x.FromCurrency.ID == fxRate.ID &&
                            x.ToCurrency.ID == fxRate.ToCurrency.ID &&
                            x.Date == fxRate.Date))
                    {
                        dbContext.FXRates.Add(fxRate);
                        existingData.FXRates.Add(fxRate);
                    }
                }

                //Prior Positions
                lastDate = dbContext.PriorPositions.Any(x => x.AccountID == selectedAccount.ID)
                    ? dbContext.PriorPositions.Where(x => x.AccountID == selectedAccount.ID).Max(x => x.Date)
                    : new DateTime(1, 1, 1);

                foreach (var priorPosition in newData.PriorPositions)
                {
                    if (priorPosition.Date > lastDate)
                    {
                        priorPosition.Account = selectedAccount;
                        priorPosition.Currency = currencyDict[priorPosition.CurrencyString];
                        priorPosition.Instrument = existingData.Instruments.FirstOrDefault(x => x.ConID == priorPosition.ConID);

                        dbContext.PriorPositions.Add(priorPosition);
                        existingData.PriorPositions.Add(priorPosition);
                    }
                }

                //Open Dividend Accruals
                //delete and then add the new ones

                dbContext.DividendAccruals.RemoveRange(dbContext.DividendAccruals.Where(x => x.AccountID == selectedAccount.ID).ToList());
                existingData.DividendAccruals.Clear();

                foreach (var dividendAccrual in newData.DividendAccruals)
                {
                    dividendAccrual.Currency = currencyDict[dividendAccrual.CurrencyString];
                    dividendAccrual.Instrument = existingData.Instruments.FirstOrDefault(x => x.ConID == dividendAccrual.ConID);
                    dividendAccrual.Account = selectedAccount;

                    if (dividendAccrual.Instrument == null)
                    {
                        var logger = LogManager.GetCurrentClassLogger();
                        logger.Log(LogLevel.Error, "Could not find instrument for dividend accrual with conid: " + dividendAccrual.ConID);
                    }
                    else
                    {
                        dbContext.DividendAccruals.Add(dividendAccrual);
                        existingData.DividendAccruals.Add(dividendAccrual);
                    }
                }

                //FX Positions
                //remove existing then add fresh

                dbContext.FXPositions.RemoveRange(dbContext.FXPositions.Where(x => x.AccountID == selectedAccount.ID).ToList());

                foreach (var fxPosition in newData.FXPositions)
                {
                    fxPosition.FunctionalCurrency = currencyDict[fxPosition.FunctionalCurrencyString];
                    fxPosition.FXCurrency = currencyDict[fxPosition.FXCurrencyString];
                    fxPosition.Account = selectedAccount;

                    dbContext.FXPositions.Add(fxPosition);
                }

                //FX Transactions
                lastDate = dbContext.FXTransactions.Any(x => x.AccountID == selectedAccount.ID)
                                        ? dbContext.FXTransactions.Where(x => x.AccountID == selectedAccount.ID).Max(x => x.DateTime)
                                        : new DateTime(1, 1, 1);

                foreach (var fxTransaction in newData.FXTransactions)
                {
                    if (fxTransaction.DateTime > lastDate)
                    {
                        fxTransaction.FunctionalCurrency = currencyDict[fxTransaction.FunctionalCurrencyString];
                        fxTransaction.FXCurrency = currencyDict[fxTransaction.FXCurrencyString];
                        fxTransaction.Account = selectedAccount;

                        dbContext.FXTransactions.Add(fxTransaction);
                        existingData.FXTransactions.Add(fxTransaction);
                    }
                }

                await tradesRepo.UpdateOpenTrades(existingData); //todo: this is slow, perhaps parallelize? qdmsclient probably doesn't like that, would need to make it thread-safe
                await dbContext.SaveChangesAsync();
            }
        }

        private static Instrument TryAddAndGetCurrencyInstrument(Order order, DataContainer data, IQpasDbContext dbContext)
        {
            var instrument =
                data
                .Instruments
                .FirstOrDefault(x => x.ConID == order.ConID);

            if (instrument != null) return instrument; //it's already in the DB, no need to add it

            //otherwise construct a new instrument, add it, and return it.
            instrument = new Instrument
            {
                Symbol = order.SymbolString,
                UnderlyingSymbol = order.SymbolString,
                Description = order.SymbolString,
                ConID = order.ConID,
                AssetCategory = AssetClass.Cash,
                Multiplier = 1
            };

            dbContext.Instruments.Add(instrument);
            data.Instruments.Add(instrument);

            return instrument;
        }
    }
}