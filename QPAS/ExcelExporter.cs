// -----------------------------------------------------------------------
// <copyright file="ExcelExporter.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Data;
using OfficeOpenXml;
using QPAS.DataSets;

namespace QPAS
{
    public class ExcelExporter
    {
        public static void Export(filterReportDS ds)
        {
            using (ExcelPackage xlPackage = new ExcelPackage())
            {
                var ws = xlPackage.Workbook.Worksheets[0];
                var valuesRange = ws.Names[""];
                //ws.Cells[valuesRange.Address].LoadFromDataTable();

                //xlPackage.SaveAs("");
            }
        }

        private void ExportCumulativePL(ExcelWorkbook wb)
        {
            InsertData("CumulativePL", wb, new DataTable());
        }

        private void InsertData(string sheetName, ExcelWorkbook wb, DataTable data)
        {
            var ws = wb.Worksheets[sheetName];
            var dataStartAddress = ws.Names["DataHere"].Address;
            var formatHelper = new ExcelFormatHelper(dataStartAddress, data.Columns.Count, ws);
            ws.Cells[dataStartAddress].LoadFromDataTable(data, false);
            formatHelper.Apply(ws, data.Rows.Count);
        }
    }
}
