using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace EntityModel.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Benchmarks",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Benchmarks", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "DatasourcePreferences",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AssetClass = table.Column<int>(type: "INTEGER", nullable: false),
                    Datasource = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatasourcePreferences", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Instruments",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    QDMSInstrumentID = table.Column<int>(type: "INTEGER", nullable: true),
                    Symbol = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    AssetCategory = table.Column<int>(type: "INTEGER", nullable: false),
                    UnderlyingSymbol = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Multiplier = table.Column<int>(type: "INTEGER", nullable: false),
                    Expiration = table.Column<DateTime>(type: "TEXT", nullable: true),
                    OptionType = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Strike = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    ConID = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instruments", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Strategies",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Strategies", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "UserScripts",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 65535, nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    ReferencedAssembliesAsString = table.Column<string>(type: "TEXT", maxLength: 65535, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserScripts", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "EquitySummaries",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Cash = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    CashLong = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    CashShort = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    SLBCashCollateral = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    SLBCashCollateralLong = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    SLBCashCollateralShort = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    Stock = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    StockLong = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    StockShort = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    SLBDirectSecuritiesBorrowed = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    SLBDirectSecuritiesBorrowedLong = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    SLBDirectSecuritiesBorrowedShort = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    SLBDirectSecuritiesLent = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    SLBDirectSecuritiesLentLong = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    SLBDirectSecuritiesLentShort = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    Options = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    OptionsLong = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    OptionsShort = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    Commodities = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    CommoditiesLong = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    CommoditiesShort = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    Bonds = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    BondsLong = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    BondsShort = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    Notes = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    NotesLong = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    NotesShort = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    InterestAccruals = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    InterestAccrualsLong = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    InterestAccrualsShort = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    SoftDollars = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    SoftDollarsLong = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    SoftDollarsShort = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    DividendAccruals = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    DividendAccrualsLong = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    DividendAccrualsShort = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    Total = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    TotalLong = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    TotalShort = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    AccountID = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquitySummaries", x => x.ID);
                    table.ForeignKey(
                        name: "FK_EquitySummaries_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BenchmarkComponents",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Weight = table.Column<double>(type: "REAL", nullable: false),
                    QDMSInstrumentID = table.Column<int>(type: "INTEGER", nullable: false),
                    BenchmarkID = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BenchmarkComponents", x => x.ID);
                    table.ForeignKey(
                        name: "FK_BenchmarkComponents_Benchmarks_BenchmarkID",
                        column: x => x.BenchmarkID,
                        principalTable: "Benchmarks",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FXPositions",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FunctionalCurrencyID = table.Column<int>(type: "INTEGER", nullable: false),
                    FXCurrencyID = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    CostPrice = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    CostBasis = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    ClosePrice = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    Value = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    UnrealizedPnL = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    AccountID = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FXPositions", x => x.ID);
                    table.ForeignKey(
                        name: "FK_FXPositions_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FXPositions_Currencies_FunctionalCurrencyID",
                        column: x => x.FunctionalCurrencyID,
                        principalTable: "Currencies",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FXPositions_Currencies_FXCurrencyID",
                        column: x => x.FXCurrencyID,
                        principalTable: "Currencies",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FXRates",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FromCurrencyID = table.Column<int>(type: "INTEGER", nullable: false),
                    ToCurrencyID = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Rate = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FXRates", x => x.ID);
                    table.ForeignKey(
                        name: "FK_FXRates_Currencies_FromCurrencyID",
                        column: x => x.FromCurrencyID,
                        principalTable: "Currencies",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FXRates_Currencies_ToCurrencyID",
                        column: x => x.ToCurrencyID,
                        principalTable: "Currencies",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DividendAccruals",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CurrencyID = table.Column<int>(type: "INTEGER", nullable: false),
                    AssetCategory = table.Column<int>(type: "INTEGER", nullable: false),
                    FXRateToBase = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    InstrumentID = table.Column<int>(type: "INTEGER", nullable: false),
                    ConID = table.Column<long>(type: "INTEGER", nullable: false),
                    ExDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PayDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Tax = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    GrossRate = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    NetAmount = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    AccountID = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DividendAccruals", x => x.ID);
                    table.ForeignKey(
                        name: "FK_DividendAccruals_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DividendAccruals_Currencies_CurrencyID",
                        column: x => x.CurrencyID,
                        principalTable: "Currencies",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DividendAccruals_Instruments_InstrumentID",
                        column: x => x.InstrumentID,
                        principalTable: "Instruments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpenPositions",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CurrencyID = table.Column<int>(type: "INTEGER", nullable: false),
                    InstrumentID = table.Column<int>(type: "INTEGER", nullable: false),
                    FXRateToBase = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Multiplier = table.Column<int>(type: "INTEGER", nullable: false),
                    MarkPrice = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    PositionValue = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    OpenPrice = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    CostBasisPrice = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    CostBasisDollars = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    PercentOfNAV = table.Column<double>(type: "REAL", nullable: false),
                    UnrealizedPnL = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    Side = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    ConID = table.Column<long>(type: "INTEGER", nullable: false),
                    AccountID = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenPositions", x => x.ID);
                    table.ForeignKey(
                        name: "FK_OpenPositions_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpenPositions_Currencies_CurrencyID",
                        column: x => x.CurrencyID,
                        principalTable: "Currencies",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OpenPositions_Instruments_InstrumentID",
                        column: x => x.InstrumentID,
                        principalTable: "Instruments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriorPositions",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CurrencyID = table.Column<int>(type: "INTEGER", nullable: false),
                    FXRateToBase = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    InstrumentID = table.Column<int>(type: "INTEGER", nullable: false),
                    AssetCategory = table.Column<int>(type: "INTEGER", nullable: false),
                    ConID = table.Column<long>(type: "INTEGER", nullable: false),
                    UnderlyingSymbol = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    UnderlyingConID = table.Column<long>(type: "INTEGER", nullable: true),
                    Price = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    PriorMTMPnL = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    AccountID = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriorPositions", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PriorPositions_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PriorPositions_Currencies_CurrencyID",
                        column: x => x.CurrencyID,
                        principalTable: "Currencies",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PriorPositions_Instruments_InstrumentID",
                        column: x => x.InstrumentID,
                        principalTable: "Instruments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Trades",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StrategyID = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Open = table.Column<bool>(type: "INTEGER", nullable: false),
                    DateOpened = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DateClosed = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResultPct = table.Column<double>(type: "REAL", nullable: false),
                    ResultDollars = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    Commissions = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    UnrealizedResultPct = table.Column<double>(type: "REAL", nullable: false),
                    UnrealizedResultDollars = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    ResultDollarsLong = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    ResultDollarsShort = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    ResultPctLong = table.Column<double>(type: "REAL", nullable: false),
                    ResultPctShort = table.Column<double>(type: "REAL", nullable: false),
                    UnrealizedResultDollarsLong = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    UnrealizedResultDollarsShort = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    UnrealizedResultPctLong = table.Column<double>(type: "REAL", nullable: false),
                    UnrealizedResultPctShort = table.Column<double>(type: "REAL", nullable: false),
                    CapitalLong = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    CapitalShort = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    CapitalTotal = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    CapitalNet = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 65535, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trades", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Trades_Strategies_StrategyID",
                        column: x => x.StrategyID,
                        principalTable: "Strategies",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CashTransactions",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CurrencyID = table.Column<int>(type: "INTEGER", nullable: false),
                    TradeID = table.Column<int>(type: "INTEGER", nullable: true),
                    AssetCategory = table.Column<int>(type: "INTEGER", nullable: false),
                    FXRateToBase = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    InstrumentID = table.Column<int>(type: "INTEGER", nullable: true),
                    ConID = table.Column<long>(type: "INTEGER", nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    AccountID = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashTransactions", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CashTransactions_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashTransactions_Currencies_CurrencyID",
                        column: x => x.CurrencyID,
                        principalTable: "Currencies",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CashTransactions_Instruments_InstrumentID",
                        column: x => x.InstrumentID,
                        principalTable: "Instruments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashTransactions_Trades_TradeID",
                        column: x => x.TradeID,
                        principalTable: "Trades",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FXTransactions",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FunctionalCurrencyID = table.Column<int>(type: "INTEGER", nullable: false),
                    FXCurrencyID = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    DateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    TradeID = table.Column<int>(type: "INTEGER", nullable: true),
                    Proceeds = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    Cost = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    AccountID = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FXTransactions", x => x.ID);
                    table.ForeignKey(
                        name: "FK_FXTransactions_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FXTransactions_Currencies_FunctionalCurrencyID",
                        column: x => x.FunctionalCurrencyID,
                        principalTable: "Currencies",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FXTransactions_Currencies_FXCurrencyID",
                        column: x => x.FXCurrencyID,
                        principalTable: "Currencies",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FXTransactions_Trades_TradeID",
                        column: x => x.TradeID,
                        principalTable: "Trades",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConID = table.Column<long>(type: "INTEGER", nullable: false),
                    TradeID = table.Column<int>(type: "INTEGER", nullable: true),
                    InstrumentID = table.Column<int>(type: "INTEGER", nullable: false),
                    TradeDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OrderPlacementTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    Commission = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    CurrencyID = table.Column<int>(type: "INTEGER", nullable: false),
                    CommissionCurrencyID = table.Column<int>(type: "INTEGER", nullable: false),
                    AssetCategory = table.Column<int>(type: "INTEGER", nullable: false),
                    FXRateToBase = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    Multiplier = table.Column<int>(type: "INTEGER", nullable: false),
                    TradeMoney = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    Proceeds = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    Taxes = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    ClosePrice = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    OpenClose = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CostBasis = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    FIFORealizedPnL = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    MTMPnL = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    OptionType = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    BuySell = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    IBOrderID = table.Column<long>(type: "INTEGER", nullable: true),
                    NetCash = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    OrderType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    OrderReference = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    IsReal = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReferencePrice = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: true),
                    ReferenceTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AccountID = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Orders_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Currencies_CommissionCurrencyID",
                        column: x => x.CommissionCurrencyID,
                        principalTable: "Currencies",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Orders_Currencies_CurrencyID",
                        column: x => x.CurrencyID,
                        principalTable: "Currencies",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Orders_Instruments_InstrumentID",
                        column: x => x.InstrumentID,
                        principalTable: "Instruments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Orders_Trades_TradeID",
                        column: x => x.TradeID,
                        principalTable: "Trades",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TagMap",
                columns: table => new
                {
                    TagsID = table.Column<int>(type: "INTEGER", nullable: false),
                    TradesID = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagMap", x => new { x.TagsID, x.TradesID });
                    table.ForeignKey(
                        name: "FK_TagMap_Tags_TagsID",
                        column: x => x.TagsID,
                        principalTable: "Tags",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TagMap_Trades_TradesID",
                        column: x => x.TradesID,
                        principalTable: "Trades",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Executions",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConID = table.Column<long>(type: "INTEGER", nullable: false),
                    InstrumentID = table.Column<int>(type: "INTEGER", nullable: false),
                    Exchange = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    TradeDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OrderPlacementTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    Commission = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    CurrencyID = table.Column<int>(type: "INTEGER", nullable: false),
                    CommissionCurrencyID = table.Column<int>(type: "INTEGER", nullable: false),
                    AssetCategory = table.Column<int>(type: "INTEGER", nullable: false),
                    FXRateToBase = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    Multiplier = table.Column<int>(type: "INTEGER", nullable: false),
                    TradeMoney = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    Proceeds = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    Taxes = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    ClosePrice = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    OpenClose = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CostBasis = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    FIFORealizedPnL = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    MTMPnL = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    OptionType = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    BuySell = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    NetCash = table.Column<decimal>(type: "TEXT", precision: 20, scale: 10, nullable: false),
                    OrderType = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    IBTradeID = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IBExecID = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    BrokerageOrderID = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IBOrderID = table.Column<long>(type: "INTEGER", nullable: false),
                    OrderReference = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    OrderID = table.Column<int>(type: "INTEGER", nullable: false),
                    AccountID = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Executions", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Executions_Accounts_AccountID",
                        column: x => x.AccountID,
                        principalTable: "Accounts",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Executions_Currencies_CommissionCurrencyID",
                        column: x => x.CommissionCurrencyID,
                        principalTable: "Currencies",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Executions_Currencies_CurrencyID",
                        column: x => x.CurrencyID,
                        principalTable: "Currencies",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Executions_Instruments_InstrumentID",
                        column: x => x.InstrumentID,
                        principalTable: "Instruments",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Executions_Orders_OrderID",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_AccountId",
                table: "Accounts",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BenchmarkComponents_BenchmarkID",
                table: "BenchmarkComponents",
                column: "BenchmarkID");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransactions_AccountID",
                table: "CashTransactions",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransactions_CurrencyID",
                table: "CashTransactions",
                column: "CurrencyID");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransactions_InstrumentID",
                table: "CashTransactions",
                column: "InstrumentID");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransactions_TradeID",
                table: "CashTransactions",
                column: "TradeID");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransactions_TransactionDate",
                table: "CashTransactions",
                column: "TransactionDate");

            migrationBuilder.CreateIndex(
                name: "IX_CashTransactions_Type",
                table: "CashTransactions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_DividendAccruals_AccountID",
                table: "DividendAccruals",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_DividendAccruals_CurrencyID",
                table: "DividendAccruals",
                column: "CurrencyID");

            migrationBuilder.CreateIndex(
                name: "IX_DividendAccruals_InstrumentID",
                table: "DividendAccruals",
                column: "InstrumentID");

            migrationBuilder.CreateIndex(
                name: "IX_EquitySummaries_AccountID",
                table: "EquitySummaries",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_Executions_AccountID",
                table: "Executions",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_Executions_CommissionCurrencyID",
                table: "Executions",
                column: "CommissionCurrencyID");

            migrationBuilder.CreateIndex(
                name: "IX_Executions_CurrencyID",
                table: "Executions",
                column: "CurrencyID");

            migrationBuilder.CreateIndex(
                name: "IX_Executions_InstrumentID",
                table: "Executions",
                column: "InstrumentID");

            migrationBuilder.CreateIndex(
                name: "IX_Executions_OrderID",
                table: "Executions",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_FXPositions_AccountID",
                table: "FXPositions",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_FXPositions_FunctionalCurrencyID",
                table: "FXPositions",
                column: "FunctionalCurrencyID");

            migrationBuilder.CreateIndex(
                name: "IX_FXPositions_FXCurrencyID",
                table: "FXPositions",
                column: "FXCurrencyID");

            migrationBuilder.CreateIndex(
                name: "IX_FXRates_Date",
                table: "FXRates",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_FXRates_FromCurrencyID",
                table: "FXRates",
                column: "FromCurrencyID");

            migrationBuilder.CreateIndex(
                name: "IX_FXRates_ToCurrencyID",
                table: "FXRates",
                column: "ToCurrencyID");

            migrationBuilder.CreateIndex(
                name: "IX_FXTransactions_AccountID",
                table: "FXTransactions",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_FXTransactions_FunctionalCurrencyID",
                table: "FXTransactions",
                column: "FunctionalCurrencyID");

            migrationBuilder.CreateIndex(
                name: "IX_FXTransactions_FXCurrencyID",
                table: "FXTransactions",
                column: "FXCurrencyID");

            migrationBuilder.CreateIndex(
                name: "IX_FXTransactions_TradeID",
                table: "FXTransactions",
                column: "TradeID");

            migrationBuilder.CreateIndex(
                name: "IX_Instruments_ConID",
                table: "Instruments",
                column: "ConID");

            migrationBuilder.CreateIndex(
                name: "IX_OpenPositions_AccountID",
                table: "OpenPositions",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_OpenPositions_CurrencyID",
                table: "OpenPositions",
                column: "CurrencyID");

            migrationBuilder.CreateIndex(
                name: "IX_OpenPositions_InstrumentID",
                table: "OpenPositions",
                column: "InstrumentID");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_AccountID",
                table: "Orders",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CommissionCurrencyID",
                table: "Orders",
                column: "CommissionCurrencyID");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CurrencyID",
                table: "Orders",
                column: "CurrencyID");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_IBOrderID",
                table: "Orders",
                column: "IBOrderID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_InstrumentID",
                table: "Orders",
                column: "InstrumentID");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TradeDate",
                table: "Orders",
                column: "TradeDate");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TradeID",
                table: "Orders",
                column: "TradeID");

            migrationBuilder.CreateIndex(
                name: "IX_PriorPositions_AccountID",
                table: "PriorPositions",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_PriorPositions_CurrencyID",
                table: "PriorPositions",
                column: "CurrencyID");

            migrationBuilder.CreateIndex(
                name: "IX_PriorPositions_Date",
                table: "PriorPositions",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_PriorPositions_InstrumentID",
                table: "PriorPositions",
                column: "InstrumentID");

            migrationBuilder.CreateIndex(
                name: "IX_TagMap_TradesID",
                table: "TagMap",
                column: "TradesID");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_DateOpened",
                table: "Trades",
                column: "DateOpened");

            migrationBuilder.CreateIndex(
                name: "IX_Trades_StrategyID",
                table: "Trades",
                column: "StrategyID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BenchmarkComponents");

            migrationBuilder.DropTable(
                name: "CashTransactions");

            migrationBuilder.DropTable(
                name: "DatasourcePreferences");

            migrationBuilder.DropTable(
                name: "DividendAccruals");

            migrationBuilder.DropTable(
                name: "EquitySummaries");

            migrationBuilder.DropTable(
                name: "Executions");

            migrationBuilder.DropTable(
                name: "FXPositions");

            migrationBuilder.DropTable(
                name: "FXRates");

            migrationBuilder.DropTable(
                name: "FXTransactions");

            migrationBuilder.DropTable(
                name: "OpenPositions");

            migrationBuilder.DropTable(
                name: "PriorPositions");

            migrationBuilder.DropTable(
                name: "TagMap");

            migrationBuilder.DropTable(
                name: "UserScripts");

            migrationBuilder.DropTable(
                name: "Benchmarks");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "Currencies");

            migrationBuilder.DropTable(
                name: "Instruments");

            migrationBuilder.DropTable(
                name: "Trades");

            migrationBuilder.DropTable(
                name: "Strategies");
        }
    }
}