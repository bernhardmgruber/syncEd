using System;
using System.Threading.Tasks;

namespace SyncEd.Document
{
    public interface IDocument
    {
        bool IsConnected { get; }
        Task<bool> Connect(string documentName);
        Task Close();

        void ChangeText(int offset, int length, string text, bool guiSource);
        event EventHandler<DocumentTextChangedEventArgs> TextChanged;
    }
}
