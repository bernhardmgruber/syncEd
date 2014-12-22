using System;
using System.Windows;
using System.Windows.Controls;

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

        private void TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DataContext != null)
                DataContext.ChangeText(e.Changes, e.UndoAction);
        }

        private void OnClosed(object sender, EventArgs e)
        {
            DataContext.Close();
        }
    }
}
