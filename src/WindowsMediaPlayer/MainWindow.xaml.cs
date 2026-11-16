using LibVLCSharp.Shared;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WindowsMediaPlayer
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<LibraryItem> _items = new ObservableCollection<LibraryItem>();
        private LibVLC _libVlc;
        private MediaPlayer _player;
        private Media _currentMedia;
        private readonly DispatcherTimer _timer;
        private bool _changingProgress;
        private bool _repeat;
        private int _imageIndex = -1;

        public MainWindow()
        {
            InitializeComponent();
            Core.Initialize();
            _libVlc = new LibVLC();
            _player = new MediaPlayer(_libVlc);
            VideoSurface.MediaPlayer = _player;
            MediaList.ItemsSource = _items;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _timer.Tick += Timer_Tick;
            _timer.Start();
            _player.EndReached += Player_EndReached;
            UpdateCount();
        }

        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Agregar audio y vídeo",
                Filter = "Medios compatibles|*.mp3;*.wav;*.flac;*.aac;*.m4a;*.ogg;*.opus;*.wma;*.aiff;*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.webm;*.mpeg;*.mpg;*.ts;*.m4v|Todos los archivos|*.*",
                Multiselect = true
            };
            if (dialog.ShowDialog(this) != true) return;
            foreach (var path in dialog.FileNames) AddMedia(path);
            if (MediaList.SelectedIndex < 0 && _items.Count > 0) MediaList.SelectedIndex = _items.Count - dialog.FileNames.Length;
        }

        private void AddMedia(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            var video = ".mp4 .mkv .avi .mov .wmv .webm .mpeg .mpg .ts .m4v .flv .ogv".Split(' ').Contains(ext);
            var kind = video ? "Vídeo" : "Audio";
            _items.Add(new LibraryItem { Title = Path.GetFileNameWithoutExtension(path), Path = path, Kind = kind, Description = kind + " · " + Path.GetExtension(path).TrimStart('.').ToUpperInvariant() });
            UpdateCount();
        }

        private void OpenImages_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Title = "Abrir imágenes", Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tif;*.tiff;*.webp|Todos los archivos|*.*", Multiselect = true };
            if (dialog.ShowDialog(this) != true) return;
            foreach (var path in dialog.FileNames)
                _items.Add(new LibraryItem { Title = Path.GetFileNameWithoutExtension(path), Path = path, Kind = "Imagen", Description = "Imagen · " + Path.GetExtension(path).TrimStart('.').ToUpperInvariant() });
            UpdateCount();
            ShowImage(dialog.FileNames[0]);
        }

        private void ShowImage(string path)
        {
            try
            {
                _player.Pause();
                var image = new BitmapImage();
                image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.UriSource = new Uri(path); image.EndInit(); image.Freeze();
                ImageSurface.Source = image; ImageSurface.Visibility = Visibility.Visible; VideoSurface.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed;
                CurrentMediaLabel.Text = Path.GetFileName(path) + "  ·  Imagen";
                NowTitle.Text = Path.GetFileNameWithoutExtension(path); NowSubtitle.Text = "Visor de imágenes";
                _imageIndex = _items.Where(x => x.Kind == "Imagen").ToList().FindIndex(x => x.Path == path);
            }
            catch (Exception ex) { MessageBox.Show(this, "No se pudo abrir la imagen: " + ex.Message, "Visor de imágenes", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void PlayItem(LibraryItem item)
        {
            if (item == null) return;
            if (item.Kind == "Imagen") { ShowImage(item.Path); return; }
            try
            {
                ImageSurface.Visibility = Visibility.Collapsed; VideoSurface.Visibility = Visibility.Visible; EmptyState.Visibility = Visibility.Collapsed;
                _currentMedia?.Dispose();
                _currentMedia = new Media(_libVlc, item.Path, FromType.FromPath);
                _player.Play(_currentMedia);
                CurrentMediaLabel.Text = item.Title; NowTitle.Text = item.Title; NowSubtitle.Text = item.Description;
                PlayButton.Content = "Ⅱ";
            }
            catch (Exception ex) { MessageBox.Show(this, "No se pudo reproducir el archivo: " + ex.Message, "Reproducción", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (_player.IsPlaying) { _player.Pause(); PlayButton.Content = "▶"; }
            else if (_player.Media != null) { _player.Play(); PlayButton.Content = "Ⅱ"; }
            else if (MediaList.SelectedItem is LibraryItem selected) PlayItem(selected);
            else AddFiles_Click(sender, e);
        }

        private void MediaList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MediaList.SelectedItem is LibraryItem item) PlayItem(item);
        }

        private void Navigate_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button button)) return;
            var page = Convert.ToString(button.Tag);
            PageTitle.Text = page;
            string kind = page == "Música" ? "Audio" : page == "Vídeos" ? "Vídeo" : page == "Radio" ? "Radio" : page == "Audiolibros" ? "Audiolibro" : page == "Podcasts" ? "Podcast" : null;
            MediaList.ItemsSource = string.IsNullOrEmpty(kind) ? _items : new ObservableCollection<LibraryItem>(_items.Where(x => x.Kind == kind));
            if (page == "Radio") CurrentMediaLabel.Text = "Pulsa «Radio URL» para abrir una emisora o stream.";
        }

        private void GroupBy_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (PageTitle != null && GroupByBox.SelectedItem is ComboBoxItem item) PageTitle.Text = "Música · " + item.Content;
        }

        private void Search_Changed(object sender, TextChangedEventArgs e)
        {
            if (MediaList == null || SearchBox.Text == "Buscar en tu biblioteca…") return;
            var query = SearchBox.Text.Trim();
            MediaList.ItemsSource = new ObservableCollection<LibraryItem>(_items.Where(x => string.IsNullOrEmpty(query) || x.Title.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || x.Description.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private void OpenRadio_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new RadioUrlDialog { Owner = this };
            if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.StreamUrl)) return;
            try
            {
                var media = new LibraryItem { Title = dialog.StreamUrl, Path = dialog.StreamUrl, Kind = "Radio", Description = "Radio · Stream de red" };
                _items.Add(media); UpdateCount(); MediaList.ItemsSource = _items; MediaList.SelectedItem = media;
                _currentMedia?.Dispose(); _currentMedia = new Media(_libVlc, media.Path, FromType.FromLocation); _player.Play(_currentMedia);
                CurrentMediaLabel.Text = "Radio en directo"; NowTitle.Text = "Radio"; NowSubtitle.Text = media.Path; VideoSurface.Visibility = Visibility.Visible; EmptyState.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex) { MessageBox.Show(this, "No se pudo abrir el stream: " + ex.Message, "Radio", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            if (ImageSurface.Visibility == Visibility.Visible) { ChangeImage(-1); return; }
            if (MediaList.Items.Count == 0) return;
            MediaList.SelectedIndex = (MediaList.SelectedIndex - 1 + MediaList.Items.Count) % MediaList.Items.Count;
        }
        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (ImageSurface.Visibility == Visibility.Visible) { ChangeImage(1); return; }
            if (MediaList.Items.Count == 0) return;
            MediaList.SelectedIndex = (MediaList.SelectedIndex + 1) % MediaList.Items.Count;
        }
        private void ChangeImage(int step)
        {
            var images = _items.Where(x => x.Kind == "Imagen").ToList();
            if (images.Count == 0) return;
            _imageIndex = (_imageIndex + step + images.Count) % images.Count;
            ShowImage(images[_imageIndex].Path);
        }
        private void Shuffle_Click(object sender, RoutedEventArgs e)
        {
            if (MediaList.Items.Count > 0) MediaList.SelectedIndex = new Random().Next(MediaList.Items.Count);
        }
        private void Repeat_Click(object sender, RoutedEventArgs e)
        {
            _repeat = !_repeat; ((Button)sender).Foreground = _repeat ? System.Windows.Media.Brushes.DeepSkyBlue : System.Windows.Media.Brushes.White;
        }
        private void Player_EndReached(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { if (_repeat) _player.Play(); else Next_Click(this, new RoutedEventArgs()); }));
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_player == null || _player.Length <= 0) return;
            _changingProgress = true;
            ProgressSlider.Maximum = _player.Length;
            ProgressSlider.Value = Math.Min(_player.Time, _player.Length);
            TimeLabel.Text = FormatTime(_player.Time) + " / " + FormatTime(_player.Length);
            _changingProgress = false;
        }
        private void Progress_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_changingProgress && _player != null && _player.Length > 0) _player.Time = (long)ProgressSlider.Value;
        }
        private static string FormatTime(long milliseconds)
        {
            var time = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
            return time.TotalHours >= 1 ? time.ToString(@"h\:mm\:ss") : time.ToString(@"m\:ss");
        }
        private void UpdateCount() { if (LibraryCount != null) LibraryCount.Text = "  ·  " + _items.Count + " elementos"; }
        protected override void OnClosing(CancelEventArgs e)
        {
            _timer?.Stop();
            if (_player != null) { _player.EndReached -= Player_EndReached; _player.Stop(); _player.Dispose(); }
            _currentMedia?.Dispose(); _libVlc?.Dispose();
            base.OnClosing(e);
        }
    }

    public class LibraryItem
    {
        public string Title { get; set; }
        public string Path { get; set; }
        public string Kind { get; set; }
        public string Description { get; set; }
    }
}
