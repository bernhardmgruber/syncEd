using Ninject.Modules;
using SyncEd.Document;

namespace SyncEd.Editor
{
    public class SyncEdModule
        : NinjectModule
    {
        public override void Load()
        {
            Bind<IDocument>().To<SimpleDocument>().InSingletonScope();
        }
    }
}