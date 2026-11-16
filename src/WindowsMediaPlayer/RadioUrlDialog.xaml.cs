using System;
using System.Windows;

namespace WindowsMediaPlayer
{
    public partial class RadioUrlDialog : Window
    {
        public string StreamUrl { get; private set; }
        public RadioUrlDialog() { InitializeComponent(); }
        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (!Uri.TryCreate(UrlBox.Text.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != "rtsp"))
            {
                MessageBox.Show(this, "Introduce una URL http, https o rtsp válida.", "URL no válida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            StreamUrl = uri.ToString(); DialogResult = true;
        }
    }
}
