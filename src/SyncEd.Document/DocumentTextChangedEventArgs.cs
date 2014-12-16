using System;

namespace SyncEd.Document
{
    public class DocumentTextChangedEventArgs
        : EventArgs
    {
        public string Text { get; private set; }

        public DocumentTextChangedEventArgs(string text)
        {
            Text = text;
        }
    }
}