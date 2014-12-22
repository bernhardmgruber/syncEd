using Ninject.Modules;
using SyncEd.Document;
using SyncEd.Network;
using SyncEd.Network.Tcp;

namespace SyncEd.Editor
{
    public class SyncEdModule
        : NinjectModule
    {
        public override void Load()
        {
            //Bind<IDocument>().To<StringBuilderDocument>().InSingletonScope();
            Bind<IDocument>().To<NetworkDocument>().InSingletonScope();
            Bind<INetwork>().To<TreeNetwork>().InSingletonScope();
        }
    }
}