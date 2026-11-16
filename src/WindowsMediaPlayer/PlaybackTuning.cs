using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using Microsoft.Win32;

namespace WindowsMediaPlayer
{
    public partial class MainWindow
    {
        private Equalizer _activeEqualizer;
        private DispatcherTimer _videoInfoTimer;
        private bool _tuningControlsInitialized;

        static MainWindow()
        {
            EventManager.RegisterClassHandler(typeof(MainWindow), FrameworkElement.LoadedEvent,
                new RoutedEventHandler(MainWindow_Loaded));
        }

        private static void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var window = sender as MainWindow;
            if (window == null || window._tuningControlsInitialized) return;
            window.InitializeNativeTuningControls();
        }

        private void InitializeNativeTuningControls()
        {
            var panel = TuningStatus?.Parent as StackPanel;
            if (panel == null || _player == null) return;
            _tuningControlsInitialized = true;
            var index = panel.Children.IndexOf(TuningStatus) + 1;

            Action<UIElement> add = element => panel.Children.Insert(index++, element);
            Func<string, TextBlock> heading = text => new TextBlock
            {
                Text = text, Foreground = System.Windows.Media.Brushes.White,
                FontSize = 12, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 10, 0, 4)
            };
            Func<string, TextBlock> caption = text => new TextBlock
            {
                Text = text, Foreground = System.Windows.Media.Brushes.LightGray,
                FontSize = 11, Margin = new Thickness(0, 3, 0, 1)
            };

            add(heading("SALIDA DE AUDIO"));
            add(caption("Volumen del reproductor"));
            var volumeRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            var volume = new Slider { Minimum = 0, Maximum = 100, Value = Math.Max(0, Math.Min(100, _player.Volume)), Width = 150, VerticalAlignment = VerticalAlignment.Center };
            var volumeLabel = new TextBlock { Text = volume.Value.ToString("0") + "%", Foreground = System.Windows.Media.Brushes.LightGray, Width = 42, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 0, 0) };
            var muteButton = new Button { Content = _player.Mute ? "Activar sonido" : "Silenciar", Margin = new Thickness(4, 0, 0, 0), Padding = new Thickness(7, 3, 7, 3) };
            volume.ValueChanged += (s, args) =>
            {
                if (_player == null) return;
                _player.Volume = (int)Math.Round(volume.Value);
                volumeLabel.Text = _player.Volume.ToString() + "%";
            };
            muteButton.Click += (s, args) =>
            {
                if (_player == null) return;
                _player.Mute = !_player.Mute;
                muteButton.Content = _player.Mute ? "Activar sonido" : "Silenciar";
            };
            volumeRow.Children.Add(volume); volumeRow.Children.Add(volumeLabel); volumeRow.Children.Add(muteButton); add(volumeRow);
            add(caption("Canal de salida"));
            var channelBox = new ComboBox { Height = 27, Margin = new Thickness(0, 0, 0, 3) };
            channelBox.Items.Add(new ComboBoxItem { Content = "Estéreo", Tag = AudioOutputChannel.Stereo });
            channelBox.Items.Add(new ComboBoxItem { Content = "Solo izquierdo", Tag = AudioOutputChannel.Left });
            channelBox.Items.Add(new ComboBoxItem { Content = "Solo derecho", Tag = AudioOutputChannel.Right });
            channelBox.SelectedIndex = 0;
            channelBox.SelectionChanged += (s, args) =>
            {
                var selected = channelBox.SelectedItem as ComboBoxItem;
                if (_player != null && selected != null && selected.Tag is AudioOutputChannel channel)
                    _player.SetChannel(channel);
            };
            add(channelBox);
            add(new TextBlock
            {
                Text = "La frecuencia y profundidad de bits siguen las del archivo y las capacidades del dispositivo de audio.",
                Foreground = System.Windows.Media.Brushes.Gray, FontSize = 10, TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 3, 0, 4)
            });

            add(heading("AJUSTES DE IMAGEN"));
            var brightness = AddVideoSlider(add, caption, "Brillo", 0.5, 1.5, 1.0, VideoAdjustOption.Brightness);
            var contrast = AddVideoSlider(add, caption, "Contraste", 0.5, 1.5, 1.0, VideoAdjustOption.Contrast);
            var saturation = AddVideoSlider(add, caption, "Saturación", 0, 2, 1.0, VideoAdjustOption.Saturation);
            var resetButton = new Button { Content = "Restablecer imagen", Margin = new Thickness(0, 4, 0, 5), Padding = new Thickness(7, 4, 7, 4) };
            resetButton.Click += (s, args) =>
            {
                brightness.Value = 1; contrast.Value = 1; saturation.Value = 1;
                ApplyVideoAdjustments(brightness.Value, contrast.Value, saturation.Value);
            };
            add(resetButton);
            var resolutionLabel = new TextBlock { Text = "Resolución del vídeo: esperando señal…", Foreground = System.Windows.Media.Brushes.LightSkyBlue, FontSize = 10, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 5, 0, 4) };
            add(resolutionLabel);
            add(caption("Resolución de captura (no modifica el archivo original)"));
            var snapshotRow = new StackPanel { Orientation = Orientation.Horizontal };
            var snapshotSize = new ComboBox { Width = 145, Height = 27 };
            snapshotSize.Items.Add(new ComboBoxItem { Content = "Original", Tag = "0x0" });
            snapshotSize.Items.Add(new ComboBoxItem { Content = "1280 × 720", Tag = "1280x720" });
            snapshotSize.Items.Add(new ComboBoxItem { Content = "1920 × 1080", Tag = "1920x1080" });
            snapshotSize.Items.Add(new ComboBoxItem { Content = "2560 × 1440", Tag = "2560x1440" });
            snapshotSize.Items.Add(new ComboBoxItem { Content = "3840 × 2160", Tag = "3840x2160" });
            snapshotSize.SelectedIndex = 0;
            var snapshotButton = new Button { Content = "Capturar", Margin = new Thickness(5, 0, 0, 0), Padding = new Thickness(7, 3, 7, 3) };
            snapshotButton.Click += (s, args) => SaveVideoSnapshot(snapshotSize);
            snapshotRow.Children.Add(snapshotSize); snapshotRow.Children.Add(snapshotButton); add(snapshotRow);

            VideoAspectBox.SelectionChanged += VideoAspect_Changed;
            VideoScaleBox.SelectionChanged += VideoScale_Changed;
            Closed += (s, args) =>
            {
                _videoInfoTimer?.Stop();
                _activeEqualizer?.Dispose();
                _activeEqualizer = null;
            };
            _videoInfoTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _videoInfoTimer.Tick += (s, args) =>
            {
                if (_player == null || _player.VoutCount == 0) return;
                uint width = 0, height = 0;
                if (_player.Size(0, ref width, ref height) && width > 0 && height > 0)
                    resolutionLabel.Text = string.Format("Resolución del vídeo: {0} × {1} · {2:0.##} MP", width, height, (double)width * height / 1000000.0);
            };
            _videoInfoTimer.Start();
        }

        private Slider AddVideoSlider(Action<UIElement> add, Func<string, TextBlock> caption, string label,
            double minimum, double maximum, double initial, VideoAdjustOption option)
        {
            add(caption(label));
            var slider = new Slider { Minimum = minimum, Maximum = maximum, Value = initial, TickFrequency = 0.05, Margin = new Thickness(0, 0, 0, 2) };
            slider.ValueChanged += (s, e) => ApplyVideoAdjustments(
                option == VideoAdjustOption.Brightness ? (float)e.NewValue : ReadVideoValue(VideoAdjustOption.Brightness, 1),
                option == VideoAdjustOption.Contrast ? (float)e.NewValue : ReadVideoValue(VideoAdjustOption.Contrast, 1),
                option == VideoAdjustOption.Saturation ? (float)e.NewValue : ReadVideoValue(VideoAdjustOption.Saturation, 1));
            // Store each control so a change can apply all three current values.
            if (option == VideoAdjustOption.Brightness) _brightnessSlider = slider;
            if (option == VideoAdjustOption.Contrast) _contrastSlider = slider;
            if (option == VideoAdjustOption.Saturation) _saturationSlider = slider;
            slider.ValueChanged -= null;
            slider.ValueChanged += (s, e) => ApplyCurrentVideoAdjustments();
            add(slider);
            return slider;
        }

        private Slider _brightnessSlider;
        private Slider _contrastSlider;
        private Slider _saturationSlider;

        private float ReadVideoValue(VideoAdjustOption option, float fallback)
        {
            if (option == VideoAdjustOption.Brightness && _brightnessSlider != null) return (float)_brightnessSlider.Value;
            if (option == VideoAdjustOption.Contrast && _contrastSlider != null) return (float)_contrastSlider.Value;
            if (option == VideoAdjustOption.Saturation && _saturationSlider != null) return (float)_saturationSlider.Value;
            return fallback;
        }

        private void ApplyCurrentVideoAdjustments()
        {
            if (_brightnessSlider == null || _contrastSlider == null || _saturationSlider == null) return;
            ApplyVideoAdjustments((float)_brightnessSlider.Value, (float)_contrastSlider.Value, (float)_saturationSlider.Value);
        }

        private void ApplyVideoAdjustments(float brightness, float contrast, float saturation)
        {
            if (_player == null) return;
            try
            {
                _player.SetAdjustInt(VideoAdjustOption.Enable, 1);
                _player.SetAdjustFloat(VideoAdjustOption.Brightness, brightness);
                _player.SetAdjustFloat(VideoAdjustOption.Contrast, contrast);
                _player.SetAdjustFloat(VideoAdjustOption.Saturation, saturation);
            }
            catch (Exception ex) { TuningStatus.Text = "No se pudo ajustar la imagen: " + ex.Message; }
        }

        private void Equalizer_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (BassSlider == null || MidSlider == null || TrebleSlider == null || _player == null) return;
            Equalizer equalizer = null;
            try
            {
                equalizer = new Equalizer();
                var count = equalizer.BandCount;
                if (count == 0) { TuningStatus.Text = "VLC no ofrece bandas de ecualización."; equalizer.Dispose(); return; }
                for (uint band = 0; band < count; band++)
                {
                    var frequency = equalizer.BandFrequency(band);
                    var gain = frequency < 250 ? BassSlider.Value : frequency < 4000 ? MidSlider.Value : TrebleSlider.Value;
                    if (!equalizer.SetAmp((float)gain, band))
                        throw new InvalidOperationException("VLC rechazó el ajuste de una banda.");
                }
                if (!_player.SetEqualizer(equalizer)) throw new InvalidOperationException("VLC no pudo aplicar el ecualizador.");
                _activeEqualizer?.Dispose();
                _activeEqualizer = equalizer;
                TuningStatus.Text = string.Format(CultureInfo.CurrentCulture,
                    "Ecualizador activo · graves {0:+0;-0;0} dB · medios {1:+0;-0;0} dB · agudos {2:+0;-0;0} dB",
                    BassSlider.Value, MidSlider.Value, TrebleSlider.Value);
            }
            catch (Exception ex)
            {
                if (equalizer != null && !ReferenceEquals(equalizer, _activeEqualizer)) equalizer.Dispose();
                TuningStatus.Text = "No se pudo aplicar el ecualizador: " + (ex.InnerException?.Message ?? ex.Message);
            }
        }

        private void VideoAspect_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_player == null || VideoAspectBox?.SelectedItem is not ComboBoxItem item) return;
            try { _player.AspectRatio = string.IsNullOrWhiteSpace(Convert.ToString(item.Tag)) ? null : Convert.ToString(item.Tag); }
            catch (Exception ex) { TuningStatus.Text = "No se pudo cambiar la proporción: " + ex.Message; }
        }

        private void VideoScale_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_player == null || VideoScaleBox?.SelectedItem is not ComboBoxItem item) return;
            try { _player.Scale = float.Parse(Convert.ToString(item.Tag), CultureInfo.InvariantCulture); }
            catch (Exception ex) { TuningStatus.Text = "No se pudo cambiar la escala: " + ex.Message; }
        }

        private void SaveVideoSnapshot(ComboBox sizePicker)
        {
            if (_player == null || _player.VoutCount == 0)
            {
                TuningStatus.Text = "Reproduce un vídeo antes de capturar una imagen.";
                return;
            }
            var dialog = new SaveFileDialog { Title = "Guardar captura del vídeo", Filter = "Imagen PNG|*.png", FileName = "captura.png", DefaultExt = ".png", AddExtension = true };
            if (dialog.ShowDialog(this) != true) return;
            try
            {
                var size = Convert.ToString((sizePicker.SelectedItem as ComboBoxItem)?.Tag ?? "0x0").Split('x');
                var width = uint.Parse(size[0], CultureInfo.InvariantCulture);
                var height = uint.Parse(size[1], CultureInfo.InvariantCulture);
                if (_player.TakeSnapshot(0, dialog.FileName, width, height))
                    TuningStatus.Text = "Captura guardada: " + dialog.FileName;
                else TuningStatus.Text = "VLC no pudo guardar la captura. Inténtalo durante la reproducción.";
            }
            catch (Exception ex) { TuningStatus.Text = "Error al guardar la captura: " + (ex.InnerException?.Message ?? ex.Message); }
        }
    }
}
