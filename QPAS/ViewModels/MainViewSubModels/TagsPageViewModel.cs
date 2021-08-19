// -----------------------------------------------------------------------
// <copyright file="TagsPageViewModel.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace QPAS
{
    public class TagsPageViewModel : ViewModelBase
    {
        public CollectionViewSource TagsSource { get; set; }

        public MainViewModel Parent { get; set; }

        public ICommand Delete { get; set; }

        private readonly IContextFactory contextFactory;
        private readonly DataContainer data;

        public TagsPageViewModel(IContextFactory contextFactory, IDialogCoordinator dialogService, DataContainer data, MainViewModel parent)
            : base(dialogService)
        {
            Parent = parent;

            TagsSource = new CollectionViewSource();
            TagsSource.Source = data.Tags;
            TagsSource.View.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            CreateCommands();
            this.contextFactory = contextFactory;
            this.data = data;
        }

        private void CreateCommands()
        {
            Delete = ReactiveCommand.CreateFromTask<IList>(async x => await DeleteTags(x));
        }

        private async Task DeleteTags(IList tags)
        {
            if (tags == null || tags.Count == 0) return;
            foreach (Tag t in tags.Cast<Tag>().Distinct().ToList()) //something starnge happens where the selection includes the same item multiple times
            {
                await DeleteTag(t).ConfigureAwait(false);
            }
        }

        private async Task DeleteTag(Tag tag)
        {
            MessageDialogResult res = await DialogService.ShowMessageAsync(Parent,
                "Delete Tag",
                string.Format("Are you sure you want to delete {0}?", tag.Name),
                MessageDialogStyle.AffirmativeAndNegative);
            if (res != MessageDialogResult.Affirmative) return;

            //keep track of the trades with this tag, we need to update their tagstrings
            var trades = data.Trades.ToList().Where(x => x.Tags.Contains(tag)).ToList();

            foreach (var trade in trades)
            {
                trade.Tags.Remove(tag);
                await Parent.TradesPageViewModel.TradesRepository.UpdateTrade(trade).ConfigureAwait(false);
                trade.TagStringUpdated();
            }

            using (var dbContext = contextFactory.Get())
            {
                dbContext.Tags.Remove(tag);
                dbContext.SaveChanges();

                data.Tags.Remove(tag);
            }
        }
    }
}