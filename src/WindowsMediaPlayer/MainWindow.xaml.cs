using LibVLCSharp.Shared;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TagLib;

namespace WindowsMediaPlayer
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<LibraryItem> _items = new ObservableCollection<LibraryItem>();
        private ICollectionView _libraryView;
        private LibVLC _libVlc;
        private MediaPlayer _player;
        private Media _currentMedia;
        private readonly DispatcherTimer _timer;
        private bool _changingProgress;
        private bool _repeat;
        private bool _shuffle;
        private int _imageIndex = -1;
        private string _currentPage = "Inicio";
        private string _searchText = "";

        public MainWindow()
        {
            InitializeComponent();
            Core.Initialize();
            _libVlc = new LibVLC();
            _player = new MediaPlayer(_libVlc);
            VideoSurface.MediaPlayer = _player;
            _libraryView = CollectionViewSource.GetDefaultView(_items);
            _libraryView.Filter = FilterLibraryItem;
            MediaList.ItemsSource = _libraryView;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            _timer.Tick += Timer_Tick;
            _timer.Start();
            _player.EndReached += Player_EndReached;
            UpdateCount();
        }

        private bool FilterLibraryItem(object value)
        {
            var item = value as LibraryItem;
            if (item == null) return false;
            var page = _currentPage;
            if (page == "Música" && item.Kind != "Audio") return false;
            if (page == "Vídeos" && item.Kind != "Vídeo") return false;
            if (page == "Podcasts" && item.Kind != "Podcast") return false;
            if (page == "Audiolibros" && item.Kind != "Audiolibro") return false;
            if (page == "Radio" && item.Kind != "Radio") return false;
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                var searchable = string.Join(" ", item.Title, item.Artist, item.Album, item.Genre, item.Description, item.Year.ToString());
                if (searchable.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) < 0) return false;
            }
            return true;
        }

        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Agregar archivos multimedia",
                Filter = "Archivos multimedia|*.mp3;*.wav;*.flac;*.aac;*.m4a;*.ogg;*.opus;*.wma;*.aiff;*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.webm;*.mpeg;*.mpg;*.ts;*.m4v;*.flv;*.ogv|Todos los archivos|*.*",
                Multiselect = true
            };
            if (dialog.ShowDialog(this) != true) return;
            var firstNewIndex = _items.Count;
            foreach (var path in dialog.FileNames) AddMedia(path);
            RefreshLibrary();
            if (firstNewIndex < _items.Count) SelectAndPlay(_items[firstNewIndex]);
        }

        private void AddMedia(string path)
        {
            var extension = System.IO.Path.GetExtension(path).ToLowerInvariant();
            var isVideo = ".mp4 .mkv .avi .mov .wmv .webm .mpeg .mpg .ts .m4v .flv .ogv".Split(' ').Contains(extension);
            var item = new LibraryItem
            {
                Title = System.IO.Path.GetFileNameWithoutExtension(path), Path = path,
                Kind = isVideo ? "Vídeo" : "Audio", Description = isVideo ? "Vídeo" : "Audio",
                Artist = "Artista desconocido", Album = "Álbum desconocido", Genre = "Sin género", Year = 0
            };
            if (!isVideo) ReadAudioTags(item);
            item.Description = isVideo ? "Vídeo · " + extension.TrimStart('.').ToUpperInvariant() : item.Artist + " · " + item.Album;
            if (!_items.Any(existing => string.Equals(existing.Path, path, StringComparison.OrdinalIgnoreCase))) _items.Add(item);
        }

        private static void ReadAudioTags(LibraryItem item)
        {
            try
            {
                using (var file = TagLib.File.Create(item.Path))
                {
                    var tag = file.Tag;
                    if (!string.IsNullOrWhiteSpace(tag.Title)) item.Title = tag.Title;
                    if (tag.Performers != null && tag.Performers.Length > 0) item.Artist = tag.Performers[0];
                    if (!string.IsNullOrWhiteSpace(tag.Album)) item.Album = tag.Album;
                    if (!string.IsNullOrWhiteSpace(tag.FirstGenre)) item.Genre = tag.FirstGenre;
                    item.Year = (int)tag.Year;
                    item.Bpm = (int)tag.BeatsPerMinute;
                }
            }
            catch (Exception) { /* Some formats have no readable tags; keep the file in the library. */ }
        }

        private void OpenImages_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Title = "Abrir imágenes", Filter = "Imágenes compatibles|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tif;*.tiff|Todos los archivos|*.*", Multiselect = true };
            if (dialog.ShowDialog(this) != true) return;
            foreach (var path in dialog.FileNames)
            {
                if (!_items.Any(item => string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase)))
                    _items.Add(new LibraryItem { Title = System.IO.Path.GetFileNameWithoutExtension(path), Path = path, Kind = "Imagen", Description = "Imagen · " + System.IO.Path.GetExtension(path).TrimStart('.').ToUpperInvariant() });
            }
            RefreshLibrary();
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
                CurrentMediaLabel.Text = System.IO.Path.GetFileName(path) + " · Imagen";
                NowTitle.Text = System.IO.Path.GetFileNameWithoutExtension(path); NowSubtitle.Text = "Visor de imágenes · anterior/siguiente";
                _imageIndex = _items.Where(item => item.Kind == "Imagen").ToList().FindIndex(item => item.Path == path);
                PlayButton.Content = "▶";
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
                var from = Uri.TryCreate(item.Path, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == "rtsp") ? FromType.FromLocation : FromType.FromPath;
                _currentMedia = new Media(_libVlc, item.Path, from);
                _player.Play(_currentMedia);
                CurrentMediaLabel.Text = item.Title; NowTitle.Text = item.Title; NowSubtitle.Text = item.Description;
                PlayButton.Content = "Ⅱ";
            }
            catch (Exception ex) { MessageBox.Show(this, "No se pudo reproducir el archivo: " + ex.Message, "Reproducción", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void SelectAndPlay(LibraryItem item)
        {
            MediaList.SelectedItem = item;
            PlayItem(item);
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
            _currentPage = Convert.ToString(button.Tag);
            PageTitle.Text = _currentPage;
            RefreshLibrary();
            if (_currentPage == "Radio") CurrentMediaLabel.Text = "Agrega una emisora con «Radio URL».";
        }

        private void GroupBy_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (PageTitle == null || !(GroupByBox.SelectedItem is ComboBoxItem item)) return;
            PageTitle.Text = _currentPage == "Inicio" ? "Música · " + item.Content : _currentPage + " · " + item.Content;
            var property = Convert.ToString(item.Content);
            string propertyName = property == "Artista" ? "Artist" : property == "Álbum" ? "Album" : property == "Género" ? "Genre" : property == "Año" ? "Year" : property == "BPM" ? "Bpm" : "Mood";
            _libraryView.GroupDescriptions.Clear();
            _libraryView.GroupDescriptions.Add(new PropertyGroupDescription(propertyName));
            _libraryView.Refresh();
        }

        private void Search_Changed(object sender, TextChangedEventArgs e)
        {
            if (SearchBox == null) return;
            _searchText = SearchBox.Text == "Buscar en tu biblioteca…" ? "" : SearchBox.Text.Trim();
            RefreshLibrary();
        }

        private void RefreshLibrary()
        {
            _libraryView?.Refresh();
            UpdateCount();
        }

        private void OpenRadio_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new RadioUrlDialog { Owner = this };
            if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.StreamUrl)) return;
            var item = new LibraryItem { Title = dialog.StreamUrl, Path = dialog.StreamUrl, Kind = "Radio", Description = "Radio · Stream de red" };
            _items.Add(item); _currentPage = "Radio"; PageTitle.Text = "Radio"; RefreshLibrary();
            MediaList.SelectedItem = item;
            PlayItem(item);
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            if (ImageSurface.Visibility == Visibility.Visible) { ChangeImage(-1); return; }
            MoveSelection(-1);
        }
        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (ImageSurface.Visibility == Visibility.Visible) { ChangeImage(1); return; }
            MoveSelection(1);
        }
        private void MoveSelection(int direction)
        {
            if (MediaList.Items.Count == 0) return;
            var index = MediaList.SelectedIndex;
            if (_shuffle && direction > 0) index = new Random().Next(MediaList.Items.Count);
            else index = (index + direction + MediaList.Items.Count) % MediaList.Items.Count;
            MediaList.SelectedIndex = index;
            MediaList.ScrollIntoView(MediaList.SelectedItem);
        }
        private void ChangeImage(int step)
        {
            var images = _items.Where(item => item.Kind == "Imagen").ToList();
            if (images.Count == 0) return;
            _imageIndex = (_imageIndex + step + images.Count) % images.Count;
            ShowImage(images[_imageIndex].Path);
        }
        private void Shuffle_Click(object sender, RoutedEventArgs e)
        {
            _shuffle = !_shuffle;
            ((Button)sender).Foreground = _shuffle ? System.Windows.Media.Brushes.DeepSkyBlue : System.Windows.Media.Brushes.White;
        }
        private void Repeat_Click(object sender, RoutedEventArgs e)
        {
            _repeat = !_repeat;
            ((Button)sender).Foreground = _repeat ? System.Windows.Media.Brushes.DeepSkyBlue : System.Windows.Media.Brushes.White;
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
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Genre { get; set; }
        public string Mood { get; set; } = "Sin clasificar";
        public int Year { get; set; }
        public int Bpm { get; set; }
    }
}
