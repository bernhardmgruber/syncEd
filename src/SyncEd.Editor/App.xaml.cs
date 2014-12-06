using System.Windows;
using Ninject;

namespace SyncEd.Editor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var kernel = new StandardKernel();

            //kernel.Bind<MainWindowViewModel>().ToSelf();
            //kernel.Bind<MainWindow>().ToSelf();

            var window = kernel.Get<MainWindow>();
            window.Show();
        }
    }
}
