namespace client.service.app
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            Launcher.OpenAsync("http://127.0.0.1:5411");
        }

        private void Button_Clicked_Inside(object sender, EventArgs e)
        {
            webview.Source = new Uri($"http://127.0.0.1:5411?p=1&t={DateTime.Now.Ticks}");
            //webview.Source = ;
            //webview.Reload();
        }
    }
}