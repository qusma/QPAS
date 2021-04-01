// -----------------------------------------------------------------------
// <copyright file="ExcelFormatHelper.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using OfficeOpenXml;
using System.Collections.Generic;

namespace QPAS
{
    /// <summary>
    /// Give this class a worksheet with a row of formatted cells.
    /// It saves that formatting and then applies it to an entire columnar range.
    /// Used to grab formatting from a template and then apply it to data.
    /// </summary>
    public class ExcelFormatHelper
    {
        private readonly Dictionary<int, string> _formats;

        private readonly int _startRow;
        private readonly int _startCol;

        public ExcelFormatHelper(string addressStart, int columns, ExcelWorksheet ws)
        {
            var startCell = ws.Cells[addressStart];
            _startCol = startCell.Start.Column;
            _startRow = startCell.Start.Row;

            _formats = new Dictionary<int, string>();

            for (int i = 0; i < columns; i++)
            {
                int col = _startCol + i;
                _formats.Add(col, ws.Cells[_startRow, col].Style.Numberformat.Format);
            }
        }

        public void Apply(ExcelWorksheet ws, int rows)
        {
            for (int i = 0; i < _formats.Count; i++)
            {
                ws.Cells[_startRow, _startCol, _startRow + rows, _startCol + i].Style.Numberformat.Format = _formats[i];
            }
        }
    }
}