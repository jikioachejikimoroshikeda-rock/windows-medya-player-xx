# Windows Media Player Next (native)

Native Windows desktop player built with WPF and LibVLCSharp. The web mockup files in the repository root are retained as a visual reference; the Windows application is in `src/WindowsMediaPlayer/WindowsMediaPlayer.sln`.

## Features

- Native WPF interface with library sections for Music, Videos, Podcasts, Audiobooks, and Radio.
- Play common audio and video formats using the VLC media engine. Actual format support depends on the codecs and stream; DRM-protected media is not supported.
- Add local media files or open a network radio/stream URL.
- Image viewer for JPG, PNG, GIF, BMP, and WebP files, with previous/next navigation and fit-to-window display.
- Search and playback queue.

## Build

1. Install Visual Studio with the **.NET desktop development** workload and the .NET Framework 4.8 Developer Pack.
2. Open `src/WindowsMediaPlayer/WindowsMediaPlayer.sln`.
3. Restore NuGet packages and build the `WindowsMediaPlayer` project for **x64**.
4. Run on Windows 7 SP1 or newer. Windows 7 may need its platform updates and the current Microsoft Visual C++ runtime. The app packages the VLC 3 Windows x64 runtime through NuGet.

The project targets .NET Framework 4.8 to retain compatibility with Windows 7 SP1 and later Windows versions. Windows 9 was never released; Windows 8.1 is included in the compatibility target.
