using System;
using System.Threading.Tasks;

namespace SyncEd.Document
{
    public interface IDocument
    {
        event EventHandler<DocumentTextChangedEventArgs> TextChanged;

        bool IsConnected { get; }
        void Connect(string documentName);
        void Close();

        void ChangeText(int offset, int length, string text);
        void ChangeCaretPos(int pos);
    }
}
