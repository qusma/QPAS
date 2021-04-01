// -----------------------------------------------------------------------
// <copyright file="IStatementParser.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using MahApps.Metro.Controls.Dialogs;
using System.Collections.Generic;

namespace QPAS
{
    public interface IStatementParser
    {
        /// <summary>
        /// The name of the parser.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Parses a statement and adds the data to the database.
        /// </summary>
        Dictionary<string, DataContainer> Parse(string statement, ProgressDialogController progress, IAppSettings settings, IEnumerable<Currency> curencies);

        /// <summary>
        /// Returns a string of the format "[filetype name] (*.[extension])|*.[extension]"
        /// showing the type of file that the parser accepts.
        /// </summary>
        string GetFileFilter();
    }
}