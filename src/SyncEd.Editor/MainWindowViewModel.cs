using System.Windows.Input;
using SyncEd.Document;

namespace SyncEd.Editor
{
    public class MainWindowViewModel
    : ViewModelBase
    {
        private readonly IDocument document;

        public MainWindowViewModel(IDocument document)
        {
            this.document = document;
        }


        public string DocumentName
        {
            get { return documentName; }
            set { SetProperty(ref documentName, value); }
        }
        private string documentName = "(Document Name)";

        public int NumberOfEditors
        {
            get { return numberOfEditors; }
            set { SetProperty(ref numberOfEditors, value); }
        }
        private int numberOfEditors = 0;

        public string DocumentText
        {
            get { return documentText; }
            set { SetProperty(ref documentText, value); }
        }
        private string documentText = "Hello, World!\nThis is a text.";

        public ICommand Connect
        {
            get { return null; }
        }

        public string ConnectionCommandText
        {
            get { return "Connect"; }
        }

    }
}