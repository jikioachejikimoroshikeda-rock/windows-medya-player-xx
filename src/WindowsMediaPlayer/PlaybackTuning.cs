using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using LibVLCSharp.Shared;

namespace WindowsMediaPlayer
{
    public partial class MainWindow
    {
        private AudioEqualizer _activeEqualizer;

        private void Equalizer_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (BassSlider == null || MidSlider == null || TrebleSlider == null || _player == null) return;

            try
            {
                var equalizer = CreateEqualizer();
                if (equalizer == null)
                {
                    if (TuningStatus != null) TuningStatus.Text = "El ecualizador no está disponible en esta versión de VLC.";
                    return;
                }

                var bands = new float[10];
                for (var i = 0; i < bands.Length; i++)
                {
                    if (i <= 2) bands[i] = (float)BassSlider.Value;
                    else if (i <= 6) bands[i] = (float)MidSlider.Value;
                    else bands[i] = (float)TrebleSlider.Value;
                }

                var equalizerType = typeof(AudioEqualizer);
                var setAmp = equalizerType.GetMethods().FirstOrDefault(method =>
                    (method.Name == "SetAmp" || method.Name == "SetAmpAtIndex") && method.GetParameters().Length == 2);
                var setPlayerEqualizer = _player.GetType().GetMethods().FirstOrDefault(method =>
                    (method.Name == "SetEqualizer" || method.Name == "SetAudioEqualizer") && method.GetParameters().Length == 1);
                if (setAmp == null || setPlayerEqualizer == null)
                {
                    equalizer.Dispose();
                    if (TuningStatus != null) TuningStatus.Text = "No se pudo inicializar el ecualizador.";
                    return;
                }

                for (var i = 0; i < bands.Length; i++)
                    setAmp.Invoke(equalizer, new object[] { ConvertIndex(i, setAmp.GetParameters()[0].ParameterType), Convert.ToSingle(bands[i]) });

                setPlayerEqualizer.Invoke(_player, new object[] { equalizer });
                _activeEqualizer?.Dispose();
                _activeEqualizer = equalizer;
                if (TuningStatus != null) TuningStatus.Text = string.Format("Graves {0:+0;-0;0} dB · Medios {1:+0;-0;0} dB · Agudos {2:+0;-0;0} dB", BassSlider.Value, MidSlider.Value, TrebleSlider.Value);
            }
            catch (Exception ex)
            {
                if (TuningStatus != null) TuningStatus.Text = "No se pudo aplicar el ecualizador: " + (ex.InnerException?.Message ?? ex.Message);
            }
        }

        private static AudioEqualizer CreateEqualizer()
        {
            var type = typeof(AudioEqualizer);
            var factory = type.GetMethods(BindingFlags.Public | BindingFlags.Static).FirstOrDefault(method => method.Name == "CreateFromPreset" && method.GetParameters().Length == 1);
            if (factory != null) return factory.Invoke(null, new[] { ConvertIndex(0, factory.GetParameters()[0].ParameterType) }) as AudioEqualizer;

            var constructor = type.GetConstructors().FirstOrDefault(ctor => ctor.GetParameters().Length == 1);
            if (constructor == null) return null;
            return constructor.Invoke(new[] { ConvertIndex(0, constructor.GetParameters()[0].ParameterType) }) as AudioEqualizer;
        }

        private static object ConvertIndex(int value, Type targetType)
        {
            return Convert.ChangeType(value, targetType);
        }

        private void VideoAspect_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_player == null || VideoAspectBox?.SelectedItem is not ComboBoxItem item) return;
            try
            {
                var ratio = Convert.ToString(item.Tag);
                var method = _player.GetType().GetMethods().FirstOrDefault(candidate => candidate.Name == "SetAspectRatio" && candidate.GetParameters().Length == 1);
                if (method != null) method.Invoke(_player, new object[] { string.IsNullOrWhiteSpace(ratio) ? null : ratio });
            }
            catch (Exception ex)
            {
                if (TuningStatus != null) TuningStatus.Text = "No se pudo cambiar la proporción: " + (ex.InnerException?.Message ?? ex.Message);
            }
        }

        private void VideoScale_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_player == null || VideoScaleBox?.SelectedItem is not ComboBoxItem item) return;
            try
            {
                var scale = float.Parse(Convert.ToString(item.Tag), System.Globalization.CultureInfo.InvariantCulture);
                var method = _player.GetType().GetMethods().FirstOrDefault(candidate => candidate.Name == "SetScale" && candidate.GetParameters().Length == 1);
                if (method != null) method.Invoke(_player, new object[] { scale });
                else
                {
                    var property = _player.GetType().GetProperty("Scale");
                    if (property != null && property.CanWrite) property.SetValue(_player, scale, null);
                }
            }
            catch (Exception ex)
            {
                if (TuningStatus != null) TuningStatus.Text = "No se pudo cambiar la escala: " + (ex.InnerException?.Message ?? ex.Message);
            }
        }
    }
}
