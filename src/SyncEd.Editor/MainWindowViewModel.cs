using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using SyncEd.Document;
using System.Windows;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace SyncEd.Editor
{
    public class MainWindowViewModel
        : ViewModelBase
    {
        private readonly IDocument document;
        private ICollection<Caret> carets = new List<Caret>();

        public MainWindowViewModel(IDocument document)
        {
            this.document = document;
        }

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
        private int numberOfEditors = 1;

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

        public bool IsInForbiddenTextRange
        {
            get { return isInForbiddenTextRange; }
            set { SetProperty(ref isInForbiddenTextRange, value); }
        }
        private bool isInForbiddenTextRange;

        private int? caretIndex;

        public ObservableCollection<Tuple<int, int, Color>> HighlightedRanges
        {
            get { return highlightedRanges; }
            set { SetProperty(ref highlightedRanges, value); }
        }
        private ObservableCollection<Tuple<int, int, Color>> highlightedRanges = new ObservableCollection<Tuple<int, int, Color>>();

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
            document.CaretChanged += (s, e) => Application.Current.Dispatcher.InvokeAsync(() => document_CaretChanged(s, e));
            document.PeerCountChanged += (s, e) => NumberOfEditors = e.Count;
        }

        void document_CaretChanged(object sender, CaretChangedEventArgs e)
        {
            Console.WriteLine("UI: caret from " + e.Peer + " changed to " + e.Position);
            var caret = carets.Where(c => c.Peer.Equals(e.Peer)).FirstOrDefault();
            if (caret == null) {
                // new caret
                if (e.Position.HasValue) // new position
                    carets.Add(new Caret() { Peer = e.Peer, Position = e.Position.Value, Color = e.Peer.Color() });
            } else {
                // known caret
                if (e.Position.HasValue) // new position
                    caret.Position = e.Position.Value;
                else // no position, remove it
                    carets.Remove(caret);
            }

            // build highlighted ranges. TODO: this can be done more efficient by just replacing values
            const int caretLockDist = 3;
            HighlightedRanges = new ObservableCollection<Tuple<int, int, Color>>(
                carets.Select(c => Tuple.Create(c.Position - caretLockDist, c.Position + caretLockDist, c.Color))
            );
            CheckAllowEditing();
        }

        bool processingChangeFromNetwork = false;

        public void ChangeText(ICollection<TextChange> changes, UndoAction undoAction)
        {
            if (!processingChangeFromNetwork) {
                foreach (var textChange in changes) {
                    string phrase = DocumentText.Substring(textChange.Offset, textChange.AddedLength);
                    document.ChangeText(textChange.Offset, textChange.RemovedLength, phrase);
                }
            }
        }
        public void ChangeCaretPos(int? pos)
        {
            caretIndex = pos;
            if (!processingChangeFromNetwork)
                document.ChangeCaretPos(pos);
            CheckAllowEditing();
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
            processingChangeFromNetwork = true;
            DocumentText = e.Text;
            processingChangeFromNetwork = false;
        }

        private void CheckAllowEditing()
        {
            IsInForbiddenTextRange = caretIndex.HasValue &&
                caretIndex != 0 && caretIndex != DocumentText.Length // allways allow editing on the begin and end of the document
                && HighlightedRanges.Any(r => caretIndex >= r.Item1 && caretIndex <= r.Item2);
        }
    }
}