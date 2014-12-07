using System.Windows;

namespace SyncEd.Editor
{
    public partial class MainWindowView
        : Window
    {
        private new MainWindowViewModel DataContext
        {
            get { return (MainWindowViewModel)base.DataContext; }
            set { base.DataContext = value; }
        }

        public MainWindowView()
        {
            InitializeComponent();
        }
    }
}
