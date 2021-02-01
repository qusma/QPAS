// -----------------------------------------------------------------------
// <copyright file="TradeWindow.xaml.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using MahApps.Metro.Controls;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;

namespace QPAS
{
    /// <summary>
    /// Interaction logic for TradeWindow.xaml
    /// </summary>
    public partial class TradeWindow : MetroWindow
    {
        public TradeViewModel ViewModel { get; set; }


        private readonly IContextFactory _contextFactory;
        private readonly TradesRepository _tradesRepo;
        private double[] _fontSizes;

        public double[] FontSizes
        {
            get
            {
                return _fontSizes;
            }
        }

        public TradeWindow(TradeViewModel viewModel, IContextFactory contextFactory, TradesRepository tradesRepo)
        {
            _contextFactory = contextFactory;
            _tradesRepo = tradesRepo;
            ViewModel = viewModel;
            InitializeComponent();


            DataContext = ViewModel;

            InitializeFontSizes();

            FontFamilyComboBox.ItemsSource = Fonts.SystemFontFamilies;
            FontSizeComboBox.ItemsSource = FontSizes;

            //load the notes
            LoadNotes();
        }

        private void LoadNotes()
        {
            if (string.IsNullOrEmpty(ViewModel.Trade.Notes)) return;

            TextRange tr = new TextRange(NotesTextBox.Document.ContentStart, NotesTextBox.Document.ContentEnd);
            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(ViewModel.Trade.Notes)))
            {
                tr.Load(ms, DataFormats.Rtf);
            }
        }

        private void InitializeFontSizes()
        {
            _fontSizes = new double[] {
                    3.0, 4.0, 5.0, 6.0, 6.5, 7.0, 7.5, 8.0, 8.5, 9.0, 9.5,
                    10.0, 10.5, 11.0, 11.5, 12.0, 12.5, 13.0, 13.5, 14.0, 15.0,
                    16.0, 17.0, 18.0, 19.0, 20.0, 22.0, 24.0, 26.0, 28.0, 30.0,
                    32.0, 34.0, 36.0, 38.0, 40.0, 44.0, 48.0, 52.0, 56.0, 60.0, 64.0, 68.0, 72.0, 76.0,
                    80.0, 88.0, 96.0, 104.0, 112.0, 120.0, 128.0, 136.0, 144.0
                    };
        }

        private void FontFamily_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FontFamily editValue = (FontFamily)e.AddedItems[0];
            ApplyPropertyValueToSelectedText(TextElement.FontFamilyProperty, editValue);
        }

        private void FontSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyPropertyValueToSelectedText(TextElement.FontSizeProperty, e.AddedItems[0]);
        }

        void ApplyPropertyValueToSelectedText(DependencyProperty formattingProperty, object value)
        {
            if (value == null)
                return;

            NotesTextBox.Selection.ApplyPropertyValue(formattingProperty, value);
        }

        private void RichTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            UpdateToggleButtonState();
            UpdateSelectionListType();
            UpdateSelectedFontFamily();
            UpdateSelectedFontSize();
        }

        void UpdateItemCheckedState(ToggleButton button, DependencyProperty formattingProperty, object expectedValue)
        {
            object currentValue = NotesTextBox.Selection.GetPropertyValue(formattingProperty);
            button.IsChecked = (currentValue == DependencyProperty.UnsetValue) ? false : currentValue != null && currentValue.Equals(expectedValue);
        }

        private void UpdateSelectionListType()
        {
            Paragraph startParagraph = NotesTextBox.Selection.Start.Paragraph;
            Paragraph endParagraph = NotesTextBox.Selection.End.Paragraph;
            if (startParagraph != null && endParagraph != null && (startParagraph.Parent is ListItem) && (endParagraph.Parent is ListItem) && object.ReferenceEquals(((ListItem)startParagraph.Parent).List, ((ListItem)endParagraph.Parent).List))
            {
                TextMarkerStyle markerStyle = ((ListItem)startParagraph.Parent).List.MarkerStyle;
                if (markerStyle == TextMarkerStyle.Disc) //bullets
                {
                    BulletsBtn.IsChecked = true;
                }
                else if (markerStyle == TextMarkerStyle.Decimal) //numbers
                {
                    NumberedBtn.IsChecked = true;
                }
            }
            else
            {
                BulletsBtn.IsChecked = false;
                NumberedBtn.IsChecked = false;
            }
        }

        private void UpdateToggleButtonState()
        {
            UpdateItemCheckedState(BoldBtn, TextElement.FontWeightProperty, FontWeights.Bold);
            UpdateItemCheckedState(ItalicBtn, TextElement.FontStyleProperty, FontStyles.Italic);
            UpdateItemCheckedState(UnderlineBtn, Inline.TextDecorationsProperty, TextDecorations.Underline);

            UpdateItemCheckedState(LeftBtn, Paragraph.TextAlignmentProperty, TextAlignment.Left);
            UpdateItemCheckedState(CenterBtn, Paragraph.TextAlignmentProperty, TextAlignment.Center);
            UpdateItemCheckedState(RightBtn, Paragraph.TextAlignmentProperty, TextAlignment.Right);
            UpdateItemCheckedState(JustifyBtn, Paragraph.TextAlignmentProperty, TextAlignment.Right);
        }

        private void UpdateSelectedFontFamily()
        {
            object value = NotesTextBox.Selection.GetPropertyValue(TextElement.FontFamilyProperty);
            FontFamily currentFontFamily = (FontFamily)((value == DependencyProperty.UnsetValue) ? null : value);
            if (currentFontFamily != null)
            {
                FontFamilyComboBox.SelectedItem = currentFontFamily;
            }
        }

        private void UpdateSelectedFontSize()
        {
            object value = NotesTextBox.Selection.GetPropertyValue(TextElement.FontSizeProperty);
            FontSizeComboBox.SelectedValue = (value == DependencyProperty.UnsetValue) ? null : value;
        }


        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //save notes as rtf
            TextRange tr = new TextRange(NotesTextBox.Document.ContentStart, NotesTextBox.Document.ContentEnd);
            using (MemoryStream ms = new MemoryStream())
            {
                tr.Save(ms, DataFormats.Rtf);
                ViewModel.Trade.Notes = Encoding.UTF8.GetString(ms.ToArray());
            }

            _tradesRepo.UpdateTrade(ViewModel.Trade).Wait();

            DataContext = null;
            ViewModel = null;
        }

        private async void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.SimulateTrade();
        }
    }
}
