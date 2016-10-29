// -----------------------------------------------------------------------
// <copyright file="StatementHandler.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

//This class loads IStatementParser and IStatementDownloader classes,
//from this assembly and plugins in the /Plugins folder.
//These classes can then be used to download and parse broker statements.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EntityModel;
using MahApps.Metro.Controls.Dialogs;
using System.ComponentModel.Composition;

namespace QPAS
{
    public class StatementHandler
    {
        private readonly IDialogCoordinator _dialogService;
        private readonly ITradesRepository _tradeRepository;
        private readonly IMainViewModel _mainVm;

        [ImportMany(typeof(IStatementParser))]
        private IEnumerable<Lazy<IStatementParser, IPlugin>> StatementParsers { get; set; }

        [ImportMany(typeof(IStatementDownloader))]
        private IEnumerable<Lazy<IStatementDownloader, IPlugin>> StatementDownloaders { get; set; }

        public List<string> DownloaderNames { get; private set; }
        public List<string> ParserNames { get; private set; }

        public StatementHandler(IDBContext context, IDialogCoordinator dialogService, IDataSourcer dataSourcer, ITradesRepository repository, IMainViewModel mainVm)
        {
            _dialogService = dialogService;

            AssembleComponents();

            DownloaderNames = StatementDownloaders.Select(x => x.Metadata.Name).ToList();
            ParserNames = StatementParsers.Select(x => x.Metadata.Name).ToList();

            _tradeRepository = repository;
            _mainVm = mainVm;
        }

        /// <summary>
        /// This method loads the plugins.
        /// </summary>
        private void AssembleComponents()
        {
            var catalog = new AggregateCatalog();

            //Note: we load not only from the plugins folder, but from this assembly as well.
            var executingAssemblyCatalog = new AssemblyCatalog(Assembly.GetExecutingAssembly());

            if (Directory.Exists(Environment.CurrentDirectory + "\\Plugins"))
            {
                catalog.Catalogs.Add(new DirectoryCatalog("Plugins"));
            }

            catalog.Catalogs.Add(executingAssemblyCatalog);

            var container = new CompositionContainer(catalog);

            try
            {
                container.ComposeParts(this);
            }
            catch (CompositionException compositionException)
            {
                _dialogService.ShowMessageAsync(_mainVm, "Error", string.Format("There was an error loading plugins: {0}", compositionException)).Forget();
            }
        }

        private async Task<IStatementDownloader> GetDownloaderByName(string name)
        {
            if (!DownloaderNames.Contains(name))
            {
                await _dialogService.ShowMessageAsync(_mainVm, "Error", "Statement downloader not found.");
                return null;
            }

            return StatementDownloaders.FirstOrDefault(x => x.Metadata.Name == name).Value;
        }

        private async Task<IStatementParser> GetParserByName(string name)
        {
            if (!ParserNames.Contains(name))
            {
                await _dialogService.ShowMessageAsync(_mainVm, "Error", "Statement parser not found.");
                return null;
            }

            return StatementParsers.FirstOrDefault(x => x.Metadata.Name == name).Value;
        }

        /// <summary>
        /// Downloads a statement from the net and then parses it.
        /// </summary>
        /// <param name="name">The name of the parser and downloader.</param>
        public async Task LoadFromWeb(string name)
        {
            var downloader = await GetDownloaderByName(name).ConfigureAwait(false);
            if (downloader == null) return;
            var parser = await GetParserByName(name).ConfigureAwait(false);
            if (parser == null) return;

            ProgressDialogController progress = await _dialogService.ShowProgressAsync(_mainVm, "Load Statement from Web", "Downloading").ConfigureAwait(false);

            Exception ex = null;
            string flex = "";
            try
            {
                flex = await downloader.DownloadStatement().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                ex = e;
            }

            if (flex == "" || ex != null)
            {
                await progress.CloseAsync();
                await _dialogService.ShowMessageAsync(_mainVm, "Error downloading statement", ex?.Message);

                return;
            }

            await Task.Run(() => parser.Parse(flex, progress));

            progress.SetMessage("Updating open trades");

            _tradeRepository.UpdateOpenTrades();
            _tradeRepository.Save();

            progress.CloseAsync().Forget();
        }

        /// <summary>
        /// Opens a file dialog to select a file, then parses the contents.
        /// </summary>
        /// <param name="name">The name of the parser.</param>
        public async Task LoadFromFile(string name)
        {
            var parser = await GetParserByName(name);
            if (parser == null) return;

            string file;
            bool? res = Dialogs.OpenFileDialog(parser.GetFileFilter(), out file);

            if (res != true) return;

            ProgressDialogController progress = await _dialogService.ShowProgressAsync(_mainVm, "Load Statement from File", "Opening File");
            string flexqText = "";
            try
            {
                flexqText = File.ReadAllText(file);
            }
            catch (IOException)
            {
                progress.CloseAsync().Forget();
                _dialogService.ShowMessageAsync(_mainVm, "Error", "Could not open file.").Forget();
                return;
            }

            await Task.Run(() => parser.Parse(flexqText, progress));

            progress.SetMessage("Updating open trades");

            _tradeRepository.UpdateOpenTrades();
            _tradeRepository.Save();

            progress.CloseAsync().Forget();
        }
    }
}
