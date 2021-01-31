// -----------------------------------------------------------------------
// <copyright file="IStatementDownloader.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Threading.Tasks;

namespace QPAS
{
    public interface IStatementDownloader
    {
        string Name { get; }
        Task<string> DownloadStatement(IAppSettings settings);
    }
}
