using System.ComponentModel;
using Xamarin.Forms;

namespace NordicAndroidBle
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            BindingContext = new MainPageViewModel();
        }

        private void SelectedLocation_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (BindingContext is MainPageViewModel locationSelect) locationSelect.DoConnect();
        }
    }
}
