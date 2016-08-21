// -----------------------------------------------------------------------
// <copyright file="ScriptingViewModel.cs" company="">
// Copyright 2015 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using EntityModel;
using MahApps.Metro.Controls.Dialogs;
using QPAS.Scripting;

namespace QPAS
{
    public class ScriptingViewModel : ViewModelBase
    {
        private IDialogCoordinator _dialogService;
        private IDBContext _dbContext;

        public bool ChangedSinceLastSave
        {
            get { return _changedSinceLastSave; }
            private set
            {
                if (value == _changedSinceLastSave) return;
                _changedSinceLastSave = value;
                OnPropertyChanged();
            }
        }

        private UserScript _selectedScript;
        private string _compileStatus;
        private string _code;
        private bool _changedSinceLastSave;
        public ObservableCollection<UserScript> Scripts { get; private set; }

        public string CompileStatus
        {
            get { return _compileStatus; }
            private set
            {
                if (value == _compileStatus) return;
                _compileStatus = value;
                OnPropertyChanged();
            }
        }

        public UserScript SelectedScript
        {
            get { return _selectedScript; }
            private set
            {
                if (Equals(value, _selectedScript)) return;
                _selectedScript = value;
                OnPropertyChanged();
            }
        }

        public string Code
        {
            get { return _code; }
            set
            {
                if (value == _code) return;
                _code = value;
                ChangedSinceLastSave = true;
                OnPropertyChanged();
                if (SelectedScript != null) SelectedScript.Code = value;
            }
        }

        public ICommand Compile { get; private set; }
        public ICommand Open { get; private set; }
        public ICommand NewScript { get; private set; }
        public ICommand DeleteScript { get; private set; }
        public ICommand Save { get; private set; }
        public ICommand AddReference { get; private set; }
        public ICommand RemoveReference { get; private set; }
        public ICommand LaunchHelp { get; private set; }

        public ScriptingViewModel(IDBContext context, IDialogCoordinator dialogService)
        {
            Scripts = new ObservableCollection<UserScript>(context.UserScripts.ToList());
            _dialogService = dialogService;
            _dbContext = context;

            Compile = new RelayCommand(RunCompile);
            Open = new RelayCommand<UserScript>(OpenScript);
            NewScript = new RelayCommand(CreateNewScript);
            DeleteScript = new RelayCommand(DeleteSelectedScript);
            Save = new RelayCommand(SaveScripts);
            AddReference = new RelayCommand(AddReferencedAssembly);
            RemoveReference = new RelayCommand<string>(RemoveReferencedAssembly);
            LaunchHelp = new RelayCommand(() => System.Diagnostics.Process.Start("http://qusma.com/qpasdocs/index.php/Scripting"));
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

            _dbContext.UserScripts.Remove(SelectedScript);
            _dbContext.SaveChanges();

            Scripts.Remove(SelectedScript);

            Code = "";

            SelectedScript = null;
        }

        private void AddReferencedAssembly()
        {
            if (SelectedScript == null) return;

            string filePath;
            bool? success = Dialogs.OpenFileDialog("DLL Files (*.dll)|*.dll", out filePath);
            if(success.HasValue && success.Value)
            {
                if (SelectedScript.ReferencedAssemblies.Contains(filePath)) return;

                SelectedScript.ReferencedAssemblies.Add(filePath);
            }
        }

        private void RemoveReferencedAssembly(string path)
        {
            if (SelectedScript == null) return;
            
            if(SelectedScript.ReferencedAssemblies.Contains(path))
            {
                SelectedScript.ReferencedAssemblies.Remove(path);
            }
        }

        private void RunCompile()
        {
            try
            {
                ScriptCompiler.CompileScriptsToDLL(Scripts);
                CompileStatus = "Compiled succesfully.";
            }
            catch(Exception ex)
            {
                CompileStatus = ex.Message;
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
            ChangedSinceLastSave = false;
            _dbContext.SaveChanges();
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

            if(res == MessageDialogResult.Affirmative)
            {
                SaveScripts();
            }
            else
            {
                //User doesn't want to save, so we roll back
                var entry = _dbContext.Entry(SelectedScript);
                entry.CurrentValues.SetValues(entry.OriginalValues);
                entry.State = EntityState.Unchanged;

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
            if(_dbContext.UserScripts.Any(x => x.Name == name))
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

            _dbContext.UserScripts.Add(script);
            Scripts.Add(script);
            SelectedScript = script;
            Code = SelectedScript.Code;
            SaveScripts();
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
            string qpasCommonPath = Path.Combine(Path.GetDirectoryName(exePath), "QPAS.Common.dll");
            script.ReferencedAssemblies.Add(qpasCommonPath);
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
    //Do not change the list of parameters.
    public {0}(ITradesRepository tradesRepository) : base(tradesRepository)
    {{
    }}

    public override void ProcessOrders(List<Order> orders)
    {{
        //logic goes here
        //see <DOC LINK HERE> for more info
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
    //Do not change the list of parameters.
    public {0}(ITradesRepository repository, List<Tag> tags, List<Strategy> strategies)
        : base(repository, tags, strategies)
    {{
    }}

    public override void ProcessTrades(List<Trade> openTrades)
    {{
        //logic goes here
        //see <DOC LINK HERE> for more info
    }}
}}";
    }
}
