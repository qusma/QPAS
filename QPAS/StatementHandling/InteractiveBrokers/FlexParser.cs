// -----------------------------------------------------------------------
// <copyright file="FlexParser.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using MahApps.Metro.Controls.Dialogs;
using NXmlMapper;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Xml.Linq;

namespace QPAS
{
    [Export(nameof(IStatementParser), typeof(IStatementParser))]
    [ExportMetadata("Name", "Interactive Brokers")]
    public class FlexParser : IStatementParser
    {
        public string Name { get { return "Interactive Brokers"; } }

        /// <summary>
        /// Parse a flex file
        /// </summary>
        /// <param name="flexXml"></param>
        /// <param name="progress"></param>
        /// <param name="settings"></param>
        /// <param name="currencies"></param>
        /// <returns>The key is the accountId, the data the parsed statement for that account</returns>
        public Dictionary<string, DataContainer> Parse(string flexXml, ProgressDialogController progress, IAppSettings settings, IEnumerable<Currency> currencies)
        {
            progress.SetMessage("Parsing Flex File");

            bool skipLastDateCheck = !settings.PreventDuplicateImports;

            XDocument xml = XDocument.Parse(flexXml);

            IEnumerable<XElement> flexStatements = xml.Descendants("FlexStatement");
            var data = new Dictionary<string, DataContainer>(); //key is accountid

            foreach (XElement flexStatement in flexStatements)
            {
                var (account, newData) = ParseStatement(flexStatement, progress, currencies);
                data.Add(account, newData);
            }

            return data;
        }

        private (string AccountId, DataContainer Data) ParseStatement(XElement xml, ProgressDialogController progress, IEnumerable<Currency> currencies)
        {
            const int totalActions = 12;

            var data = new DataContainer();
            data.Currencies.AddRange(currencies);

            ParseAccounts(xml, data);

            string accountId = xml.Attribute("accountId").Value;

            progress.SetProgress(1.0 / totalActions);
            ParseSecuritiesInfo(xml, data);

            progress.SetProgress(2.0 / totalActions);
            ParseCashTransactions(xml, data);

            progress.SetProgress(3.0 / totalActions);
            ParseCFDCharges(xml, data);

            progress.SetProgress(4.0 / totalActions);
            ParseOrders(xml, data);

            progress.SetProgress(5.0 / totalActions);
            ParseExecutions(xml, data);

            progress.SetProgress(6.0 / totalActions);
            ParseEquitySummaries(xml, data);

            progress.SetProgress(7.0 / totalActions);
            ParseOpenPositions(xml, data);

            progress.SetProgress(8.0 / totalActions);
            ParseFXRates(xml, data);

            progress.SetProgress(9.0 / totalActions);
            ParsePriorPeriodPositions(xml, data);

            progress.SetProgress(10.0 / totalActions);
            ParseOpenDividendAccruals(xml, data);

            progress.SetProgress(11.0 / totalActions);
            ParseFXPositions(xml, data);

            progress.SetProgress(12.0 / totalActions);
            ParseFXTransactions(xml, data);

            return (AccountId: accountId, Data: data);
        }

        private void ParseAccounts(XElement xml, DataContainer data)
        {
            var accountId = xml.Attribute("accountId").Value;
            if (!data.Accounts.Any(x => x.AccountId == accountId))
            {
                data.Accounts.Add(new Account { AccountId = accountId });
            }
        }

        public string GetFileFilter()
        {
            return "Flex Files (*.xml)|*.xml";
        }

        private static void ParseFXTransactions(XContainer xml, DataContainer data)
        {
            var fxTransactionMapper = new Mapper<FXTransaction>(xml.Descendants("FxTransaction"));
            List<FXTransaction> fxTransactions = fxTransactionMapper.ParseAll();

            foreach (FXTransaction i in fxTransactions)
            {
                data.FXTransactions.Add(i);
            }

            //<FxTransaction accountId="U1066712" acctAlias="" assetCategory="CASH" reportDate="20130401"
            //functionalCurrency="USD" fxCurrency="CAD" activityDescription="XRE() DIVIDEND .06726 CAD PER SHARE"
            //dateTime="20130328;202000" quantity="16.29" proceeds="16.034084" cost="-16.034084" realizedPL="0"
            //code="O" levelOfDetail="TRANSACTION" />
        }

        private static void ParseFXPositions(XContainer xml, DataContainer data)
        {
            var fxPositionsMapper = new Mapper<FXPosition>(xml.Descendants("FxPosition"));
            List<FXPosition> fxPositions = fxPositionsMapper.ParseAll();

            //then add the new ones
            foreach (FXPosition i in fxPositions)
            {
                data.FXPositions.Add(i);
            }

            //<FxPosition accountId="U1066712" acctAlias="" assetCategory="CASH" reportDate="20140325"
            //functionalCurrency="USD" fxCurrency="CAD" quantity="22.379966" costPrice="0.890970031" costBasis="-19.939879"
            //closePrice="0.89552" value="20.041707" unrealizedPL="0.101828" code="" lotDescription="" lotOpenDateTime="" levelOfDetail="SUMMARY" />
        }

        private static void ParseOpenDividendAccruals(XContainer xml, DataContainer data)
        {
            var openDividendAccrualsMapper = new Mapper<DividendAccrual>(xml.Descendants("OpenDividendAccrual"));
            List<DividendAccrual> dividendAccruals = openDividendAccrualsMapper.ParseAll();

            //then add the new ones
            foreach (DividendAccrual i in dividendAccruals)
            {
                data.DividendAccruals.Add(i);
            }

            //<OpenDividendAccrual accountId="U1066712" currency="USD" assetCategory="STK" fxRateToBase="1" symbol="PICB"
            //description="POWERSHARES INT CORP BOND" conid="75980548" securityID="" securityIDType="" cusip="" isin=""
            //underlyingConid="" underlyingSymbol="" issuer="" exDate="2013-01-15" payDate="2013-01-31" quantity="19" tax="0.44"
            //fee="0" grossRate="0.07613" grossAmount="1.45" netAmount="1.01" code="" fromAcct="" toAcct="" />
        }

        private static void ParsePriorPeriodPositions(XContainer xml, DataContainer data)
        {
            var priorPeriodPositionsMapper = new Mapper<PriorPosition>(xml.Descendants("PriorPeriodPosition"));
            List<PriorPosition> priorPeriodPositions = priorPeriodPositionsMapper.ParseAll();

            foreach (PriorPosition priorPosition in priorPeriodPositions)
            {
                data.PriorPositions.Add(priorPosition);
            }

            //<PriorPeriodPosition accountId="U1066712" currency="USD" assetCategory="STK" fxRateToBase="1" symbol="ACWV"
            //description="ISHARES MSCI ALL COUNTRY WOR" conid="96090060" securityID="" securityIDType="" cusip="" isin=""
            //underlyingConid="" underlyingSymbol="" issuer="" date="2012-12-28" price="55.23" priorMtmPnl="-9" />
        }

        private static void ParseFXRates(XContainer xml, DataContainer data)
        {
            var fxRatesMapper = new Mapper<FXRate>(xml.Descendants("ConversionRate"));
            List<FXRate> fxRates = fxRatesMapper.ParseAll();

            foreach (FXRate i in fxRates)
            {
                data.FXRates.Add(i);
            }

            //<ConversionRate reportDate="2012-12-28" fromCurrency="CHF" toCurrency="USD" rate="1.0947" />
        }

        private static void ParseOpenPositions(XContainer xml, DataContainer data)
        {
            if (!xml.Descendants("OpenPositions").Any()) return;
            var openPositionsMapper = new Mapper<OpenPosition>(xml.Descendants("OpenPosition").Where(x => x.Attribute("levelOfDetail").Value == "SUMMARY"));
            List<OpenPosition> openPositions = openPositionsMapper.ParseAll();

            foreach (OpenPosition op in openPositions)
            {
                data.OpenPositions.Add(op);
            }

            //<OpenPosition accountId="U1066712" currency="USD" assetCategory="STK" fxRateToBase="1" symbol="ACWV"
            //description="ISHARES MSCI ALL COUNTRY WOR" conid="96090060" securityID="" securityIDType="" cusip=""
            //isin="" underlyingConid="" underlyingSymbol="" issuer="" reportDate="20130131" position="18" multiplier="1"
            //markPrice="58.01" positionValue="1044.18" openPrice="55.877777778" costBasisPrice="55.877777778"
            //costBasisMoney="1005.8" percentOfNAV="1.61" fifoPnlUnrealized="38.38" side="Long" @levelOfDetaillevelOfDetail="SUMMARY"
            //openDateTime="" holdingPeriodDateTime="" code="" originatingOrderID="" />
        }

        private static void ParseEquitySummaries(XContainer xml, DataContainer data)
        {
            var equitySummaryMapper = new Mapper<EquitySummary>(xml.Descendants("EquitySummaryByReportDateInBase"));
            List<EquitySummary> equitySummaries = equitySummaryMapper.ParseAll();

            foreach (EquitySummary equtySummary in equitySummaries)
            {
                data.EquitySummaries.Add(equtySummary);
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

        private static void ParseExecutions(XContainer xml, DataContainer data)
        {
            if (!xml.Descendants("Trades").Any()) return;

            var tradesMapper = new Mapper<Execution>(xml.Descendants("Trades").First().Descendants("Trade"));
            List<Execution> executions = tradesMapper.ParseAll();

            var orderReferenceSet = new List<long>(); //used to keep track of which orders we have set the order reference for, so we don't do it multiple times

            //then add the new ones
            foreach (Execution exec in executions)
            {
                var order = data.Orders.First(x => x.IBOrderID == exec.IBOrderID);
                exec.Order = order;
                if (!string.IsNullOrEmpty(exec.OrderReference) && !orderReferenceSet.Contains(exec.IBOrderID))
                {
                    orderReferenceSet.Add(exec.IBOrderID);
                    order.OrderReference = exec.OrderReference;
                }
                order.Executions.Add(exec);
                data.Executions.Add(exec);
            }

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

        private static void ParseOrders(XContainer xml, DataContainer data)
        {
            if (!xml.Descendants("Trades").Any()) return;

            var ordersMapper = new Mapper<Order>(xml.Descendants("Trades").First().Descendants("Order"));
            List<Order> orders = ordersMapper.ParseAll();

            //then add the new ones
            foreach (Order order in orders)
            {
                order.IsReal = true;
                data.Orders.Add(order);
            }

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

        private static void ParseCFDCharges(XContainer xml, DataContainer data)
        {
            var cfdTransactionsMapper = new Mapper<CashTransaction>(xml.Descendants("CFDCharge"));
            cfdTransactionsMapper.SetAttributeMap("total", "Amount");
            cfdTransactionsMapper.SetAttributeMap("date", "TransactionDate", "yyyy-MM-dd");
            List<CashTransaction> cfdCharges = cfdTransactionsMapper.ParseAll();

            foreach (CashTransaction i in cfdCharges)
            {
                i.Type = "CFD Charge";

                data.CashTransactions.Add(i);
            }

            //<CFDCharge accountId="U1066712F" currency="USD" assetCategory="CFD" fxRateToBase="1"
            //symbol="--" description="--" conid="--" securityID="--" securityIDType="--" cusip="--"
            //isin="--" underlyingConid="--" underlyingSymbol="--" issuer="--" date="2013-01-03" received="0"
            //paid="-1.27" total="-1.27" transactionID="3283049378" />
        }

        private static void ParseCashTransactions(XContainer xml, DataContainer data)
        {
            var cashTransactionsMapper = new Mapper<CashTransaction>(xml.Descendants("CashTransaction"));
            List<CashTransaction> cashTransactions = cashTransactionsMapper.ParseAll();

            foreach (CashTransaction i in cashTransactions)
            {
                data.CashTransactions.Add(i);
            }

            //<CashTransaction accountId="U1066712" currency="CAD" assetCategory="STK" fxRateToBase="0.99717"
            //symbol="XRE" description="XRE() DIVIDEND .0615 CAD PER SHARE - CA TAX" conid="74580643" securityID=""
            //securityIDType="" cusip="" isin="" underlyingConid="" underlyingSymbol="" issuer="" dateTime="2012-07-31"
            //amount="-6.92" type="Withholding Tax" tradeID="" code="" />
        }

        private static void ParseSecuritiesInfo(XContainer xml, DataContainer data)
        {
            var instrumentMapper = new Mapper<Instrument>(xml.Descendants("SecurityInfo"));
            List<Instrument> instruments = instrumentMapper.ParseAll();

            foreach (Instrument i in instruments)
            {
                if (data.Instruments.Count(x => x.ConID == i.ConID) == 0)
                {
                    data.Instruments.Add(i);
                }
            }

            //<SecurityInfo @assetCategoryassetCategory="STK" symbol="AAPL" description="APPLE INC"
            //conid="265598" securityID="" securityIDType="" cusip="" isin="" underlyingConid=""
            //underlyingSymbol="" issuer="" multiplier="1" expiry="" strike="" maturity="-" issueDate="" />
        }
    }
}