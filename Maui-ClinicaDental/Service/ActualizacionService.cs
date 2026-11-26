// Service/ActualizacionService.cs — Revisión de actualizaciones (2026-11-25).
// La app se distribuye por APK (Ad Hoc), no por Play Store, así que
// Android no permite instalar en silencio: lo automático es revisar
// solo al abrir y avisar con un botón Descargar; la instalación la
// confirma el usuario con un toque. La revisión manual es el menú
// "Buscar actualización".
// La versión instalada es AppInfo.VersionString (del csproj:
// ApplicationDisplayVersion). La disponible viene de GET /version:
// "Actualizacion:Version" y "Actualizacion:UrlApk" en appsettings.
// Cuando publiques un APK nuevo, sube ApplicationDisplayVersion en
// el csproj y pon la misma versión y la URL pública del APK en el API.
// La revisión automática solo avisa una vez por sesión: si se toca
// "Después", no vuelve a preguntar hasta cerrar y abrir la app.

namespace Maui_ClinicaDental.Service;

public static class ActualizacionService
{
    private static bool _revisionAutomaticaHecha;

    public static async Task RevisarAsync(bool manual)
    {
        if (!manual && _revisionAutomaticaHecha)
            return;

        var api = new ApiService();
        var disponible = await api.GetVersionAsync();
        if (disponible is null)
        {
            if (manual)
                await MostrarAlertaAsync(
                    "No se pudo revisar",
                    "Sin conexión al API. Revisa que esté corriendo e inténtalo de nuevo.");
            return;
        }

        _revisionAutomaticaHecha = true;

        var instalada = Microsoft.Maui.ApplicationModel.AppInfo.Current.VersionString;
        if (!EsMasNueva(disponible.Version, instalada))
        {
            if (manual)
                await MostrarAlertaAsync(
                    "Estás al día",
                    $"Tienes la versión más reciente ({instalada}).");
            return;
        }

        var descargar = await Shell.Current.DisplayAlertAsync(
            "Actualización disponible",
            $"Hay una versión nueva ({disponible.Version}). La instalada es {instalada}." +
            (string.IsNullOrWhiteSpace(disponible.Notas) ? "" : $"\n{disponible.Notas}") +
            "\n¿Quieres descargarla?",
            "Descargar", "Después");
        if (descargar && !string.IsNullOrWhiteSpace(disponible.UrlApk))
            await Microsoft.Maui.ApplicationModel.Browser.OpenAsync(
                disponible.UrlApk,
                Microsoft.Maui.ApplicationModel.BrowserLaunchMode.SystemPreferred);
    }

    // Compara versión por versión (1.0 < 1.0.1 < 1.1). Si alguna parte
    // no es un número, se trata como 0.
    private static bool EsMasNueva(string disponible, string instalada)
    {
        var a = disponible.Split('.');
        var b = instalada.Split('.');
        for (var i = 0; i < Math.Max(a.Length, b.Length); i++)
        {
            var x = i < a.Length && int.TryParse(a[i], out var vx) ? vx : 0;
            var y = i < b.Length && int.TryParse(b[i], out var vy) ? vy : 0;
            if (x != y) return x > y;
        }
        return false;
    }

    private static Task MostrarAlertaAsync(string titulo, string mensaje)
    {
        var pagina = Shell.Current ?? Application.Current?.MainPage;
        return pagina is null
            ? Task.CompletedTask
            : pagina.DisplayAlertAsync(titulo, mensaje, "OK");
    }
}
