using Ninject.Modules;
using SyncEd.Document;
using SyncEd.Network;
using SyncEd.Network.Tcp.CompleteGraph;
using SyncEd.Network.Tcp.SpanningTree;

namespace SyncEd.Editor
{
    public class SyncEdModule
        : NinjectModule
    {
        public override void Load()
        {
            Bind<IDocument>().To<NetworkDocument>().InSingletonScope();
            Bind<INetwork>().To<SpanningTreeNetwork>().InSingletonScope();
            //Bind<INetwork>().To<CompleteGraphNetwork>().InSingletonScope();
        }
    }
}