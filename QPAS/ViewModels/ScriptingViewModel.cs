// -----------------------------------------------------------------------
// <copyright file="ScriptingViewModel.cs" company="">
// Copyright 2015 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace QPAS
{
    public class ScriptingViewModel : ViewModelBase
    {
        private IDialogCoordinator _dialogService;
        private IContextFactory _contextFactory;
        private readonly IScriptRunner _scriptRunner;
        private readonly DataContainer _data;

        public bool ChangedSinceLastSave
        {
            get => _changedSinceLastSave;
            private set => this.RaiseAndSetIfChanged(ref _changedSinceLastSave, value);
        }

        private UserScript _selectedScript;
        private string _status;
        private string _code;
        private bool _changedSinceLastSave;
        private bool _statusOk;

        public ObservableCollection<UserScript> Scripts { get; }

        public string Status
        {
            get => _status;
            private set => this.RaiseAndSetIfChanged(ref _status, value);
        }

        public bool StatusOk
        {
            get => _statusOk;
            private set => this.RaiseAndSetIfChanged(ref _statusOk, value);
        }

        public UserScript SelectedScript
        {
            get => _selectedScript;
            private set => this.RaiseAndSetIfChanged(ref _selectedScript, value);
        }

        public string Code
        {
            get => _code;
            set
            {
                if (value == _code) return;
                ChangedSinceLastSave = true;
                this.RaiseAndSetIfChanged(ref _code, value);
                if (SelectedScript != null) SelectedScript.Code = value;
            }
        }

        public ICommand Test { get; }
        public ICommand Open { get; }
        public ICommand NewScript { get; }
        public ICommand DeleteScript { get; }
        public ICommand Save { get; }
        public ICommand AddReference { get; }
        public ICommand RemoveReference { get; }
        public ICommand LaunchHelp { get; }

        public ScriptingViewModel(IContextFactory contextFactory, IScriptRunner scriptRunner, DataContainer data, IDialogCoordinator dialogService)
        {
            _dialogService = dialogService;
            _contextFactory = contextFactory;
            _scriptRunner = scriptRunner;
            _data = data;
            using (var dbContext = _contextFactory.Get())
            {
                Scripts = new ObservableCollection<UserScript>(dbContext.UserScripts.ToList());
            }

            Test = ReactiveCommand.CreateFromTask(async () => await RunTest());
            Open = new RelayCommand<UserScript>(OpenScript);
            NewScript = new RelayCommand(CreateNewScript);
            DeleteScript = new RelayCommand(DeleteSelectedScript);
            Save = new RelayCommand(SaveScripts);
            AddReference = new RelayCommand(AddReferencedAssembly);
            RemoveReference = new RelayCommand<string>(RemoveReferencedAssembly);
            LaunchHelp = new RelayCommand(() =>
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "https://github.com/qusma/QPAS/wiki",
                    UseShellExecute = true
                };
                Process.Start(psi);
            });
        }

        private async void DeleteSelectedScript()
        {
            if (SelectedScript == null) return;

            MessageDialogResult result = await _dialogService.ShowMessageAsync(
                this,
                "Delete Script",
                "Are you sure you want to delete the script " + SelectedScript.Name + "?",
                MessageDialogStyle.AffirmativeAndNegative);

            if (result == MessageDialogResult.Negative) return;

            using (var dbContext = _contextFactory.Get())
            {
                dbContext.UserScripts.Remove(SelectedScript);
                dbContext.SaveChanges();
            }

            Scripts.Remove(SelectedScript);

            Code = "";

            SelectedScript = null;
        }

        private void AddReferencedAssembly()
        {
            if (SelectedScript == null) return;

            string filePath;
            bool? success = Dialogs.OpenFileDialog("DLL Files (*.dll)|*.dll", out filePath);
            if (success.HasValue && success.Value)
            {
                if (SelectedScript.ReferencedAssemblies.Contains(filePath)) return;

                SelectedScript.ReferencedAssemblies.Add(filePath);
            }
        }

        private void RemoveReferencedAssembly(string path)
        {
            if (SelectedScript == null) return;

            if (SelectedScript.ReferencedAssemblies.Contains(path))
            {
                SelectedScript.ReferencedAssemblies.Remove(path);
            }
        }

        private async Task RunTest()
        {
            if (SelectedScript == null) return;

            try
            {
                if (SelectedScript.Type == UserScriptType.TradeScript)
                {
                    var actions = await _scriptRunner.GetTradeScriptActions(SelectedScript);
                    Status = "Run complete. Results: \n" + string.Join('\n', actions.Select(x => x.ToString()));
                }
                else if (SelectedScript.Type == UserScriptType.OrderScript)
                {
                    var orders = _data.Orders.Where(x => x.Trade == null).ToList();
                    var actions = await _scriptRunner.GetOrderScriptActions(SelectedScript, orders);
                    Status = "Run complete. Results: \n" + string.Join('\n', actions.Select(x => x.ToString()));
                }
                StatusOk = true;
            }
            catch (Exception ex)
            {
                Status = ex.Message + "\n" + ex.StackTrace;
                StatusOk = false;
            }
        }

        private void OpenScript(UserScript script)
        {
            if (SelectedScript == script) return;

            HandleUnsavedChanges();

            SelectedScript = script;
            Code = SelectedScript.Code;
            ChangedSinceLastSave = false;
        }

        private void SaveScripts()
        {
            if (SelectedScript == null) return;

            ChangedSinceLastSave = false;
            using (var dbContext = _contextFactory.Get())
            {
                dbContext.Entry(SelectedScript).State = EntityState.Modified;
                dbContext.SaveChanges();
            }
        }

        private async void HandleUnsavedChanges()
        {
            if (!ChangedSinceLastSave) return;

            MessageDialogResult res = await _dialogService
                .ShowMessageAsync(
                    this,
                    "Unsaved Changes",
                    "There are unsaved changes, would you like to save them?",
                    MessageDialogStyle.AffirmativeAndNegative);

            if (res == MessageDialogResult.Affirmative)
            {
                SaveScripts();
            }
            else
            {
                //User doesn't want to save, so we roll back
                using (var dbContext = _contextFactory.Get())
                {
                    var entry = dbContext.Entry(SelectedScript);
                    entry.CurrentValues.SetValues(entry.OriginalValues);
                    entry.State = EntityState.Unchanged;
                }
                ChangedSinceLastSave = false;
            }
        }

        private void CreateNewScript()
        {
            HandleUnsavedChanges();

            //Build a dialog for the user to enter the script name and type
            var dialog = new CustomDialog();

            StackPanel panel = new StackPanel();

            Label label = new Label() { Content = "Enter script name:" };
            TextBox textBox = new TextBox();

            StackPanel radioBtnPanel = new StackPanel { Orientation = Orientation.Horizontal };
            var orderRadioBtn = new RadioButton { Content = "Order Script", IsChecked = true, Margin = new Thickness(5) };
            var tradeRadioBtn = new RadioButton { Content = "Trade Script", IsChecked = false, Margin = new Thickness(5) };
            radioBtnPanel.Children.Add(orderRadioBtn);
            radioBtnPanel.Children.Add(tradeRadioBtn);

            UniformGrid btnPanel = new UniformGrid { Columns = 2, HorizontalAlignment = HorizontalAlignment.Stretch };
            Button nextBtn = new Button { Content = "Create Script", Margin = new Thickness(5) };
            nextBtn.Click += (s, e) =>
                {
                    string scriptType = (orderRadioBtn.IsChecked.HasValue && orderRadioBtn.IsChecked.Value) ? "Order" : "Trade";
                    CreateNewScript(textBox.Text, scriptType);
                    _dialogService.HideMetroDialogAsync(this, dialog);

                };

            Button cancelBtn = new Button { Content = "Cancel", Margin = new Thickness(5) };
            cancelBtn.Click += (s, e) =>
                {
                    _dialogService.HideMetroDialogAsync(this, dialog);
                };

            btnPanel.Children.Add(nextBtn);
            btnPanel.Children.Add(cancelBtn);


            panel.Children.Add(label);
            panel.Children.Add(textBox);
            panel.Children.Add(radioBtnPanel);
            panel.Children.Add(btnPanel);

            dialog.Content = panel;

            //There is no point to awaiting this one, it returns immediately
            _dialogService.ShowMetroDialogAsync(this, dialog);
        }

        private void CreateNewScript(string name, string type)
        {
            if (string.IsNullOrEmpty(name)) return;

            using (var dbContext = _contextFactory.Get())
            {
                if (dbContext.UserScripts.Any(x => x.Name == name))
                {
                    _dialogService.ShowMessageAsync(this, "Script Already Exists", "A script with that name already exists.");
                    return;
                }

                //If there are unsaved changes, prompt to save before making a new script
                HandleUnsavedChanges();

                //test text for uniqueness
                var script = new UserScript();
                script.Name = name;

                AddStandardAssemblies(script);

                script.Code = string.Format(type == "Order"
                    ? OrderScriptBase
                    : TradeScriptBase, name);

                script.Type = type == "Order"
                    ? UserScriptType.OrderScript
                    : UserScriptType.TradeScript;

                dbContext.UserScripts.Add(script);
                Scripts.Add(script);
                SelectedScript = script;
                Code = SelectedScript.Code;
                ChangedSinceLastSave = false;

                dbContext.SaveChanges();
            }
        }

        private void AddStandardAssemblies(UserScript script)
        {
            script.ReferencedAssemblies.Add("mscorlib.dll");
            script.ReferencedAssemblies.Add("System.dll");
            script.ReferencedAssemblies.Add("System.Collections.dll");
            script.ReferencedAssemblies.Add("System.Core.dll");

            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            script.ReferencedAssemblies.Add(exePath);
            string entityModelDllPath = Path.Combine(Path.GetDirectoryName(exePath), "EntityModel.dll");
            script.ReferencedAssemblies.Add(entityModelDllPath);
            string nlogPath = Path.Combine(Path.GetDirectoryName(exePath), "NLog.dll");
            script.ReferencedAssemblies.Add(nlogPath);
        }

        private const string OrderScriptBase =
@"using System;
using System.Collections.Generic;
using QPAS;
using QPAS.Scripting;
using EntityModel;
using System.Linq;
using NLog;

public class {0} : OrderScriptBase
{{
    //Do not change the constructor parameters.
    public {0}(DataContainer data, ILogger logger) : base(data, logger)
    {{
    }}

    public override void ProcessOrders(List<Order> orders)
    {{
        //logic goes here
        //Do not manipulate trades/orders/etc directly.
        //Available functions:
        //  Trade CreateTrade(string name)
        //  bool SetTrade(Order order, int tradeID)
        //  bool SetTrade(Order order, string tradeName)
        //  bool SetTrade(Order order, Trade trade)
        //  void Log(string message)
        //see https://github.com/qusma/QPAS/wiki/Scripting for more info
    }}
}}";

        private const string TradeScriptBase =
@"using System;
using System.Collections.Generic;
using QPAS;
using QPAS.Scripting;
using EntityModel;
using System.Linq;
using NLog;

public class {0} : TradeScriptBase
{{
    //Do not change the constructor parameters.
    public {0}(DataContainer data, ILogger logger) : base(data, logger)
    {{
    }}

    public override void ProcessTrades(List<Trade> openTrades)
    {{
        //logic goes here
        //Existing tags and strategies can be found in the Tags and Strategies properties of this object.
        //Do not manipulate trades/orders/etc directly.
        //Available functions: 
        //  void SetTag(Trade, string), void SetTag(Trade, Tag)
        //  void SetStrategy(Trade, string), void SetStrategy(Trade, Strategy)
        //  void CloseTrade(Trade)
        //  void Log(string message)
        //see https://github.com/qusma/QPAS/wiki/Scripting for more info
    }}
}}";
    }
}
