using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace QPAS
{
    public class BacktestImportViewModel : ViewModelBase
    {
        public EquityCurve EquityCurve { get; set; }

        public string RawData
        {
            get => _rawData;
            private set => this.RaiseAndSetIfChanged(ref _rawData, value);
        }

        public string FilePath
        {
            get => _filePath;
            private set => this.RaiseAndSetIfChanged(ref _filePath, value);
        }

        public ObservableCollection<KeyValuePair<string, string>> RawSplitData { get; private set; }

        public string SelectedDelimiter
        {
            get => _selectedDelimiter;
            set => this.RaiseAndSetIfChanged(ref _selectedDelimiter, value);
        }

        public int SkipLines
        {
            get => _skipLines;
            set => this.RaiseAndSetIfChanged(ref _skipLines, value);
        }

        public string DateTimeFormat
        {
            get => _dateTimeFormat;
            set => this.RaiseAndSetIfChanged(ref _dateTimeFormat, value);
        }

        public RelayCommand OpenFileCmd { get; private set; }

        private IDialogCoordinator _dialogService;
        private string _selectedDelimiter;
        private string _rawData;
        private string _dateTimeFormat;
        private string _filePath;
        private int _skipLines;

        public BacktestImportViewModel(IDialogCoordinator dialogService)
        {
            _dialogService = dialogService;
            RawSplitData = new ObservableCollection<KeyValuePair<string, string>>();

            OpenFileCmd = new RelayCommand(OpenFile);

            DateTimeFormat = "yyyy-MM-dd";
            SkipLines = 1;

            this.WhenAny(x => x.SelectedDelimiter, x => x).Subscribe(_ => SplitData());
            this.WhenAny(x => x.SkipLines, x => x).Subscribe(_ => SplitData());
        }

        private void OpenFile()
        {
            string file;
            bool? res = Dialogs.OpenFileDialog("CSV Files (*.csv)|*.csv", out file);
            if (!res.HasValue || res.Value == false)
            {
                //No file
                return;
            }

            FilePath = file;

            //Open the file
            try
            {
                RawData = File.ReadAllText(file);
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessageAsync(this, "Error opening file.", ex.Message).Forget();
                return;
            }

            SplitData();
        }

        private void SplitData()
        {
            if (string.IsNullOrEmpty(SelectedDelimiter)) return;

            char[] delimiter = GetDelimiter();

            string[] lines = Regex.Split(RawData, @"(?:\r\n){1,}");
            RawSplitData.Clear();
            for (int index = SkipLines; index < lines.Length; index++)
            {
                string line = lines[index];
                string[] items = line.Split(delimiter);

                if (string.IsNullOrEmpty(items[0])) continue; //skip any empty lines

                var kvp = new KeyValuePair<string, string>(
                    items[0], items.Length > 1 ? items[1] : "");
                RawSplitData.Add(kvp);
            }
        }

        private char[] GetDelimiter()
        {
            char[] delimiter;
            if (SelectedDelimiter == "Space")
            {
                delimiter = " ".ToCharArray();
            }
            else if (SelectedDelimiter == "Tab")
            {
                delimiter = "\t".ToCharArray();
            }
            else
            {
                delimiter = SelectedDelimiter.ToCharArray();
            }
            return delimiter;
        }

        /// <summary>
        /// Returns false is parsing failed.
        /// </summary>
        /// <returns></returns>
        public bool Import()
        {
            var dates = new List<DateTime>();
            var equityValues = new List<double>();

            int count = 0;
            foreach (var kvp in RawSplitData)
            {
                DateTime date;
                if (!DateTime.TryParseExact(kvp.Key, DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    _dialogService.ShowMessageAsync(this, "Parsing error.",
                        string.Format("Failed to date value at line {0}: {1}", count, kvp.Key));
                    return false;
                }
                dates.Add(date);

                double equity;
                if (!double.TryParse(kvp.Value, out equity))
                {
                    _dialogService.ShowMessageAsync(this, "Parsing error.",
                        string.Format("Failed to parse equity value at line {0}: {1}", count, kvp.Value));
                    return false;
                }
                equityValues.Add(equity);

                count++;
            }

            //Make sure there's sufficient data
            if (dates.Count <= 1)
            {
                _dialogService.ShowMessageAsync(this, "Parsing error.", "Parsed backtest data too short.");
                EquityCurve = null;
                return false;
            }

            //Then generate an EquityCurve using the data
            EquityCurve = new EquityCurve(100, dates[0]);
            for (int i = 1; i < dates.Count; i++)
            {
                EquityCurve.AddReturn(equityValues[i] / equityValues[i - 1] - 1, dates[i]);
            }
            return true;
        }
    }
}