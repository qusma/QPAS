// -----------------------------------------------------------------------
// <copyright file="StatementHandler.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using MahApps.Metro.Controls.Dialogs;
using NLog;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace QPAS
{
    /// <summary>
    /// This class loads IStatementParser and IStatementDownloader classes,
    /// from this assembly and plugins in the /Plugins folder.
    /// These classes can then be used to download and parse broker statements.
    /// </summary>
    public class StatementHandler
    {
        private readonly IDialogCoordinator _dialogService;
        private readonly IContextFactory _contextFactory;
        private readonly IAppSettings _settings;
        private readonly IMainViewModel _mainVm;
        private List<Currency> _currencies;
        private Logger _logger = LogManager.GetCurrentClassLogger();

        [ImportMany(nameof(IStatementParser))]
        private IEnumerable<ExportFactory<IStatementParser, PluginMetadata>> StatementParsers { get; set; }

        [ImportMany(nameof(IStatementDownloader))]
        private IEnumerable<ExportFactory<IStatementDownloader, PluginMetadata>> StatementDownloaders { get; set; }

        public List<string> DownloaderNames { get; private set; }
        public List<string> ParserNames { get; private set; }

        public StatementHandler(IDialogCoordinator dialogService, IContextFactory contextFactory, IAppSettings settings, IMainViewModel mainVm)
        {
            _dialogService = dialogService;
            _contextFactory = contextFactory;
            _settings = settings;
            _mainVm = mainVm;

            using (var dbContext = _contextFactory.Get())
            {
                _currencies = dbContext.Currencies.ToList();
            }

            AssembleComponents();

            DownloaderNames = StatementDownloaders.Select(x => x.Metadata.Name).ToList();
            ParserNames = StatementParsers.Select(x => x.Metadata.Name).ToList();
        }

        /// <summary>
        /// This method loads the plugins.
        /// </summary>
        private void AssembleComponents()
        {
            var configuration = new ContainerConfiguration();

            var pluginsDirPath = Path.Combine(Environment.CurrentDirectory + "\\Plugins");
            if (Directory.Exists(pluginsDirPath))
            {
                foreach (var file in Directory.EnumerateFiles(pluginsDirPath))
                {
                    try
                    {
                        var asm = Assembly.LoadFrom(file);
                        configuration.WithAssembly(asm);
                    }
                    catch
                    { }
                }
            }

            configuration.WithAssembly(Assembly.GetExecutingAssembly());

            try
            {
                using (var container = configuration.CreateContainer())
                {
                    container.SatisfyImports(this);
                }
            }
            catch (Exception ex)
            {
                //_dialogService.ShowMessageAsync(_mainVm, "Error", string.Format("There was an error loading plugins: {0}", ex)).Forget();
                _logger.Error(ex, "Error loading plugins");
            }
        }

        private async Task<IStatementDownloader> GetDownloaderByName(string name)
        {
            if (!DownloaderNames.Contains(name))
            {
                await _dialogService.ShowMessageAsync(_mainVm, "Error", "Statement downloader not found.");
                return null;
            }

            return StatementDownloaders.FirstOrDefault(x => x.Metadata.Name == name).CreateExport().Value;
        }

        private async Task<IStatementParser> GetParserByName(string name)
        {
            if (!ParserNames.Contains(name))
            {
                await _dialogService.ShowMessageAsync(_mainVm, "Error", "Statement parser not found.");
                return null;
            }

            return StatementParsers.FirstOrDefault(x => x.Metadata.Name == name).CreateExport().Value;
        }

        /// <summary>
        /// Downloads a statement from the net and then parses it.
        /// </summary>
        /// <param name="name">The name of the parser and downloader.</param>
        public async Task<Dictionary<string, DataContainer>> LoadFromWeb(string name, ProgressDialogController progress)
        {
            var downloader = await GetDownloaderByName(name).ConfigureAwait(false);
            if (downloader == null) throw new Exception("Downloader " + name + " not found.");
            var parser = await GetParserByName(name).ConfigureAwait(false);
            if (parser == null) throw new Exception("Parser " + name + " not found.");

            Exception ex = null;
            string flexqText = "";
            try
            {
                flexqText = await downloader.DownloadStatement(_settings).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                ex = e;
            }

            if (flexqText == "" || ex != null)
            {
                await progress.CloseAsync();
                await _dialogService.ShowMessageAsync(_mainVm, "Error downloading statement", ex?.Message);

                return null;
            }

            return await ParseStatement(parser, flexqText, progress);
        }

        /// <summary>
        /// Opens a file dialog to select a file, then parses the contents.
        /// </summary>
        /// <param name="name">The name of the parser.</param>
        public async Task<Dictionary<string, DataContainer>> LoadFromFile(string name, ProgressDialogController progress)
        {
            var parser = await GetParserByName(name);
            if (parser == null) throw new Exception("Parser " + name + " not found.");

            string file;
            bool? res = Dialogs.OpenFileDialog(parser.GetFileFilter(), out file);

            if (res != true)
            {
                return null;
            }

            string flexqText;
            try
            {
                flexqText = File.ReadAllText(file);
            }
            catch (IOException ex)
            {
                await progress.CloseAsync();
                await _dialogService.ShowMessageAsync(_mainVm, "Error", "Could not open file: " + ex.Message);
                return null;
            }

            return await ParseStatement(parser, flexqText, progress);
        }

        private async Task<Dictionary<string, DataContainer>> ParseStatement(IStatementParser parser, string flexqText, ProgressDialogController progress)
        {
            Dictionary<string, DataContainer> newData = null;
            try
            {
                await Task.Run(() => newData = parser.Parse(flexqText, progress, _settings, _currencies));
            }
            catch (Exception ex)
            {
                progress.CloseAsync().Forget();
                _dialogService.ShowMessageAsync(_mainVm, "Error", ex.Message).Forget();
                _logger.Error(ex, "Flex file parse error");
                return null;
            }

            progress.SetMessage("Updating open trades");

            return newData;
        }
    }
}