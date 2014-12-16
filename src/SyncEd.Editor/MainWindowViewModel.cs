using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using SyncEd.Document;
using System.Windows;
using System.Diagnostics;
using System.Threading.Tasks;

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

        public FlowDocument Document
        {
            get { return flowDocument; }
            set { SetProperty(ref flowDocument, value); }
        }
        private FlowDocument flowDocument;
        public string DocumentName
        {
            get { return documentName; }
            set { SetProperty(ref documentName, value); }
        }
        private string documentName = "document name";

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
        private string documentText = String.Empty;

        public bool IsConnected
        {
            get { return isConnected; }
            set { SetProperty(ref isConnected, value); }
        }
        private bool isConnected = false;

        /*public IEnumerable<int> Carets
        {
            
        }*/


        public bool CanConnect
        {
            get { return canConnect; }
            set { SetProperty(ref canConnect, value); }
        }
        private bool canConnect = true;

        public ICommand ConnectCommand
        {
            get { return connectCommand ?? (connectCommand = new RelayCommand(_ => Connect())); }
        }
        private ICommand connectCommand;

        public async void Connect()
        {
            CanConnect = false;
            await Task.Run(() => document.Connect(DocumentName));
            IsConnected = true;

            document.TextChanged += (s, e) => Application.Current.Dispatcher.InvokeAsync(() => document_DocumentTextChanged(s, e));
        }

        bool processingChangeFromNetwork = false;

        public void ChangeText(ICollection<TextChange> changes, UndoAction undoAction)
        {
            if (!processingChangeFromNetwork)
            {
                foreach (var textChange in changes)
                {
                    string phrase = DocumentText.Substring(textChange.Offset, textChange.AddedLength);
                    document.ChangeText(textChange.Offset, textChange.RemovedLength, phrase);
                }
            }
        }
        public void ChangeCaretPos(int pos)
        {
            document.ChangeCaretPos(pos);
        }

        public async void Close()
        {
            await Task.Run(() => document.Close());
            document.TextChanged -= document_DocumentTextChanged;
            CanConnect = true;
            IsConnected = false;
        }

        private void document_DocumentTextChanged(object sender, DocumentTextChangedEventArgs e)
        {
            Console.WriteLine("change from network");
            processingChangeFromNetwork = true;
            DocumentText = e.Text;
            processingChangeFromNetwork = false;
        }
    }
}