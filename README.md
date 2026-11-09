# TV Mundial (AplicacionTV)

App para ver TV en vivo desde el celular (Android e iOS), hecha con **.NET MAUI** (el sucesor oficial de Xamarin.Forms), C# y XAML. No usa base de datos.

## Funciones

- Menú de categorías: **Fútbol**, Deportes, Favoritos, En Español, Entretenimiento, Películas, Series, Noticias, Infantil, Música, Documentales y más (31 en total).
- Lista de canales con logo, país y bandera.
- Búsqueda por nombre y filtro por país.
- Reproductor HLS integrado (MediaElement: ExoPlayer en Android, AVPlayer en iOS), con opción de abrir el canal en otra app (VLC, etc.) si falla.
- Favoritos guardados en el teléfono (archivo `favoritos.m3u`, sin base de datos).
- Caché de las listas por 6 horas; si no hay internet usa la última copia.

## Fuente de canales

Listas públicas de [iptv-org](https://github.com/iptv-org/iptv):

- `https://iptv-org.github.io/iptv/categories/{categoria}.m3u`
- `https://iptv-org.github.io/iptv/languages/spa.m3u`

La API `https://iptv-org.github.io/api/channels.json` solo trae información de los canales (sin enlaces de video); por eso la app usa las listas M3U, que sí incluyen los enlaces.

"Fútbol" es un filtro sobre Deportes por palabras clave (ESPN, beIN, DAZN, TUDN, Claro Sports, liga, football, etc.).

> Solo hay canales que se transmiten de forma gratuita y pública. Las ligas de pago (por ejemplo, partidos exclusivos de plataformas por suscripción) normalmente no están. Algunos canales pueden estar caídos o bloqueados según tu país.

## Instalar en Android

Descarga `TVMundial.apk` de este repositorio, ábrelo en el teléfono y permite "instalar apps de origen desconocido".

## Compilar

Requisitos: .NET 10 SDK con las cargas de trabajo `maui-android` / `maui-ios`, JDK 17 y Android SDK.

```powershell
# Android (APK)
dotnet publish AplicacionTV.csproj -f net10.0-android -c Release
```

En esta PC se usa `build-apk.ps1`, que apunta a un SDK portátil en `D:\DevTools` (el disco C: no tenía espacio).

### iOS

Compilar para iPhone requiere una **Mac con Xcode** (enlazada desde Visual Studio o directamente) y una cuenta de Apple Developer para firmar e instalar:

```bash
dotnet publish AplicacionTV.csproj -f net10.0-ios -c Release
```
