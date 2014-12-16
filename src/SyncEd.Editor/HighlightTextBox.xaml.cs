using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SyncEd.Editor
{
    /// <summary>
    /// Interaction logic for HighlightTextBox.xaml
    /// </summary>
    public partial class HighlightTextBox
        : TextBox
    {
        public static DependencyProperty HighlightRangesProperty = DependencyProperty.Register("HighlightRanges", typeof(IEnumerable<Tuple<int, int>>), typeof(HighlightTextBox),
            new FrameworkPropertyMetadata(Enumerable.Empty<Tuple<int, int>>(), FrameworkPropertyMetadataOptions.AffectsRender));

        [Bindable(true)]
        public IEnumerable<Tuple<int, int>> HighlightRanges
        {
            get { return (IEnumerable<Tuple<int, int>>)GetValue(HighlightRangesProperty); }
            set { SetValue(HighlightRangesProperty, value); }
        }

        public static DependencyProperty HighlightColorProperty = DependencyProperty.Register("HighlightColor", typeof(Brush), typeof(HighlightTextBox),
            new FrameworkPropertyMetadata(Brushes.Red, FrameworkPropertyMetadataOptions.AffectsRender));

        [Bindable(true)]
        public Brush HighlightColor
        {
            get { return (Brush)GetValue(HighlightColorProperty); }
            set { SetValue(HighlightColorProperty, value); }
        }

        public HighlightTextBox()
        {
            InitializeComponent();

            DefaultStyleKey = typeof(HighlightTextBox);

            TextChanged += (s, a) => InvalidateVisual();
            IsEnabledChanged += (s, a) => InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (string.IsNullOrEmpty(Text))
            {
                base.OnRender(drawingContext);
                return;
            }

            EnsureScrolling();

            // save colors
            var bg = Background;
            var fg = Foreground;
            Background = Foreground = Brushes.Transparent;

            // clear background and prepare clipping region
            drawingContext.DrawRectangle(bg, new Pen(), new Rect(0, 0, ActualWidth, ActualHeight));
            drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, this.ActualWidth, this.ActualHeight)));

            // prepare text
            var formattedText = new FormattedText(Text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                                                    new Typeface(FontFamily.Source), FontSize, fg) {
                                                        Trimming = TextTrimming.None,
                                                        MaxTextWidth = ViewportWidth,
                                                        MaxTextHeight = Math.Max(ActualHeight + VerticalOffset, 0)
                                                    };

            foreach (var range in HighlightRanges) {
                if (range.Item1 < range.Item2 && Text.Length < range.Item2)
                    formattedText.SetForegroundBrush(HighlightColor, range.Item1, range.Item2 - range.Item1);
            }

            // draw text
            // from: http://www.codeproject.com/Articles/33939/CodeBox
            double leftMargin = 4.0 + BorderThickness.Left;
            double topMargin = 2 + BorderThickness.Top;

            drawingContext.DrawText(formattedText, new Point(leftMargin, topMargin - VerticalOffset));

            // restore colors
            Background = bg;
            Foreground = fg;
        }

        // from: http://www.codeproject.com/Articles/33939/CodeBox
        private bool scrollingEventEnabled = false;
        private void EnsureScrolling()
        {
            if (!scrollingEventEnabled)
            {
                DependencyObject dp = VisualTreeHelper.GetChild(this, 0);
                ScrollViewer sv = VisualTreeHelper.GetChild(dp, 0) as ScrollViewer;
                sv.ScrollChanged += (s, e) => InvalidateVisual();
                scrollingEventEnabled = true;
            }
        }
    }
}
