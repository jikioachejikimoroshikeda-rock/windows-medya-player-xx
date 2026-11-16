# Windows Media Player Next — aplicación nativa para Windows

Reproductor de escritorio nativo en C# con WPF, LibVLCSharp y etiquetas multimedia TagLibSharp. La maqueta web original se conserva en la raíz como referencia visual; la aplicación de Windows está en `src/WindowsMediaPlayer/WindowsMediaPlayer.sln`.

## Incluye

1. Aplicación de escritorio WPF en C# (no es una página web).
2. Reproducción local de formatos de audio y vídeo que admita el motor VLC y los códecs disponibles. DRM y formatos dañados/protegidos no se pueden garantizar.
3. Visor de imágenes local para JPG/JPEG, PNG, BMP, GIF y TIFF, con navegación anterior/siguiente entre imágenes abiertas.
4. Biblioteca con búsqueda, cola y agrupación por artista, álbum, género, año, BPM o estado de ánimo. Para audio se leen las etiquetas ID3/Vorbis compatibles; si falta un dato, se indica como desconocido/sin género. El estado de ánimo se deja sin clasificar si el archivo no contiene ese dato.
5. Apertura de emisoras y streams de radio mediante URL HTTP(S)/RTSP.

También ofrece secciones de Música, Vídeos, Podcasts, Audiolibros y Radio. Podcasts y audiolibros aceptan archivos multimedia locales añadidos por el usuario; la clasificación especializada de esos elementos aún debe asignarse en una iteración posterior.

## Compilar

1. En Windows, instala Visual Studio con la carga de trabajo **Desarrollo de escritorio de .NET** y el Developer Pack de .NET Framework 4.8.
2. Abre `src/WindowsMediaPlayer/WindowsMediaPlayer.sln`.
3. Restaura los paquetes NuGet y compila `WindowsMediaPlayer` en x64.
4. Ejecuta en Windows 7 SP1 o posterior. Windows 7 puede necesitar las actualizaciones de plataforma y el runtime de Microsoft Visual C++ apropiado. VLC se distribuye mediante el paquete NuGet de Windows x64.

El objetivo .NET Framework 4.8 mantiene como objetivo Windows 7 SP1 y versiones posteriores; Windows 8.1 está incluido. Windows 9 nunca se publicó.

## Límites

«Todo tipo» de medios no se puede prometer: la reproducción depende del códec, contenedor, fuente y arquitectura. VLC admite una gama amplia, pero no DRM. El proyecto es x64, así que los complementos VLC deben corresponder a esa arquitectura. Esta solución se ha preparada como código fuente; la compilación debe validarse en Windows con Visual Studio antes de distribuir un ejecutable.
