// -----------------------------------------------------------------------
// <copyright file="Seed.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using System.Collections.Generic;
using System.Linq;

namespace QPAS
{
    public class Seed
    {
        public static void DoSeed(IQpasDbContext context)
        {
            if (context.Currencies.Any()) return;

                var currencies = new List<Currency>
            {
                new Currency { Name = "USD" },
                new Currency { Name = "CAD" },
                new Currency { Name = "GBP" },
                new Currency { Name = "EUR" },
                new Currency { Name = "CHF" },
                new Currency { Name = "JPY" },
                new Currency { Name = "AUD" },
                new Currency { Name = "SEK" },
                new Currency { Name = "HKD" },
                new Currency { Name = "NOK" },
                new Currency { Name = "HUF" },
                new Currency { Name = "SAR" },
                new Currency { Name = "BGL" },
                new Currency { Name = "TWD" },
                new Currency { Name = "CZK" },
                new Currency { Name = "DKK" },
                new Currency { Name = "ILS" },
                new Currency { Name = "ISK" },
                new Currency { Name = "KRW" },
                new Currency { Name = "PLN" },
                new Currency { Name = "BRL" },
                new Currency { Name = "ROL" },
                new Currency { Name = "RUR" },
                new Currency { Name = "HRK" },
                new Currency { Name = "ALL" },
                new Currency { Name = "THB" },
                new Currency { Name = "TRY" },
                new Currency { Name = "PKR" },
                new Currency { Name = "IDR" },
                new Currency { Name = "UAH" },
                new Currency { Name = "BYB" },
                new Currency { Name = "EEK" },
                new Currency { Name = "LVL" },
                new Currency { Name = "LTL" },
                new Currency { Name = "IRR" },
                new Currency { Name = "VND" },
                new Currency { Name = "AMD" },
                new Currency { Name = "AZM" },
                new Currency { Name = "MKD" },
                new Currency { Name = "ZAR" },
                new Currency { Name = "GEL" },
                new Currency { Name = "INR" },
                new Currency { Name = "MYR" },
                new Currency { Name = "KZT" },
                new Currency { Name = "KGS" },
                new Currency { Name = "KES" },
                new Currency { Name = "UZS" },
                new Currency { Name = "MNT" },
                new Currency { Name = "SYP" },
                new Currency { Name = "MVR" },
                new Currency { Name = "IQD" },
                new Currency { Name = "CNY" },
                new Currency { Name = "MXN" },
                new Currency { Name = "CSD" },
                new Currency { Name = "BNd" },
                new Currency { Name = "EGP" },
                new Currency { Name = "LYD" },
                new Currency { Name = "SGD" },
                new Currency { Name = "GTQ" },
                new Currency { Name = "DZD" },
                new Currency { Name = "MOP" },
                new Currency { Name = "NZD" },
                new Currency { Name = "CRC" },
                new Currency { Name = "MAD" },
                new Currency { Name = "PAB" },
                new Currency { Name = "TND" },
                new Currency { Name = "DOP" },
                new Currency { Name = "OMR" },
                new Currency { Name = "JMD" },
                new Currency { Name = "VEB" },
                new Currency { Name = "YER" },
                new Currency { Name = "COP" },
                new Currency { Name = "BZD" },
                new Currency { Name = "PEN" },
                new Currency { Name = "JOD" },
                new Currency { Name = "TTD" },
                new Currency { Name = "ARS" },
                new Currency { Name = "LBP" },
                new Currency { Name = "ZWD" },
                new Currency { Name = "KWD" },
                new Currency { Name = "PHP" },
                new Currency { Name = "CLP" },
                new Currency { Name = "AED" },
                new Currency { Name = "UYU" },
                new Currency { Name = "BHD" },
                new Currency { Name = "PYG" },
                new Currency { Name = "QAR" },
                new Currency { Name = "BOB" },
                new Currency { Name = "HNL" },
                new Currency { Name = "NIO" },
                new Currency { Name = "ETB" },
                new Currency { Name = "AFN" },
                new Currency { Name = "BDT" },
                new Currency { Name = "XOF" },
                new Currency { Name = "RWF" },
                new Currency { Name = "RUB" },
                new Currency { Name = "NPR" },
                new Currency { Name = "RSD" },
                new Currency { Name = "LKR" },
                new Currency { Name = "LAK" },
                new Currency { Name = "KHR" },
                new Currency { Name = "TMT" },
                new Currency { Name = "BAM" },
                new Currency { Name = "TJS" },
                new Currency { Name = "CNH" } };

            context.Currencies.AddRange(currencies);

            context.SaveChanges();

            var tags = new List<Tag>
            {
                new Tag { Name = "Side: Long" },
                new Tag { Name = "Side: Short" },
                new Tag { Name = "Side: Long/Short" },
                new Tag { Name = "Market: Developed" },
                new Tag { Name = "Market: Emerging" },
                new Tag { Name = "Length: Intraday" },
                new Tag { Name = "Length: Overnight" },
                new Tag { Name = "Length: Swing" },
                new Tag { Name = "Length: Long Term" },
                new Tag { Name = "Asset class: Equities" },
                new Tag { Name = "Asset class: Futures" },
                new Tag { Name = "Asset class: Options" },
                new Tag { Name = "Asset class: Bonds" },
                new Tag { Name = "Asset class: Other" }
            };

            context.Tags.AddRange(tags);

            var preferredDatasources = new List<DatasourcePreference>
            {
                new DatasourcePreference { AssetClass = AssetClass.Bag, Datasource = "Interactive Brokers"},
                new DatasourcePreference { AssetClass = AssetClass.Bill, Datasource = "Interactive Brokers"},
                new DatasourcePreference { AssetClass = AssetClass.Bond, Datasource = "Interactive Brokers"},
                new DatasourcePreference { AssetClass = AssetClass.Cash, Datasource = "Interactive Brokers"},
                new DatasourcePreference { AssetClass = AssetClass.CFD, Datasource = "Interactive Brokers"},
                new DatasourcePreference { AssetClass = AssetClass.Commodity, Datasource = "Interactive Brokers"},
                new DatasourcePreference { AssetClass = AssetClass.Future, Datasource = "Interactive Brokers"},
                new DatasourcePreference { AssetClass = AssetClass.FutureOption, Datasource = "Interactive Brokers"},
                new DatasourcePreference { AssetClass = AssetClass.Index, Datasource = "Interactive Brokers"},
                new DatasourcePreference { AssetClass = AssetClass.Option, Datasource = "Interactive Brokers"},
                new DatasourcePreference { AssetClass = AssetClass.Stock, Datasource = "Yahoo"},
                new DatasourcePreference { AssetClass = AssetClass.Warrant, Datasource = "Interactive Brokers"},
                new DatasourcePreference { AssetClass = AssetClass.Undefined, Datasource = "Interactive Brokers"},
            };

            context.DatasourcePreferences.AddRange(preferredDatasources);

            context.SaveChanges();
        }
    }
}