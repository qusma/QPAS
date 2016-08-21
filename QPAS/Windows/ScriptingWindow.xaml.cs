using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using EntityModel;
using MahApps.Metro.Controls;

namespace QPAS
{
    /// <summary>
    /// Interaction logic for ScriptingWindow.xaml
    /// </summary>
    public partial class ScriptingWindow : MetroWindow
    {
        public ScriptingWindow(IDBContext context)
        {
            InitializeComponent();

        }
    }
}
