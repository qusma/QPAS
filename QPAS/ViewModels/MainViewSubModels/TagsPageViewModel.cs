// -----------------------------------------------------------------------
// <copyright file="TagsPageViewModel.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityModel;
using MahApps.Metro.Controls.Dialogs;
using System.Collections;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace QPAS
{
    public class TagsPageViewModel : ViewModelBase
    {
        public CollectionViewSource TagsSource { get; set; }

        public MainViewModel Parent { get; set; }

        internal IDBContext Context;

        public ICommand Delete { get; set; }

        public TagsPageViewModel(IDBContext context, IDialogService dialogService, MainViewModel parent)
            : base(dialogService)
        {
            Context = context;
            Parent = parent;

            TagsSource = new CollectionViewSource();
            TagsSource.Source = Context.Tags.Local;
            TagsSource.View.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            CreateCommands();
        }

        private void CreateCommands()
        {
            Delete = new RelayCommand<IList>(DeleteTags);
        }

        public override void Refresh()
        {
            Context.Tags.Load();

            TagsSource.View.Refresh();
        }

        private void DeleteTags(IList tags)
        {
            if (tags == null || tags.Count == 0) return;
            foreach (Tag t in tags)
            {
                DeleteTag(t);
            }
        }

        private async void DeleteTag(Tag tag)
        {
            MessageDialogResult res = await DialogService.ShowMessageAsync(
                "Delete Tag",
                string.Format("Are you sure you want to delete {0}?", tag.Name),
                MessageDialogStyle.AffirmativeAndNegative);
            if (res != MessageDialogResult.Affirmative) return;

            //keep track of the trades with this tag, we need to update their tagstrings
            var trades = Context.Trades.ToList().Where(x => x.Tags.Contains(tag)).ToList();

            Context.Tags.Remove(tag);
            Context.SaveChanges();

            foreach (Trade t in trades)
            {
                t.TagStringUpdated();
            }
        }
    }
}