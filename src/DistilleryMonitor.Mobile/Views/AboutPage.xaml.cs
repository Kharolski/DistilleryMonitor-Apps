using DistilleryMonitor.Mobile.ViewModels;

namespace DistilleryMonitor.Mobile.Views
{
    public partial class AboutPage : ContentPage
    {
        public AboutPage(AboutPageViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
