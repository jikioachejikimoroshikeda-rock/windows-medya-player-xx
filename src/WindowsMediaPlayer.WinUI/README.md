# Edición WinUI 3

Se añadió una aplicación WinUI 3 independiente en `src/WindowsMediaPlayer.WinUI`. La aplicación WPF existente se conserva sin cambios para no romper sus funciones actuales.

## Requisitos

- Windows 10 versión 2004 (build 19041) o posterior.
- Visual Studio 2022 con la carga de trabajo **Desarrollo de aplicaciones de Windows** y el SDK de Windows 10/11.
- .NET 8 SDK.

## Ejecutar

Abre `src/WindowsMediaPlayer.WinUI/WindowsMediaPlayer.WinUI.csproj` en Visual Studio, restaura los paquetes NuGet y ejecuta el proyecto. El proyecto es unpackaged y depende del Windows App SDK instalado por NuGet.

La edición WinUI incluye biblioteca local con búsqueda y navegación Inicio/Música/Vídeos, selección múltiple de archivos y controles de transporte nativos mediante `MediaPlayerElement`. Los formatos disponibles dependen de los códecs instalados en Windows. Las funciones existentes de VLC, etiquetas, imágenes, radio y ajustes avanzados siguen en la edición WPF; no se han migrado todavía.
