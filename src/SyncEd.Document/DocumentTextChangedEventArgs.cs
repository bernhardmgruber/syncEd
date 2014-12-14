using System;

namespace SyncEd.Document
{
    public class DocumentTextChangedEventArgs
        : EventArgs
    {
        public string Text { get; private set; }

        public bool GuiSource { get; set; }

        public DocumentTextChangedEventArgs(string text, bool guiSource)
        {
            Text = text;
            GuiSource = guiSource;
        }
    }
}