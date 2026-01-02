using PriceLists.Maui.Views;

namespace PriceLists.Maui
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(ImportPreviewPage), typeof(ImportPreviewPage));
            Routing.RegisterRoute(nameof(ListDetailPage), typeof(ListDetailPage));
        }
    }
}
