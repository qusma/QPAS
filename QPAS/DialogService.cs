// -----------------------------------------------------------------------
// <copyright file="DialogService.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Win32;

namespace QPAS
{
    public static class Dialogs
    {
        public static bool? OpenFileDialog(string filter, out string filePath)
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.Filter = filter;
            bool? result = fd.ShowDialog();
            if (result.HasValue && result.Value)
            {
                filePath = fd.FileName;
            }
            else
            {
                filePath = "";
            }

            return result;
        }
    }
}