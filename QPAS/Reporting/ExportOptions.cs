// -----------------------------------------------------------------------
// <copyright file="ExportOptions.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QPAS
{
    public class ExportOptions
    {
        public List<string> SelectedItems
        {
            get
            {
                return CheckList.Where(x => x.IsChecked).Select(x => (string)x.Tag).ToList();
            }
        }
        public ObservableCollection<CheckListItem<string>> CheckList { get; private set; }

        private Dictionary<string, string> _optionToTitleMap;

        public ExportOptions()
        {
            CheckList = new ObservableCollection<CheckListItem<string>>();
            _optionToTitleMap = new Dictionary<string, string>();

            PopulateOptionsList();

            foreach (var kvp in _optionToTitleMap)
            {
                var item = new CheckListItem<string>(kvp.Value, true);
                item.Tag = kvp.Key;
                CheckList.Add(item);
            }
        }

        private void PopulateOptionsList()
        {
            _optionToTitleMap.Add("PerformanceStats", "Performance Stats");
            _optionToTitleMap.Add("TradeStats", "Trade Stats");
            _optionToTitleMap.Add("CumulativePL", "Cumulative Profit/Loss");
            _optionToTitleMap.Add("CumulativeRets", "Cumulative Returns");
            _optionToTitleMap.Add("CapUsage", "Capital Usage");
            _optionToTitleMap.Add("PLByMonth", "Profit/Loss by Month");
            _optionToTitleMap.Add("ROACByMonth", "ROAC by Month");
            _optionToTitleMap.Add("ROTCByMonth", "ROTC by Month");
            _optionToTitleMap.Add("StratCorr", "Strategy Correlations");
            _optionToTitleMap.Add("MAE", "Maximum Adverse Excursion");
            _optionToTitleMap.Add("MFE", "Maximum Favorable Excursion");
            _optionToTitleMap.Add("CashTransactions", "Cash Transactions");
            _optionToTitleMap.Add("RoacByInstrument", "ROAC by Instrument");
            _optionToTitleMap.Add("TradeSizes", "Trade Sizes vs Returns");
            _optionToTitleMap.Add("ACF", "Autocorrelation");
            _optionToTitleMap.Add("PACF", "Partial Autocorrelation");
            _optionToTitleMap.Add("VaR", "Value at Risk");
            _optionToTitleMap.Add("ES", "Expected Shortfall");
        }
    }
}
