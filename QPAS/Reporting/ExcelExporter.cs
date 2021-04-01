// ----------------------------------------------------------------------
// <copyright file="ExcelExporter.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using QPAS.DataSets;
using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;

namespace QPAS
{
    public class ExcelExporter
    {
        private filterReportDS _ds;

        public ExcelExporter()
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="options"></param>
        /// <returns>The path of the exported file. Empty if not saved.</returns>
        public string Export(filterReportDS ds, ExportOptions options)
        {
            _ds = ds;

            //initialize the package using the template from the resources
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream templateStream = assembly.GetManifestResourceStream("QPAS.Resources.ExportTemplate.xlsx"))
            using (var packageStream = new MemoryStream())
            using (var xlPackage = new ExcelPackage(packageStream, templateStream))
            {
                var wb = xlPackage.Workbook;

                //Loop through the options. If they're enable, call the exporting function
                //assigned to it through the _exportMethodMap dictionary
                var sheetNames = wb.Worksheets.Select(x => x.Name).ToList();
                foreach (string sheetName in sheetNames)
                {
                    if (options.SelectedItems.Contains(sheetName))
                    {
                        InsertData(sheetName, wb);
                    }
                    else
                    {
                        //not enabled -- delete the worksheet
                        //wb.Worksheets.Delete(wb.Worksheets[sheetName].Index);
                    }
                }

                //save to file
                var dialog = new Microsoft.Win32.SaveFileDialog();
                dialog.Filter = "Excel files (.xlxs)|*.xlsx";
                if (dialog.ShowDialog() == true)
                {
                    using (var fileStream = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write))
                    {
                        xlPackage.SaveAs(fileStream);
                    }
                    return dialog.FileName;
                }
            }

            return "";
        }

        private void InsertData(string sheetName, ExcelWorkbook wb)
        {
            var ws = wb.Worksheets[sheetName];
            if (!ws.Names.ContainsKey("HeaderRow")) return; //Without the HeaderRow name we can't insert data

            int rowCount = 0;

            //Find the header row
            int headerRow = ws.Names["HeaderRow"].Start.Row;

            //Find the headers and insert data for each of them
            int column = 1;
            while (!String.IsNullOrEmpty(ws.Cells[headerRow, column].Text))
            {
                //Parse the thing
                string columnFormat = ws.Cells[headerRow, column].Value.ToString();
                if (columnFormat.Length < 7)
                {
                    //It's gotta have at least 7 characters to be a valid set of options
                    column++;
                    continue;
                }

                string[] opts = columnFormat.Substring(1, columnFormat.Length - 2).Split(";".ToCharArray());
                if (opts.Length < 4)
                {
                    //Mis-specified options, skip this one
                    column++;
                    continue;
                }

                string table = opts[0];
                string tableColName = opts[1];
                string format = opts[2];
                string colTitle = opts[3];
                string styleSettings = opts.Length >= 5 ? opts[4] : "";

                if (string.IsNullOrEmpty(tableColName))
                {
                    //Dump entire table, including headers
                    InsertFullTable(ws, headerRow, column, _ds.Tables[table], format);
                }
                else
                {
                    //Dump just a single column
                    int tmpRowCount = InsertSingleColumn(ws, headerRow, column, _ds.Tables[table], format, tableColName, colTitle);
                    if (column == 1) rowCount = tmpRowCount;
                }

                //After we put in the data, apply any required styling to it
                ApplyColumnStyleSettings(ws, styleSettings, new ExcelAddress(headerRow + 1, column, headerRow + 1 + _ds.Tables[table].Rows.Count, column));

                column++;
            }

            SetChartSeriesLengths(ws, headerRow, rowCount);
        }

        private int InsertSingleColumn(ExcelWorksheet ws, int headerRow, int column, DataTable table, string format, string colName, string colTitle)
        {
            var items = table.AsEnumerable().Select(x => x[colName]);
            int rowCount = 0;
            if (column == 1) rowCount = table.Rows.Count;

            //Fill the column with the values
            ws.Cells[headerRow + 1, column, headerRow + 1 + table.Rows.Count, column].LoadFromCollection(items, false);
            ws.Cells[headerRow + 1, column, headerRow + 1 + table.Rows.Count, column].Style.Numberformat.Format = format;

            //replace the options thing with the column title
            ws.Cells[headerRow, column].Value = colTitle;

            if (column > 1) //We don't want to autosize the first column
            {
                ws.Column(column).AutoFit();
            }

            return rowCount;
        }

        private void InsertFullTable(ExcelWorksheet ws, int headerRow, int column, DataTable table, string format)
        {
            ws.Cells[headerRow, column, headerRow + table.Rows.Count - 1, column + table.Columns.Count - 1].LoadFromDataTable(table, true);

            if (string.IsNullOrEmpty(format))
            {
                //This is a bit hacky, but what can you do...
                //we have to guess the format of the columns because we can't provide them directly in the template
                GuessColumnFormats(ws, headerRow, column, table);
            }
            else
            {
                ws.Cells[headerRow + 1, column, headerRow + 1 + table.Rows.Count, column + table.Columns.Count].Style.Numberformat.Format = format;
            }

            for (int i = 0; i < table.Columns.Count; i++)
            {
                if (column + i == 1) continue; //We don't want to autosize the first column
                ws.Column(column + i).AutoFit();
            }
        }

        private void GuessColumnFormats(ExcelWorksheet ws, int headerRow, int column, DataTable table)
        {
            for (int i = 0; i < table.Columns.Count; i++)
            {
                DataColumn dc = table.Columns[i];
                string format = "";

                //depending on the type of the column we choose a format
                if (dc.DataType == typeof(decimal))
                {
                    format = "$0.00";
                }
                else if (dc.DataType == typeof(double))
                {
                    //check the range of the numbers...if it's small, it's probably %, otherwise it's $
                    double max = table.AsEnumerable().Select(x => Math.Abs((double)x[i])).Max();
                    if (max > 50)
                    {
                        format = "$0.00";
                    }
                    else
                    {
                        format = "0.00%";
                    }
                }
                else if (dc.DataType == typeof(DateTime))
                {
                    format = "yyyy-MM-dd";
                }

                ws.Cells[headerRow + 1, column + i, headerRow + 1 + table.Rows.Count, column + i].Style.Numberformat.Format = format;
            }
        }

        /// <summary>
        /// Set chart series lengths
        /// We implicitly assume they're all the same
        /// </summary>
        private void SetChartSeriesLengths(ExcelWorksheet ws, int headerRow, int rowCount)
        {
            foreach (var excelDrawing in ws.Drawings.Where(x => x.GetType().IsSubclassOf(typeof(ExcelChart))))
            {
                var chart = (ExcelChart)excelDrawing;
                foreach (var chartType in chart.PlotArea.ChartTypes)
                {
                    foreach (ExcelChartSerie serie in chartType.Series)
                    {
                        var yReferenceAddress = new ExcelAddress(serie.Series);
                        if (yReferenceAddress.Address == "") continue;
                        var yAddress = new ExcelAddress(headerRow + 1, yReferenceAddress.Start.Column, headerRow + 1 + rowCount, yReferenceAddress.End.Column);
                        serie.Series = yAddress.Address;

                        var xReferenceAddress = new ExcelAddress(serie.XSeries);
                        if (xReferenceAddress.Address == "") continue;
                        var xAddress = new ExcelAddress(headerRow + 1, xReferenceAddress.Start.Column, headerRow + 1 + rowCount, xReferenceAddress.End.Column);
                        serie.XSeries = xAddress.Address;
                    }
                }
            }
        }

        /// <summary>
        /// Parses the style field and applies any settings within it.
        /// Style selections should be separated by the | character.
        /// </summary>
        /// <param name="ws"></param>
        /// <param name="styleSettings"></param>
        /// <param name="address"></param>
        private void ApplyColumnStyleSettings(ExcelWorksheet ws, string styleSettings, ExcelAddress address)
        {
            if (string.IsNullOrEmpty(styleSettings)) return;
            string[] settings = styleSettings.Split("|".ToCharArray());
            if (settings.Contains("bold"))
            {
                ws.Cells[address.Address].Style.Font.Bold = true;
            }
        }
    }
}