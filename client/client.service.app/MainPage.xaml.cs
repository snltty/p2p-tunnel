using common.libs;
using common.libs.extends;
using System.Net.Sockets;
using System.Net;

namespace client.service.app
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            WriteLogger();
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            Launcher.OpenAsync("https://snltty.gitee.io/p2p-tunnel");

        }

        private void WriteLogger()
        {
            Task.Factory.StartNew(() =>
            {
                Dictionary<LoggerTypes, Color> colors = new()
                {
                    {LoggerTypes.WARNING,Color.FromRgb(241 ,157, 52) },
                    {LoggerTypes.INFO,Color.FromRgb(0,0,0) },
                    {LoggerTypes.DEBUG,Color.FromRgb(0,0,255) },
                    {LoggerTypes.ERROR,Color.FromRgb(255,0,0) },
                };
                Color defaultColor = Color.FromRgb(0, 0, 0);
                var endpoint = new IPEndPoint(IPAddress.Any, 59411);
                var udp = new UdpClient(endpoint);
                IPEndPoint ep = null;
                while (true)
                {
                    try
                    {
                        var bytes = udp.Receive(ref ep);
                        if (bytes.Length > 0)
                        {
                            LoggerModel logger = bytes.AsSpan().GetString().DeJson<LoggerModel>();
                            Color color = defaultColor;
                            if (colors.ContainsKey(logger.Type))
                            {
                                color = colors[logger.Type];
                            }

                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                Label label = new()
                                {
                                    FormattedText = new FormattedString { },
                                    TextColor = color
                                };
                                label.FormattedText.Spans.Add(new Span { Text = $"[{logger.Type}]", FontAttributes = FontAttributes.Bold });
                                label.FormattedText.Spans.Add(new Span { Text = $"{logger.Time:yyyy-MM-dd HH:mm:ss}:", FontAttributes = FontAttributes.Italic });
                                label.FormattedText.Spans.Add(new Span { Text = logger.Content });
                                loggerBox.Children.Add(label);
                                if (loggerBox.Children.Count > 100)
                                {
                                    loggerBox.Children.RemoveAt(0);
                                }
                            });
                        }
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
            }, TaskCreationOptions.LongRunning);

        }
    }
}