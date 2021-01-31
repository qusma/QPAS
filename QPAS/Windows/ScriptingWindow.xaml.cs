using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace QPAS
{
    /// <summary>
    /// Interaction logic for ScriptingWindow.xaml
    /// </summary>
    public partial class ScriptingWindow : MetroWindow
    {
        public ScriptingViewModel ViewModel { get; set; }

        public ScriptingWindow(IContextFactory contextFactory, IScriptRunner scriptRunner, DataContainer data)
        {
            InitializeComponent();

            ViewModel = new ScriptingViewModel(contextFactory, scriptRunner, data, DialogCoordinator.Instance);
            DataContext = ViewModel;
        }
    }
}
