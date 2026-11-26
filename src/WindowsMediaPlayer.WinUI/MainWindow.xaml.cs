using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace WindowsMediaPlayer.WinUI;

public sealed partial class MainWindow : Window
{
    private readonly ObservableCollection<LibraryItem> _items = new();
    private readonly MediaPlayer _mediaPlayer = new();
    private string _currentPage = "Inicio";
    private string _searchText = string.Empty;

    public MainWindow()
    {
        InitializeComponent();
        LibraryList.ItemsSource = _items;
        PlayerElement.SetMediaPlayer(_mediaPlayer);
        Closed += (_, _) => _mediaPlayer.Dispose();
        UpdateCount();
    }

    private async void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.AddRange(new[]
        {
            ".mp3", ".wav", ".flac", ".m4a", ".aac", ".wma", ".ogg", ".opus",
            ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".webm", ".mpeg", ".mpg", ".m4v"
        });
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));

        IReadOnlyList<StorageFile> files;
        try
        {
            files = await picker.PickMultipleFilesAsync();
        }
        catch (Exception ex)
        {
            ShowError("No se pudieron abrir los archivos", ex.Message);
            return;
        }

        if (files.Count == 0)
            return;

        LibraryItem? firstAdded = null;
        foreach (var file in files)
        {
            if (_items.Any(item => string.Equals(item.File.Path, file.Path, StringComparison.OrdinalIgnoreCase)))
                continue;

            var item = new LibraryItem(file);
            _items.Add(item);
            firstAdded ??= item;
        }

        UpdateCount();
        if (firstAdded is not null)
        {
            LibraryList.SelectedItem = firstAdded;
            PlayItem(firstAdded);
        }
    }

    private void LibraryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LibraryList.SelectedItem is LibraryItem item)
            PlayItem(item);
    }

    private void PlayItem(LibraryItem item)
    {
        try
        {
            _mediaPlayer.Source = MediaSource.CreateFromStorageFile(item.File);
            _mediaPlayer.Play();
            EmptyState.Visibility = Visibility.Collapsed;
            NowPlayingTitle.Text = item.Title;
        }
        catch (Exception ex)
        {
            ShowError("No se pudo reproducir el archivo", ex.Message);
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = SearchBox.Text.Trim();
        RefreshLibrary();
    }

    private void Navigate_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string page })
            return;

        _currentPage = page;
        PageTitle.Text = page;
        RefreshLibrary();
    }

    private void RefreshLibrary()
    {
        var filtered = _items.Where(item =>
            (_currentPage == "Inicio" ||
             (_currentPage == "Música" && item.Kind == "Audio") ||
             (_currentPage == "Vídeos" && item.Kind == "Vídeo")) &&
            (string.IsNullOrWhiteSpace(_searchText) ||
             item.Title.Contains(_searchText, StringComparison.CurrentCultureIgnoreCase) ||
             item.Details.Contains(_searchText, StringComparison.CurrentCultureIgnoreCase)))
            .ToList();

        LibraryList.ItemsSource = filtered;
        UpdateCount(filtered.Count);
    }

    private void UpdateCount(int? visibleCount = null)
    {
        LibraryCount.Text = $"·  {visibleCount ?? _items.Count} elementos";
    }

    private async void ShowError(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "Cerrar",
            XamlRoot = Content.XamlRoot
        };
        await dialog.ShowAsync();
    }
}

public sealed class LibraryItem
{
    public StorageFile File { get; }
    public string Title { get; }
    public string Details { get; }
    public string Kind { get; }

    public LibraryItem(StorageFile file)
    {
        File = file;
        Title = Path.GetFileNameWithoutExtension(file.Name);
        Kind = ".mp4 .mkv .avi .mov .wmv .webm .mpeg .mpg .m4v".Contains(Path.GetExtension(file.Name), StringComparison.OrdinalIgnoreCase)
            ? "Vídeo"
            : "Audio";
        Details = $"{Kind} · {file.Name}";
    }
}
