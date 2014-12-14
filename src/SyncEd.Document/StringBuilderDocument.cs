using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace SyncEd.Document
{
    public class StringBuilderDocument
        : IDocument
    {
        private readonly StringBuilder documentText;

        public StringBuilderDocument()
        {
            documentText = new StringBuilder();
        }

        public bool IsConnected { get; private set; }
        public Task<bool> Connect(string documentName)
        {
            if (IsConnected)
                throw new NotSupportedException();

            bool exists = documentName == "Test";
            IsConnected = exists;
            return Task.Delay(500).ContinueWith(task => exists);
        }

        public Task Close()
        {
            bool wasConnected = IsConnected;

            IsConnected = false;

            return Task.Delay(wasConnected ? 250 : 0);
        }

        public void ChangeText(int offset, int length, string text, bool _)
        {
            Trace.WriteLine(string.Format("ChangeText [{0} {1}]: {2}", offset, length, text));
            documentText.Remove(offset, length);
            documentText.Insert(offset, text);
            FireTextChanged();

        }

        public event EventHandler<DocumentTextChangedEventArgs> TextChanged;

        protected void FireTextChanged()
        {
            string text = documentText.ToString();
            if (TextChanged != null)
                TextChanged(this, new DocumentTextChangedEventArgs(text, false));
        }
    }
}