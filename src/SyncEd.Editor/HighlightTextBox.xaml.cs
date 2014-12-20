using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SyncEd.Editor
{
    /// <summary>
    /// Interaction logic for HighlightTextBox.xaml
    /// </summary>
    public partial class HighlightTextBox
        : TextBox
    {
        public static DependencyProperty HighlightRangesProperty = DependencyProperty.Register("HighlightRanges", typeof(IEnumerable<Tuple<int, int, Color>>), typeof(HighlightTextBox),
            new FrameworkPropertyMetadata(Enumerable.Empty<Tuple<int, int, Color>>(), FrameworkPropertyMetadataOptions.AffectsRender));

        [Bindable(true)]
        public IEnumerable<Tuple<int, int, Color>> HighlightRanges
        {
            get { return (IEnumerable<Tuple<int, int, Color>>)GetValue(HighlightRangesProperty); }
            set { SetValue(HighlightRangesProperty, value); }
        }

        public static DependencyProperty HighlightColorProperty = DependencyProperty.Register("HighlightColor", typeof(Brush), typeof(HighlightTextBox),
            new FrameworkPropertyMetadata(Brushes.Red, FrameworkPropertyMetadataOptions.AffectsRender));


        public HighlightTextBox()
        {
            InitializeComponent();

            DefaultStyleKey = typeof(HighlightTextBox);

            TextChanged += (s, a) => InvalidateVisual();
            IsEnabledChanged += (s, a) => InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (string.IsNullOrEmpty(Text)) {
                base.OnRender(drawingContext);
                return;
            }

            EnsureScrolling();

            Background = Foreground = Brushes.Transparent;

            // clear background and prepare clipping region
            drawingContext.DrawRectangle(new SolidColorBrush(Colors.White), new Pen(), new Rect(0, 0, ActualWidth, ActualHeight));
            drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, this.ActualWidth, this.ActualHeight)));

            // prepare text
            var formattedText = new FormattedText(Text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface(FontFamily.Source), FontSize, new SolidColorBrush(Colors.Black)) {
                Trimming = TextTrimming.None,
                MaxTextWidth = ViewportWidth,
                MaxTextHeight = Math.Max(ActualHeight + VerticalOffset, 0)
            };

            foreach (var range in HighlightRanges) {
                var l = formattedText.Text.Length;

                // ensure consistent ranges
                var start = Math.Max(0, Math.Min(range.Item1, l));
                var end = Math.Max(0, Math.Min(range.Item2, l));

                formattedText.SetForegroundBrush(new SolidColorBrush(range.Item3), start, end - start);
            }

            // draw text
            // from: http://www.codeproject.com/Articles/33939/CodeBox
            double leftMargin = 4.0 + BorderThickness.Left;
            double topMargin = 2 + BorderThickness.Top;

            drawingContext.DrawText(formattedText, new Point(leftMargin, topMargin - VerticalOffset));
        }

        /*protected override void OnTextChanged(TextChangedEventArgs e)
        {
            if (setOldText)
                return;

            var forbiddenChanges =
                from change in e.Changes
                let start = change.Offset
                let end = start + Math.Max(change.AddedLength, change.AddedLength)
                where start != 0 && start != oldText.Length // allow input at they start and end of text
                where HighlightRanges.Any(hr => start <= hr.Item2 && end >= hr.Item1)
                select change;

            //var filteredArgs = new TextChangedEventArgs(e.RoutedEvent, e.UndoAction, e.Changes.Except(forbiddenChanges).ToList());

            if (forbiddenChanges.Any()) {
                setOldText = true;
                Text = oldText;
                setOldText = false;
            } else {
                oldText = Text;
                base.OnTextChanged(e);
            }
        }*/

        // from: http://www.codeproject.com/Articles/33939/CodeBox
        private bool scrollingEventEnabled = false;
        private void EnsureScrolling()
        {
            if (!scrollingEventEnabled) {
                DependencyObject dp = VisualTreeHelper.GetChild(this, 0);
                ScrollViewer sv = VisualTreeHelper.GetChild(dp, 0) as ScrollViewer;
                sv.ScrollChanged += (s, e) => InvalidateVisual();
                scrollingEventEnabled = true;
            }
        }
    }
}
