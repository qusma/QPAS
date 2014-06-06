// -----------------------------------------------------------------------
// <copyright file="IStatementDownloader.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

namespace QPAS
{
    public interface IStatementDownloader
    {
        string Name { get; }
        string DownloadStatement();
    }
}
