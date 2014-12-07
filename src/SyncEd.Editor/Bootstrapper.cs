using System;
using System.Collections.Generic;
using System.Windows;
using Caliburn.Micro;
using Ninject;

namespace SyncEd.Editor
{
    public class Bootstrapper
        : BootstrapperBase
    {
        private readonly IKernel kernel;

        public Bootstrapper()
        {
            kernel = new StandardKernel();

            kernel.Bind<IWindowManager>().To<WindowManager>().InSingletonScope();
            kernel.Bind<IEventAggregator>().To<EventAggregator>().InSingletonScope();

            Initialize();
        }

        protected override void Configure()
        {
            kernel.Load(AppDomain.CurrentDomain.GetAssemblies());

            base.Configure();
        }

        protected override object GetInstance(Type service, string key)
        {
            return kernel.Get(service, key);
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return kernel.GetAll(service);
        }

        protected override void BuildUp(object instance)
        {
            kernel.Inject(instance);
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            DisplayRootViewFor<MainWindowViewModel>();
        }

        protected override void OnExit(object sender, EventArgs e)
        {
            kernel.Dispose();
        }
    }
}