namespace EntityModel.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.BenchmarkComponents",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Weight = c.Double(nullable: false),
                        QDMSInstrumentID = c.Int(nullable: false),
                        BenchmarkID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Benchmarks", t => t.BenchmarkID)
                .Index(t => t.BenchmarkID);
            
            CreateTable(
                "dbo.Benchmarks",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Name = c.String(maxLength: 255),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.CashTransactions",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        CurrencyID = c.Int(nullable: false),
                        TradeID = c.Int(),
                        AssetCategory = c.Int(nullable: false),
                        FXRateToBase = c.Decimal(nullable: false, precision: 20, scale: 10),
                        InstrumentID = c.Int(),
                        ConID = c.Long(),
                        TransactionDate = c.DateTime(nullable: false),
                        Amount = c.Decimal(nullable: false, precision: 20, scale: 10),
                        Type = c.String(maxLength: 255),
                        Description = c.String(maxLength: 255),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Currencies", t => t.CurrencyID)
                .ForeignKey("dbo.Instruments", t => t.InstrumentID)
                .ForeignKey("dbo.Trades", t => t.TradeID)
                .Index(t => t.CurrencyID)
                .Index(t => t.TradeID)
                .Index(t => t.InstrumentID)
                .Index(t => t.TransactionDate)
                .Index(t => t.Type);
            
            CreateTable(
                "dbo.Currencies",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Name = c.String(maxLength: 255),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.Instruments",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        QDMSInstrumentID = c.Int(),
                        Symbol = c.String(maxLength: 50),
                        Description = c.String(maxLength: 255),
                        AssetCategory = c.Int(nullable: false),
                        UnderlyingSymbol = c.String(maxLength: 50),
                        Multiplier = c.Int(nullable: false),
                        Expiration = c.DateTime(),
                        OptionType = c.String(maxLength: 10),
                        Strike = c.Decimal(nullable: false, precision: 20, scale: 10),
                        ConID = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .Index(t => t.ConID);
            
            CreateTable(
                "dbo.Trades",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        StrategyID = c.Int(),
                        Name = c.String(maxLength: 255),
                        Open = c.Boolean(nullable: false),
                        DateOpened = c.DateTime(nullable: false),
                        DateClosed = c.DateTime(),
                        ResultPct = c.Double(nullable: false),
                        ResultDollars = c.Decimal(nullable: false, precision: 20, scale: 10),
                        Commissions = c.Decimal(nullable: false, precision: 20, scale: 10),
                        UnrealizedResultPct = c.Double(nullable: false),
                        UnrealizedResultDollars = c.Decimal(nullable: false, precision: 20, scale: 10),
                        ResultDollarsLong = c.Decimal(nullable: false, precision: 20, scale: 10),
                        ResultDollarsShort = c.Decimal(nullable: false, precision: 20, scale: 10),
                        ResultPctLong = c.Double(nullable: false),
                        ResultPctShort = c.Double(nullable: false),
                        UnrealizedResultDollarsLong = c.Decimal(nullable: false, precision: 20, scale: 10),
                        UnrealizedResultDollarsShort = c.Decimal(nullable: false, precision: 20, scale: 10),
                        UnrealizedResultPctLong = c.Double(nullable: false),
                        UnrealizedResultPctShort = c.Double(nullable: false),
                        CapitalLong = c.Decimal(nullable: false, precision: 20, scale: 10),
                        CapitalShort = c.Decimal(nullable: false, precision: 20, scale: 10),
                        CapitalTotal = c.Decimal(nullable: false, precision: 20, scale: 10),
                        CapitalNet = c.Decimal(nullable: false, precision: 20, scale: 10),
                        Notes = c.String(maxLength: 255),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Strategies", t => t.StrategyID)
                .Index(t => t.StrategyID)
                .Index(t => t.DateOpened);
            
            CreateTable(
                "dbo.FXTransactions",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        FunctionalCurrencyID = c.Int(nullable: false),
                        FXCurrencyID = c.Int(nullable: false),
                        Description = c.String(maxLength: 255),
                        DateTime = c.DateTime(nullable: false),
                        Quantity = c.Decimal(nullable: false, precision: 20, scale: 10),
                        TradeID = c.Int(),
                        Proceeds = c.Decimal(nullable: false, precision: 20, scale: 10),
                        Cost = c.Decimal(nullable: false, precision: 20, scale: 10),
                        Code = c.String(maxLength: 255),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Currencies", t => t.FunctionalCurrencyID)
                .ForeignKey("dbo.Currencies", t => t.FXCurrencyID)
                .ForeignKey("dbo.Trades", t => t.TradeID)
                .Index(t => t.FunctionalCurrencyID)
                .Index(t => t.FXCurrencyID)
                .Index(t => t.TradeID);
            
            CreateTable(
                "dbo.Orders",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        ConID = c.Long(nullable: false),
                        TradeID = c.Int(),
                        InstrumentID = c.Int(nullable: false),
                        TradeDate = c.DateTime(nullable: false),
                        OrderPlacementTime = c.DateTime(),
                        Quantity = c.Int(nullable: false),
                        Price = c.Decimal(nullable: false, precision: 20, scale: 10),
                        Commission = c.Decimal(nullable: false, precision: 20, scale: 10),
                        CurrencyID = c.Int(nullable: false),
                        CommissionCurrencyID = c.Int(nullable: false),
                        AssetCategory = c.Int(nullable: false),
                        FXRateToBase = c.Decimal(nullable: false, precision: 20, scale: 10),
                        Multiplier = c.Int(nullable: false),
                        TradeMoney = c.Decimal(nullable: false, precision: 20, scale: 10),
                        Proceeds = c.Decimal(nullable: false, precision: 20, scale: 10),
                        Taxes = c.Decimal(nullable: false, precision: 20, scale: 10),
                        ClosePrice = c.Decimal(nullable: false, precision: 20, scale: 10),
                        OpenClose = c.String(maxLength: 10),
                        Notes = c.String(maxLength: 50),
                        CostBasis = c.Decimal(nullable: false, precision: 20, scale: 10),
                        FIFORealizedPnL = c.Decimal(nullable: false, precision: 20, scale: 10),
                        MTMPnL = c.Decimal(nullable: false, precision: 20, scale: 10),
                        OptionType = c.String(maxLength: 10),
                        BuySell = c.String(maxLength: 10),
                        IBOrderID = c.Long(),
                        NetCash = c.Decimal(nullable: false, precision: 20, scale: 10),
                        OrderType = c.String(maxLength: 20),
                        IsReal = c.Boolean(nullable: false),
                        ReferencePrice = c.Decimal(precision: 20, scale: 10),
                        ReferenceTime = c.DateTime(),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Currencies", t => t.CommissionCurrencyID)
                .ForeignKey("dbo.Currencies", t => t.CurrencyID)
                .ForeignKey("dbo.Instruments", t => t.InstrumentID)
                .ForeignKey("dbo.Trades", t => t.TradeID)
                .Index(t => t.TradeID)
                .Index(t => t.InstrumentID)
                .Index(t => t.TradeDate)
                .Index(t => t.CurrencyID)
                .Index(t => t.CommissionCurrencyID)
                .Index(t => t.IBOrderID, unique: true);
            
            CreateTable(
                "dbo.Executions",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        ConID = c.Long(nullable: false),
                        InstrumentID = c.Int(nullable: false),
                        Exchange = c.String(maxLength: 50),
                        TradeDate = c.DateTime(nullable: false),
                        OrderPlacementTime = c.DateTime(),
                        Quantity = c.Int(nullable: false),
                        Price = c.Decimal(nullable: false, precision: 20, scale: 10),
                        Commission = c.Decimal(nullable: false, precision: 20, scale: 10),
                        CurrencyID = c.Int(nullable: false),
                        CommissionCurrencyID = c.Int(nullable: false),
                        AssetCategory = c.Int(nullable: false),
                        FXRateToBase = c.Decimal(nullable: false, precision: 20, scale: 10),
                        Multiplier = c.Int(nullable: false),
                        TradeMoney = c.Decimal(nullable: false, precision: 20, scale: 10),
                        Proceeds = c.Decimal(nullable: false, precision: 20, scale: 10),
                        Taxes = c.Decimal(nullable: false, precision: 20, scale: 10),
                        ClosePrice = c.Decimal(nullable: false, precision: 20, scale: 10),
                        OpenClose = c.String(maxLength: 10),
                        Notes = c.String(maxLength: 50),
                        CostBasis = c.Decimal(nullable: false, precision: 20, scale: 10),
                        FIFORealizedPnL = c.Decimal(nullable: false, precision: 20, scale: 10),
                        MTMPnL = c.Decimal(nullable: false, precision: 20, scale: 10),
                        OptionType = c.String(maxLength: 10),
                        BuySell = c.String(maxLength: 10),
                        NetCash = c.Decimal(nullable: false, precision: 20, scale: 10),
                        OrderType = c.String(maxLength: 10),
                        IBTradeID = c.String(maxLength: 100),
                        IBExecID = c.String(maxLength: 100),
                        BrokerageOrderID = c.String(maxLength: 100),
                        IBOrderID = c.Long(nullable: false),
                        OrderID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Currencies", t => t.CommissionCurrencyID)
                .ForeignKey("dbo.Currencies", t => t.CurrencyID)
                .ForeignKey("dbo.Instruments", t => t.InstrumentID)
                .ForeignKey("dbo.Orders", t => t.OrderID)
                .Index(t => t.InstrumentID)
                .Index(t => t.CurrencyID)
                .Index(t => t.CommissionCurrencyID)
                .Index(t => t.OrderID);
            
            CreateTable(
                "dbo.Strategies",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Name = c.String(maxLength: 255),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.Tags",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Name = c.String(maxLength: 50),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.DatasourcePreferences",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        AssetClass = c.Int(nullable: false),
                        Datasource = c.String(maxLength: 255),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.DividendAccruals",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        CurrencyID = c.Int(nullable: false),
                        AssetCategory = c.Int(nullable: false),
                        FXRateToBase = c.Decimal(nullable: false, precision: 20, scale: 10),
                        InstrumentID = c.Int(nullable: false),
                        ConID = c.Long(nullable: false),
                        ExDate = c.DateTime(nullable: false),
                        PayDate = c.DateTime(),
                        Quantity = c.Int(nullable: false),
                        Tax = c.Decimal(nullable: false, precision: 20, scale: 10),
                        GrossRate = c.Decimal(nullable: false, precision: 20, scale: 10),
                        GrossAmount = c.Decimal(nullable: false, precision: 20, scale: 10),
                        NetAmount = c.Decimal(nullable: false, precision: 20, scale: 10),
                        Code = c.String(maxLength: 100),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Currencies", t => t.CurrencyID)
                .ForeignKey("dbo.Instruments", t => t.InstrumentID)
                .Index(t => t.CurrencyID)
                .Index(t => t.InstrumentID);
            
            CreateTable(
                "dbo.EquitySummaries",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Date = c.DateTime(nullable: false),
                        Cash = c.Decimal(nullable: false, precision: 20, scale: 10),
                        CashLong = c.Decimal(nullable: false, precision: 20, scale: 10),
                        CashShort = c.Decimal(nullable: false, precision: 20, scale: 10),
                        SLBCashCollateral = c.Decimal(nullable: false, precision: 20, scale: 10),
                        SLBCashCollateralLong = c.Decimal(nullable: false, precision: 20, scale: 10),
                        SLBCashCollateralShort = c.Decimal(nullable: false, precision: 20, scale: 10),
                        Stock = c.Decimal(nullable: false, precision: 20, scale: 10),
                        StockLong = c.Decimal(nullable: false, precision: 20, scale: 10),
                        StockShort = c.Decimal(nullable: false, precision: 20, scale: 10),
                        SLBDirectSecuritiesBorrowed = c.Decimal(nullable: false, precision: 20, scale: 10),
                        SLBDirectSecuritiesBorrowedLong = c.Decimal(nullable: false, precision: 20, scale: 10),
                        SLBDirectSecuritiesBorrowedShort = c.Decimal(nullable: false, precision: 20, scale: 10),
                        SLBDirectSecuritiesLent = c.Decimal(nullable: false, precision: 20, scale: 10),
                        SLBDirectSecuritiesLentLong = c.Decimal(nullable: false, precision: 20, scale: 10),
                        SLBDirectSecuritiesLentShort = c.Decimal(nullable: false, precision: 20, scale: 10),
                        Options = c.Decimal(nullable: false, precision: 20, scale: 10),
                        OptionsLong = c.Decimal(nullable: false, precision: 20, scale: 10),
                        OptionsShort = c.Decimal(nullable: false, precision: 20, scale: 10),
                        Commodities = c.Decimal(nullable: false, precision: 20, scale: 10),
                        CommoditiesLong = c.Decimal(nullable: false, precision: 20, scale: 10),
                        CommoditiesShort = c.Decimal(nullable: false, precision: 20, scale: 10),
                        Bonds = c.Decimal(nullable: false, precision: 20, scale: 10),
                        BondsLong = c.Decimal(nullable: false, precision: 20, scale: 10),
                        BondsShort = c.Decimal(nullable: false, precision: 20, scale: 10),
                        Notes = c.Decimal(nullable: false, precision: 20, scale: 10),
                        NotesLong = c.Decimal(nullable: false, precision: 20, scale: 10),
                        NotesShort = c.Decimal(nullable: false, precision: 20, scale: 10),
                        InterestAccruals = c.Decimal(nullable: false, precision: 20, scale: 10),
                        InterestAccrualsLong = c.Decimal(nullable: false, precision: 20, scale: 10),
                        InterestAccrualsShort = c.Decimal(nullable: false, precision: 20, scale: 10),
                        SoftDollars = c.Decimal(nullable: false, precision: 20, scale: 10),
                        SoftDollarsLong = c.Decimal(nullable: false, precision: 20, scale: 10),
                        SoftDollarsShort = c.Decimal(nullable: false, precision: 20, scale: 10),
                        DividendAccruals = c.Decimal(nullable: false, precision: 20, scale: 10),
                        DividendAccrualsLong = c.Decimal(nullable: false, precision: 20, scale: 10),
                        DividendAccrualsShort = c.Decimal(nullable: false, precision: 20, scale: 10),
                        Total = c.Decimal(nullable: false, precision: 20, scale: 10),
                        TotalLong = c.Decimal(nullable: false, precision: 20, scale: 10),
                        TotalShort = c.Decimal(nullable: false, precision: 20, scale: 10),
                    })
                .PrimaryKey(t => t.ID);
            
            CreateTable(
                "dbo.FXPositions",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        FunctionalCurrencyID = c.Int(nullable: false),
                        FXCurrencyID = c.Int(nullable: false),
                        Quantity = c.Decimal(nullable: false, precision: 20, scale: 10),
                        CostPrice = c.Decimal(nullable: false, precision: 20, scale: 10),
                        CostBasis = c.Decimal(nullable: false, precision: 20, scale: 10),
                        ClosePrice = c.Decimal(nullable: false, precision: 20, scale: 10),
                        Value = c.Decimal(nullable: false, precision: 20, scale: 10),
                        UnrealizedPnL = c.Decimal(nullable: false, precision: 20, scale: 10),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Currencies", t => t.FunctionalCurrencyID)
                .ForeignKey("dbo.Currencies", t => t.FXCurrencyID)
                .Index(t => t.FunctionalCurrencyID)
                .Index(t => t.FXCurrencyID);
            
            CreateTable(
                "dbo.FXRates",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        FromCurrencyID = c.Int(nullable: false),
                        ToCurrencyID = c.Int(nullable: false),
                        Date = c.DateTime(nullable: false),
                        Rate = c.Decimal(nullable: false, precision: 20, scale: 10),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Currencies", t => t.FromCurrencyID)
                .ForeignKey("dbo.Currencies", t => t.ToCurrencyID)
                .Index(t => t.FromCurrencyID)
                .Index(t => t.ToCurrencyID)
                .Index(t => t.Date);
            
            CreateTable(
                "dbo.OpenPositions",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        CurrencyID = c.Int(nullable: false),
                        InstrumentID = c.Int(nullable: false),
                        FXRateToBase = c.Decimal(nullable: false, precision: 20, scale: 10),
                        Quantity = c.Int(nullable: false),
                        Multiplier = c.Int(nullable: false),
                        MarkPrice = c.Decimal(nullable: false, precision: 20, scale: 10),
                        PositionValue = c.Decimal(nullable: false, precision: 20, scale: 10),
                        OpenPrice = c.Decimal(nullable: false, precision: 20, scale: 10),
                        CostBasisPrice = c.Decimal(nullable: false, precision: 20, scale: 10),
                        CostBasisDollars = c.Decimal(nullable: false, precision: 20, scale: 10),
                        PercentOfNAV = c.Double(nullable: false),
                        UnrealizedPnL = c.Decimal(nullable: false, precision: 20, scale: 10),
                        Side = c.String(maxLength: 10),
                        ConID = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Currencies", t => t.CurrencyID)
                .ForeignKey("dbo.Instruments", t => t.InstrumentID)
                .Index(t => t.CurrencyID)
                .Index(t => t.InstrumentID);
            
            CreateTable(
                "dbo.PriorPositions",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Date = c.DateTime(nullable: false),
                        CurrencyID = c.Int(nullable: false),
                        FXRateToBase = c.Decimal(nullable: false, precision: 20, scale: 10),
                        InstrumentID = c.Int(nullable: false),
                        AssetCategory = c.Int(nullable: false),
                        ConID = c.Long(nullable: false),
                        UnderlyingSymbol = c.String(maxLength: 100),
                        UnderlyingConID = c.Long(),
                        Price = c.Decimal(nullable: false, precision: 20, scale: 10),
                        PriorMTMPnL = c.Decimal(nullable: false, precision: 20, scale: 10),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Currencies", t => t.CurrencyID)
                .ForeignKey("dbo.Instruments", t => t.InstrumentID)
                .Index(t => t.Date)
                .Index(t => t.CurrencyID)
                .Index(t => t.InstrumentID);
            
            CreateTable(
                "dbo.TagMap",
                c => new
                    {
                        TradeID = c.Int(nullable: false),
                        TagID = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.TradeID, t.TagID })
                .ForeignKey("dbo.Trades", t => t.TradeID, cascadeDelete: true)
                .ForeignKey("dbo.Tags", t => t.TagID, cascadeDelete: true)
                .Index(t => t.TradeID)
                .Index(t => t.TagID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PriorPositions", "InstrumentID", "dbo.Instruments");
            DropForeignKey("dbo.PriorPositions", "CurrencyID", "dbo.Currencies");
            DropForeignKey("dbo.OpenPositions", "InstrumentID", "dbo.Instruments");
            DropForeignKey("dbo.OpenPositions", "CurrencyID", "dbo.Currencies");
            DropForeignKey("dbo.FXRates", "ToCurrencyID", "dbo.Currencies");
            DropForeignKey("dbo.FXRates", "FromCurrencyID", "dbo.Currencies");
            DropForeignKey("dbo.FXPositions", "FXCurrencyID", "dbo.Currencies");
            DropForeignKey("dbo.FXPositions", "FunctionalCurrencyID", "dbo.Currencies");
            DropForeignKey("dbo.DividendAccruals", "InstrumentID", "dbo.Instruments");
            DropForeignKey("dbo.DividendAccruals", "CurrencyID", "dbo.Currencies");
            DropForeignKey("dbo.TagMap", "TagID", "dbo.Tags");
            DropForeignKey("dbo.TagMap", "TradeID", "dbo.Trades");
            DropForeignKey("dbo.Trades", "StrategyID", "dbo.Strategies");
            DropForeignKey("dbo.Orders", "TradeID", "dbo.Trades");
            DropForeignKey("dbo.Orders", "InstrumentID", "dbo.Instruments");
            DropForeignKey("dbo.Executions", "OrderID", "dbo.Orders");
            DropForeignKey("dbo.Executions", "InstrumentID", "dbo.Instruments");
            DropForeignKey("dbo.Executions", "CurrencyID", "dbo.Currencies");
            DropForeignKey("dbo.Executions", "CommissionCurrencyID", "dbo.Currencies");
            DropForeignKey("dbo.Orders", "CurrencyID", "dbo.Currencies");
            DropForeignKey("dbo.Orders", "CommissionCurrencyID", "dbo.Currencies");
            DropForeignKey("dbo.FXTransactions", "TradeID", "dbo.Trades");
            DropForeignKey("dbo.FXTransactions", "FXCurrencyID", "dbo.Currencies");
            DropForeignKey("dbo.FXTransactions", "FunctionalCurrencyID", "dbo.Currencies");
            DropForeignKey("dbo.CashTransactions", "TradeID", "dbo.Trades");
            DropForeignKey("dbo.CashTransactions", "InstrumentID", "dbo.Instruments");
            DropForeignKey("dbo.CashTransactions", "CurrencyID", "dbo.Currencies");
            DropForeignKey("dbo.BenchmarkComponents", "BenchmarkID", "dbo.Benchmarks");
            DropIndex("dbo.TagMap", new[] { "TagID" });
            DropIndex("dbo.TagMap", new[] { "TradeID" });
            DropIndex("dbo.PriorPositions", new[] { "InstrumentID" });
            DropIndex("dbo.PriorPositions", new[] { "CurrencyID" });
            DropIndex("dbo.PriorPositions", new[] { "Date" });
            DropIndex("dbo.OpenPositions", new[] { "InstrumentID" });
            DropIndex("dbo.OpenPositions", new[] { "CurrencyID" });
            DropIndex("dbo.FXRates", new[] { "Date" });
            DropIndex("dbo.FXRates", new[] { "ToCurrencyID" });
            DropIndex("dbo.FXRates", new[] { "FromCurrencyID" });
            DropIndex("dbo.FXPositions", new[] { "FXCurrencyID" });
            DropIndex("dbo.FXPositions", new[] { "FunctionalCurrencyID" });
            DropIndex("dbo.DividendAccruals", new[] { "InstrumentID" });
            DropIndex("dbo.DividendAccruals", new[] { "CurrencyID" });
            DropIndex("dbo.Executions", new[] { "OrderID" });
            DropIndex("dbo.Executions", new[] { "CommissionCurrencyID" });
            DropIndex("dbo.Executions", new[] { "CurrencyID" });
            DropIndex("dbo.Executions", new[] { "InstrumentID" });
            DropIndex("dbo.Orders", new[] { "IBOrderID" });
            DropIndex("dbo.Orders", new[] { "CommissionCurrencyID" });
            DropIndex("dbo.Orders", new[] { "CurrencyID" });
            DropIndex("dbo.Orders", new[] { "TradeDate" });
            DropIndex("dbo.Orders", new[] { "InstrumentID" });
            DropIndex("dbo.Orders", new[] { "TradeID" });
            DropIndex("dbo.FXTransactions", new[] { "TradeID" });
            DropIndex("dbo.FXTransactions", new[] { "FXCurrencyID" });
            DropIndex("dbo.FXTransactions", new[] { "FunctionalCurrencyID" });
            DropIndex("dbo.Trades", new[] { "DateOpened" });
            DropIndex("dbo.Trades", new[] { "StrategyID" });
            DropIndex("dbo.Instruments", new[] { "ConID" });
            DropIndex("dbo.CashTransactions", new[] { "Type" });
            DropIndex("dbo.CashTransactions", new[] { "TransactionDate" });
            DropIndex("dbo.CashTransactions", new[] { "InstrumentID" });
            DropIndex("dbo.CashTransactions", new[] { "TradeID" });
            DropIndex("dbo.CashTransactions", new[] { "CurrencyID" });
            DropIndex("dbo.BenchmarkComponents", new[] { "BenchmarkID" });
            DropTable("dbo.TagMap");
            DropTable("dbo.PriorPositions");
            DropTable("dbo.OpenPositions");
            DropTable("dbo.FXRates");
            DropTable("dbo.FXPositions");
            DropTable("dbo.EquitySummaries");
            DropTable("dbo.DividendAccruals");
            DropTable("dbo.DatasourcePreferences");
            DropTable("dbo.Tags");
            DropTable("dbo.Strategies");
            DropTable("dbo.Executions");
            DropTable("dbo.Orders");
            DropTable("dbo.FXTransactions");
            DropTable("dbo.Trades");
            DropTable("dbo.Instruments");
            DropTable("dbo.Currencies");
            DropTable("dbo.CashTransactions");
            DropTable("dbo.Benchmarks");
            DropTable("dbo.BenchmarkComponents");
        }
    }
}
