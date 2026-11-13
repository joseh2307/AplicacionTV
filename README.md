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

Apple no permite instalar un APK ni compilar apps de iPhone en Windows. Por eso este repositorio compila la app en una Mac de GitHub (GitHub Actions, archivo `.github/workflows/ios.yml`) cada vez que se suben cambios, y publica **`TVMundial.ipa`** en la sección **Releases** (etiqueta `ios-latest`).

El `.ipa` sale sin firmar. Para instalarlo en el iPhone:

1. En una PC con Windows instala [Sideloadly](https://sideloadly.io) e iTunes (versión de apple.com).
2. Descarga `TVMundial.ipa` desde *Releases* → `ios-latest`.
3. Conecta el iPhone por cable, arrastra el `.ipa` a Sideloadly e inicia sesión con tu Apple ID.
4. En el iPhone: *Ajustes → General → VPN y gestión de dispositivos* → confía en tu Apple ID. En iOS 16 o superior activa también *Ajustes → Privacidad y seguridad → Modo de desarrollador*.

Con un Apple ID gratuito la app caduca cada **7 días** y hay que volver a instalarla desde Sideloadly. Con una cuenta de Apple Developer de pago (99 USD al año) dura 1 año.
