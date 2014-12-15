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

        private Point renderPoint;

        public HighlightTextBox()
        {
            InitializeComponent();

            DefaultStyleKey = typeof(HighlightTextBox);

            TextChanged += (s, a) => InvalidateVisual();
            IsEnabledChanged += (s, a) => InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (!IsEnabled) {
                Foreground = (Brush)ForegroundProperty.DefaultMetadata.DefaultValue;
                //Background = (Brush)BackgroundProperty.DefaultMetadata.DefaultValue;
                Background = Brushes.LightGray;
                base.OnRender(drawingContext);
            } else {
                Foreground = Background = Brushes.Transparent;
                drawingContext.DrawRectangle(Brushes.White, new Pen(), new Rect(0, 0, ActualWidth, ActualHeight));

                if (string.IsNullOrEmpty(Text))
                    return;

                var formattedText = new FormattedText(Text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                                                      new Typeface(FontFamily.Source), FontSize,
                                                      new SolidColorBrush(Colors.Black)) {
                                                          Trimming = TextTrimming.None,
                                                          MaxTextWidth = ViewportWidth
                                                      };

                foreach (var range in HighlightRanges) {
                    if (range.Item1 < range.Item2 && Text.Length < range.Item2)
                        formattedText.SetForegroundBrush(HighlightColor, range.Item1, range.Item2 - range.Item1);
                }

                var firstCharRect = GetRectFromCharacterIndex(0);
                if (!double.IsInfinity(firstCharRect.Top))
                    renderPoint = new Point(firstCharRect.Left, firstCharRect.Top);

                drawingContext.DrawText(formattedText, renderPoint);
            }
        }
    }
}
