using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Xml.Serialization;

namespace QPAS
{
    /// <summary>
    /// Extension methods for DataGrid
    /// These methods are thanks to http://blogs.msdn.com/b/vinsibal/archive/2008/11/05/wpf-datagrid-new-item-template-sample.aspx
    /// </summary>
    public static class DataGridExtensions
    {
        /// <summary>
        /// Returns a DataGridCell for the given row and column
        /// </summary>
        /// <param name="grid">The DataGrid</param>
        /// <param name="row">The zero-based row index</param>
        /// <param name="column">The zero-based column index</param>
        /// <returns>The requested DataGridCell, or null if the indices are out of range</returns>
        public static DataGridCell GetCell(this DataGrid grid, Int32 row, Int32 column)
        {
            DataGridRow gridrow = grid.GetRow(row);
            if (gridrow != null)
            {
                DataGridCellsPresenter presenter = GetVisualChild<DataGridCellsPresenter>(gridrow);

                // try to get the cell but it may possibly be virtualized
                DataGridCell cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                if (cell == null)
                {
                    // now try to bring into view and retreive the cell
                    grid.ScrollIntoView(gridrow, grid.Columns[column]);

                    cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                }

                return (cell);
            }

            return (null);
        }

        /// <summary>
        /// Gets the DataGridRow based on the given index
        /// </summary>
        /// <param name="idx">The zero-based index of the container to get</param>
        public static DataGridRow GetRow(this DataGrid dataGrid, Int32 idx)
        {
            DataGridRow row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(idx);
            if (row == null)
            {
                // may be virtualized, bring into view and try again
                dataGrid.ScrollIntoView(dataGrid.Items[idx]);
                dataGrid.UpdateLayout();

                row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(idx);
            }

            return (row);
        }

        private static T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default(T);

            Int32 numvisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (Int32 i = 0; i < numvisuals; ++i)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                    child = GetVisualChild<T>(v);
                else
                    break;
            }

            return child;
        }
        public static string SerializeLayout(this DataGrid grid)
        {
            var allSettings = grid.Columns.Select(c => new ColumnOptions
            {
                DisplayIndex = c.DisplayIndex,
                Width = c.ActualWidth,
            }).ToList();

            var serializer = new XmlSerializer(typeof(List<ColumnOptions>));

            using (var sw = new StringWriter())
            {
                serializer.Serialize(sw, allSettings);
                return sw.ToString();
            }
        }

        public static void DeserializeLayout(this DataGrid grid, string settings)
        {
            List<ColumnOptions> allSettings;
            var serializer = new XmlSerializer(typeof(List<ColumnOptions>));
            using (var sw = new StringReader(settings))
            {
                allSettings = (List<ColumnOptions>)serializer.Deserialize(sw);
            }

            for (int i = 0; i < allSettings.Count; i++)
            {
                ColumnOptions co = allSettings[i];
                grid.Columns[i].Width = co.Width;
                grid.Columns[i].DisplayIndex = Math.Max(0, co.DisplayIndex);
            }
        }
    }
}
