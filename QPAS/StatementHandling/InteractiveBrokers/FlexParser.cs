// -----------------------------------------------------------------------
// <copyright file="FlexParser.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Xml.Linq;
using EntityModel;
using MahApps.Metro.Controls.Dialogs;
using NLog;
using NXmlMapper;

namespace QPAS
{
    [Export(typeof(IStatementParser))]
    [ExportMetadata("Name", "Interactive Brokers")]
    public class FlexParser : IStatementParser
    {
        public string Name { get { return "Interactive Brokers"; } }

        public void Parse(string flexXML, ProgressDialogController progress)
        {
            progress.SetMessage("Parsing Flex File");
            var context = new DBContext();
            context.Configuration.AutoDetectChangesEnabled = false;

            bool skipLastDateCheck = !Properties.Settings.Default.preventDuplicateImports;

            int totalActions = 12;

            XDocument xml = XDocument.Parse(flexXML);

            progress.SetProgress(1.0 / totalActions);
            ParseSecuritiesInfo(xml, context);

            DateTime lastDate =
                context.CashTransactions.Any()
                ? context.CashTransactions.Max(x => x.TransactionDate)
                : new DateTime(1, 1, 1);

            progress.SetProgress(2.0 / totalActions);
            ParseCashTransactions(xml, context, skipLastDateCheck, lastDate);

            progress.SetProgress(3.0 / totalActions);
            ParseCFDCharges(xml, context, skipLastDateCheck, lastDate);

            progress.SetProgress(4.0 / totalActions);
            ParseOrders(xml, context, skipLastDateCheck);

            progress.SetProgress(5.0 / totalActions);
            ParseExecutions(xml, context, skipLastDateCheck);

            progress.SetProgress(6.0 / totalActions);
            ParseEquitySummaries(xml, context);

            progress.SetProgress(7.0 / totalActions);
            ParseOpenPositions(xml, context);

            progress.SetProgress(8.0 / totalActions);
            ParseFXRates(xml, context);

            progress.SetProgress(9.0 / totalActions);
            ParsePriorPeriodPositions(xml, context, skipLastDateCheck);

            progress.SetProgress(10.0 / totalActions);
            ParseOpenDividendAccruals(xml, context);

            progress.SetProgress(11.0 / totalActions);
            ParseFXPositions(xml, context);

            progress.SetProgress(12.0 / totalActions);
            ParseFXTransactions(xml, context, skipLastDateCheck);

            context.Configuration.AutoDetectChangesEnabled = true;
            context.Dispose();
        }

        public string GetFileFilter()
        {
            return "Flex Files (*.xml)|*.xml";
        }

        private static void ParseFXTransactions(XDocument xml, IDBContext context, bool skipLastDateCheck)
        {
            var fxTransactionMapper = new Mapper<FXTransaction>(xml.Descendants("FxTransaction"));
            List<FXTransaction> fxTransactions = fxTransactionMapper.ParseAll();

            DateTime lastDate = context.FXTransactions.Any()
                ? context.FXTransactions.Max(x => x.DateTime)
                : new DateTime(1, 1, 1);

            var currencies = context.Currencies.ToList();

            //then add the new ones
            foreach (FXTransaction i in fxTransactions)
            {
                if (i.DateTime > lastDate || skipLastDateCheck)
                {
                    i.FunctionalCurrency = currencies.FirstOrDefault(x => x.Name == i.FunctionalCurrencyString);
                    i.FXCurrency = currencies.FirstOrDefault(x => x.Name == i.FXCurrencyString);
                    context.FXTransactions.Add(i);
                }
            }
            context.SaveChanges();

            //<FxTransaction accountId="U1066712" acctAlias="" assetCategory="CASH" reportDate="20130401"
            //functionalCurrency="USD" fxCurrency="CAD" activityDescription="XRE() DIVIDEND .06726 CAD PER SHARE"
            //dateTime="20130328;202000" quantity="16.29" proceeds="16.034084" cost="-16.034084" realizedPL="0"
            //code="O" levelOfDetail="TRANSACTION" />
        }

        private static void ParseFXPositions(XDocument xml, IDBContext context)
        {
            var fxPositionsMapper = new Mapper<FXPosition>(xml.Descendants("FxPosition"));
            List<FXPosition> fxPositions = fxPositionsMapper.ParseAll();

            //delete all of them
            context.FXPositions.RemoveRange(context.FXPositions.ToList());

            //then add the new ones
            foreach (FXPosition i in fxPositions)
            {
                i.FunctionalCurrency = context.Currencies.FirstOrDefault(x => x.Name == i.FunctionalCurrencyString);
                i.FXCurrency = context.Currencies.FirstOrDefault(x => x.Name == i.FXCurrencyString);
                context.FXPositions.Add(i);
            }
            context.SaveChanges();

            //<FxPosition accountId="U1066712" acctAlias="" assetCategory="CASH" reportDate="20140325"
            //functionalCurrency="USD" fxCurrency="CAD" quantity="22.379966" costPrice="0.890970031" costBasis="-19.939879"
            //closePrice="0.89552" value="20.041707" unrealizedPL="0.101828" code="" lotDescription="" lotOpenDateTime="" levelOfDetail="SUMMARY" />
        }

        private static void ParseOpenDividendAccruals(XDocument xml, IDBContext context)
        {
            var openDividendAccrualsMapper = new Mapper<DividendAccrual>(xml.Descendants("OpenDividendAccrual"));
            List<DividendAccrual> dividendAccruals = openDividendAccrualsMapper.ParseAll();

            //delete all of them
            context.DividendAccruals.RemoveRange(context.DividendAccruals.ToList());

            //then add the new ones
            foreach (DividendAccrual i in dividendAccruals)
            {
                i.Currency = context.Currencies.FirstOrDefault(x => x.Name == i.CurrencyString);
                i.Instrument = context.Instruments.FirstOrDefault(x => x.ConID == i.ConID);
                if (i.Instrument == null)
                {
                    var logger = LogManager.GetCurrentClassLogger();
                    logger.Log(LogLevel.Error, "Could not find instrument for dividend accrual with conid: " + i.ConID);
                }
                else
                {
                    context.DividendAccruals.Add(i);
                }
            }
            context.SaveChanges();

            //<OpenDividendAccrual accountId="U1066712" currency="USD" assetCategory="STK" fxRateToBase="1" symbol="PICB"
            //description="POWERSHARES INT CORP BOND" conid="75980548" securityID="" securityIDType="" cusip="" isin=""
            //underlyingConid="" underlyingSymbol="" issuer="" exDate="2013-01-15" payDate="2013-01-31" quantity="19" tax="0.44"
            //fee="0" grossRate="0.07613" grossAmount="1.45" netAmount="1.01" code="" fromAcct="" toAcct="" />
        }

        private static void ParsePriorPeriodPositions(XDocument xml, IDBContext context, bool skipLastDateCheck)
        {
            var priorPeriodPositionsMapper = new Mapper<PriorPosition>(xml.Descendants("PriorPeriodPosition"));
            List<PriorPosition> priorPeriodPositions = priorPeriodPositionsMapper.ParseAll();

            DateTime lastDate = context.PriorPositions.Any()
                ? context.PriorPositions.Max(x => x.Date)
                : new DateTime(1, 1, 1);

            var currencies = context.Currencies.ToList();
            var instruments = context.Instruments.ToList();

            foreach (PriorPosition i in priorPeriodPositions)
            {
                if (skipLastDateCheck || i.Date > lastDate)
                {
                    i.Currency = currencies.FirstOrDefault(x => x.Name == i.CurrencyString);
                    i.Instrument = instruments.FirstOrDefault(x => x.ConID == i.ConID);
                    context.PriorPositions.Add(i);
                }
            }
            context.SaveChanges();

            //<PriorPeriodPosition accountId="U1066712" currency="USD" assetCategory="STK" fxRateToBase="1" symbol="ACWV"
            //description="ISHARES MSCI ALL COUNTRY WOR" conid="96090060" securityID="" securityIDType="" cusip="" isin=""
            //underlyingConid="" underlyingSymbol="" issuer="" date="2012-12-28" price="55.23" priorMtmPnl="-9" />
        }

        private static void ParseFXRates(XDocument xml, IDBContext context)
        {
            var fxRatesMapper = new Mapper<FXRate>(xml.Descendants("ConversionRate"));
            List<FXRate> fxRates = fxRatesMapper.ParseAll();

            var currencies = context.Currencies.ToList();

            foreach (FXRate i in fxRates)
            {
                i.FromCurrency = currencies.FirstOrDefault(x => x.Name == i.FromCurrencyString);
                i.ToCurrency = currencies.FirstOrDefault(x => x.Name == i.ToCurrencyString);

                if (!context.FXRates.Any(x =>
                    x.FromCurrency.ID == i.FromCurrency.ID &&
                    x.ToCurrency.ID == i.ToCurrency.ID &&
                    x.Date == i.Date))
                {
                    context.FXRates.Add(i);
                }
            }
            context.SaveChanges();

            //<ConversionRate reportDate="2012-12-28" fromCurrency="CHF" toCurrency="USD" rate="1.0947" />
        }

        private static void ParseOpenPositions(XDocument xml, IDBContext context)
        {
            if (!xml.Descendants("OpenPositions").Any()) return;
            var openPositionsMapper = new Mapper<OpenPosition>(xml.Descendants("OpenPosition").Where(x => x.Attribute("levelOfDetail").Value == "SUMMARY"));
            List<OpenPosition> openPositions = openPositionsMapper.ParseAll();

            //start by deleting the old ones
            context.OpenPositions.RemoveRange(context.OpenPositions.ToList());
            context.SaveChanges();

            //then add the new ones
            foreach (OpenPosition i in openPositions)
            {
                i.Instrument = context.Instruments.FirstOrDefault(x => x.ConID == i.ConID);
                i.Currency = context.Currencies.FirstOrDefault(x => x.Name == i.CurrencyString);

                context.OpenPositions.Add(i);
            }
            context.SaveChanges();

            //<OpenPosition accountId="U1066712" currency="USD" assetCategory="STK" fxRateToBase="1" symbol="ACWV"
            //description="ISHARES MSCI ALL COUNTRY WOR" conid="96090060" securityID="" securityIDType="" cusip=""
            //isin="" underlyingConid="" underlyingSymbol="" issuer="" reportDate="20130131" position="18" multiplier="1"
            //markPrice="58.01" positionValue="1044.18" openPrice="55.877777778" costBasisPrice="55.877777778"
            //costBasisMoney="1005.8" percentOfNAV="1.61" fifoPnlUnrealized="38.38" side="Long" @levelOfDetaillevelOfDetail="SUMMARY"
            //openDateTime="" holdingPeriodDateTime="" code="" originatingOrderID="" />
        }

        private static void ParseEquitySummaries(XDocument xml, IDBContext context)
        {
            var equitySummaryMapper = new Mapper<EquitySummary>(xml.Descendants("EquitySummaryByReportDateInBase"));
            List<EquitySummary> equitySummaries = equitySummaryMapper.ParseAll();

            foreach (EquitySummary i in equitySummaries)
            {
                if (context.EquitySummaries.Count(x => x.Date == i.Date) == 0)
                {
                    context.EquitySummaries.Add(i);
                    context.SaveChanges();
                }
            }

            //<EquitySummaryByReportDateInBase accountId="U1066712" reportDate="2012-07-18" cash="-8839.601715" cashLong="0"
            //cashShort="-8839.601715" slbCashCollateral="0" slbCashCollateralLong="0" slbCashCollateralShort="0"
            //stock="38134.228955" stockLong="38134.228955" stockShort="0" slbDirectSecuritiesBorrowed="0"
            //slbDirectSecuritiesBorrowedLong="0" slbDirectSecuritiesBorrowedShort="0" slbDirectSecuritiesLent="0"
            //slbDirectSecuritiesLentLong="0" slbDirectSecuritiesLentShort="0" options="235.75" optionsLong="235.75"
            //optionsShort="0" commodities="0" commoditiesLong="0" commoditiesShort="0" bonds="0" bondsLong="0" bondsShort="0"
            //notes="0" notesLong="0" notesShort="0" interestAccruals="-7.2318305" interestAccrualsLong="0"
            //interestAccrualsShort="-7.2318305" softDollars="0" softDollarsLong="0" softDollarsShort="0" dividendAccruals="0"
            //dividendAccrualsLong="0" dividendAccrualsShort="0" total="29523.1454095" totalLong="38369.978955" totalShort="-8846.8335455" />
        }

        private static void ParseExecutions(XDocument xml, IDBContext context, bool skipLastDateCheck)
        {
            if (!xml.Descendants("Trades").Any()) return;

            var tradesMapper = new Mapper<Execution>(xml.Descendants("Trades").First().Descendants("Trade"));
            List<Execution> executions = tradesMapper.ParseAll();

            DateTime lastDate = context.Executions.Any()
                ? context.Executions.Max(x => x.TradeDate)
                : new DateTime(1, 1, 1);

            var currencies = context.Currencies.ToList();
            var instruments = context.Instruments.ToList();

            //then add the new ones
            foreach (Execution i in executions)
            {
                if (i.TradeDate > lastDate || skipLastDateCheck)
                {
                    i.Instrument = instruments.FirstOrDefault(x => x.ConID == i.ConID);
                    i.Currency = currencies.FirstOrDefault(x => x.Name == i.CurrencyString);
                    i.CommissionCurrency = currencies.FirstOrDefault(x => x.Name == i.CommissionCurrencyString);
                    i.Order = context.Orders.FirstOrDefault(x => x.IBOrderID == i.IBOrderID);
                    context.Executions.Add(i);
                }
            }
            context.SaveChanges();

            //<Trade accountId="U1066712" currency="USD" assetCategory="STK" fxRateToBase="1" symbol="VGK"
            //description="VANGUARD MSCI EUROPEAN ETF" conid="27684070" securityID="" securityIDType="" cusip="" isin=""
            //underlyingConid="" underlyingSymbol="" issuer="" tradeID="812956946" reportDate="20121231" tradeDate="20121231"
            //tradeTime="160000" settleDateTarget="20130104" transactionType="ExchTrade" exchange="ARCA" quantity="-60" tradePrice="48.84"
            //multiplier="1" tradeMoney="-2930.4" proceeds="2930.4" taxes="0" ibCommission="-1" ibCommissionCurrency="USD" closePrice="48.84"
            //openCloseIndicator="C" notes="P;" cost="-2869.2" fifoPnlRealized="60.2" mtmPnl="0" origTradePrice="0" origTradeDate=""
            //origTradeID="" origOrderID="0" strike="" expiry="" putCall="" buySell="SELL" ibOrderID="415554439"
            //ibExecID="0000d3de.50e1a59d.01.01" brokerageOrderID="" orderReference="" volatilityOrderLink=""
            //orderPlacementTime="" clearingFirmID="" exchOrderId="N/A" extExecID="AD_5629512420665350" orderTime="20121231;142412"
            //openDateTime="--" holdingPeriodDateTime="--" whenRealized="--" whenReopened="--" levelOfDetail="EXECUTION"
            //changeInPrice="0" changeInQuantity="0" netCash="2929.4" orderType="MOC" />
        }

        private static void ParseOrders(XDocument xml, IDBContext context, bool skipLastDateCheck)
        {
            if (!xml.Descendants("Trades").Any()) return;

            var ordersMapper = new Mapper<Order>(xml.Descendants("Trades").First().Descendants("Order"));
            List<Order> orders = ordersMapper.ParseAll();

            DateTime lastDate = context.Orders.Any()
                ? context.Orders.Max(x => x.TradeDate)
                : new DateTime(1, 1, 1);

            var instruments = context.Instruments.ToList();
            var currencies = context.Currencies.ToList();

            //then add the new ones
            foreach (Order order in orders)
            {
                if (order.TradeDate > lastDate || skipLastDateCheck)
                {
                    order.IsReal = true;
                    if(order.AssetCategory == AssetClass.Cash)
                    {
                        //These are currency trades. But currencies aren't provided in the SecuritiesInfos
                        //So we have to hack around it and add the currency as an instrument "manually" if it's not in yet
                        order.Instrument = TryAddAndGetCurrencyInstrument(order, context);
                    }
                    else
                    {
                        order.Instrument = instruments.FirstOrDefault(x => x.ConID == order.ConID);
                    }
                    
                    order.Currency = currencies.FirstOrDefault(x => x.Name == order.CurrencyString);
                    order.CommissionCurrency = currencies.FirstOrDefault(x => x.Name == order.CommissionCurrencyString);
                    context.Orders.Add(order);
                }
            }
            context.SaveChanges();

            //<Order accountId="U1066712" currency="USD" assetCategory="STK" fxRateToBase="1" symbol="AAPL"
            //description="APPLE INC" conid="265598" securityID="" securityIDType="" cusip="" isin=""
            //underlyingConid="" underlyingSymbol="" issuer="" tradeID="--" reportDate="20140325" tradeDate="20140325"
            //tradeTime="093002" settleDateTarget="20140328" transactionType="--" exchange="--" quantity="-48"
            //tradePrice="541.37" multiplier="1" tradeMoney="-25985.76" proceeds="25985.76" taxes="0" ibCommission="-1.574285"
            //ibCommissionCurrency="USD" closePrice="544.99" openCloseIndicator="C" notes="C" cost="-25877.8" fifoPnlRealized="106.385715"
            //mtmPnl="-173.76" origTradePrice="--" origTradeDate="--" origTradeID="--" origOrderID="--" strike="" expiry="" putCall=""
            //buySell="SELL" ibOrderID="537171278" ibExecID="--" brokerageOrderID="--" orderReference="--" volatilityOrderLink="--"
            //orderPlacementTime="--" clearingFirmID="--" exchOrderId="--" extExecID="--" orderTime="20140325;093002" openDateTime="--"
            //holdingPeriodDateTime="--" whenRealized="--" whenReopened="--" levelOfDetail="ORDER" changeInPrice="--" changeInQuantity="--"
            //netCash="25984.185715" orderType="LMT" />
        }

        private static Instrument TryAddAndGetCurrencyInstrument(Order order, IDBContext context)
        {
            var instrument =
                context
                .Instruments
                .FirstOrDefault(x => x.AssetCategory == AssetClass.Cash && x.Symbol == order.SymbolString);

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
            //todo some are USD.CAD, others EUR.USD....how?
            //Change in asset price leaves us with a currency position seemingly out of nowhere... actually no it doesn't, there are Fx transactions on both sides

            context.Instruments.Add(instrument);
            context.SaveChanges();
            return instrument;
        }

        private static void ParseCFDCharges(XDocument xml, IDBContext context, bool skipLastDateCheck, DateTime lastDate)
        {
            var cfdTransactionsMapper = new Mapper<CashTransaction>(xml.Descendants("CFDCharge"));
            cfdTransactionsMapper.SetAttributeMap("total", "Amount");
            cfdTransactionsMapper.SetAttributeMap("date", "TransactionDate", "yyyy-MM-dd");
            List<CashTransaction> cfdCharges = cfdTransactionsMapper.ParseAll();

            var instruments = context.Instruments.ToList();
            var currencies = context.Currencies.ToList();

            foreach (CashTransaction i in cfdCharges)
            {
                i.Type = "CFD Charge";

                if (skipLastDateCheck || i.TransactionDate > lastDate)
                {
                    i.Currency = currencies.FirstOrDefault(x => x.Name == i.CurrencyString);
                    i.Instrument = instruments.FirstOrDefault(x => x.ConID == i.ConID);
                    context.CashTransactions.Add(i);
                }
            }
            context.SaveChanges();

            //<CFDCharge accountId="U1066712F" currency="USD" assetCategory="CFD" fxRateToBase="1"
            //symbol="--" description="--" conid="--" securityID="--" securityIDType="--" cusip="--"
            //isin="--" underlyingConid="--" underlyingSymbol="--" issuer="--" date="2013-01-03" received="0"
            //paid="-1.27" total="-1.27" transactionID="3283049378" />
        }

        private static void ParseCashTransactions(XDocument xml, IDBContext context, bool skipLastDateCheck, DateTime lastDate)
        {
            var cashTransactionsMapper = new Mapper<CashTransaction>(xml.Descendants("CashTransaction"));
            List<CashTransaction> cashTransactions = cashTransactionsMapper.ParseAll();

            var instruments = context.Instruments.ToList();
            var currencies = context.Currencies.ToList();

            foreach (CashTransaction i in cashTransactions)
            {
                if (skipLastDateCheck || i.TransactionDate > lastDate)
                {
                    i.Currency = currencies.FirstOrDefault(x => x.Name == i.CurrencyString);
                    i.Instrument = instruments.FirstOrDefault(x => x.ConID == i.ConID);
                    context.CashTransactions.Add(i);
                }
            }
            context.SaveChanges();

            //<CashTransaction accountId="U1066712" currency="CAD" assetCategory="STK" fxRateToBase="0.99717"
            //symbol="XRE" description="XRE() DIVIDEND .0615 CAD PER SHARE - CA TAX" conid="74580643" securityID=""
            //securityIDType="" cusip="" isin="" underlyingConid="" underlyingSymbol="" issuer="" dateTime="2012-07-31"
            //amount="-6.92" type="Withholding Tax" tradeID="" code="" />
        }

        private static void ParseSecuritiesInfo(XDocument xml, IDBContext context)
        {
            var instrumentMapper = new Mapper<Instrument>(xml.Descendants("SecurityInfo"));
            List<Instrument> instruments = instrumentMapper.ParseAll();

            foreach (Instrument i in instruments)
            {
                if (context.Instruments.Count(x => x.ConID == i.ConID) == 0)
                {
                    context.Instruments.Add(i);
                    context.SaveChanges();
                }
            }

            //<SecurityInfo @assetCategoryassetCategory="STK" symbol="AAPL" description="APPLE INC"
            //conid="265598" securityID="" securityIDType="" cusip="" isin="" underlyingConid=""
            //underlyingSymbol="" issuer="" multiplier="1" expiry="" strike="" maturity="-" issueDate="" />
        }
    }
}