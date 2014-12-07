using System.Threading.Tasks;

namespace SyncEd.Document
{
    public interface IDocument
    {
        bool IsConnected { get; }
        Task<bool> Connect(string documentName);
        Task Close();
    }
}
