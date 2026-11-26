// Service/AvisosCitas.cs — Aviso de citas nuevas (2026-11-25).
// Guarda en Preferences el CitaId más alto que el usuario ya vio.
// Una cita es "nueva" si su id es mayor. Tocar el diente o abrir
// Citas marca todas como vistas. La cita que uno mismo crea se marca
// vista al guardarla, para que el diente no se encienda por ella.
// La primera vez no avisa de lo que ya existe: marca lo actual
// como visto sin encender el diente.

using Microsoft.Maui.Storage;

namespace Maui_ClinicaDental.Service;

public static class AvisosCitas
{
    private const string Clave = "UltimaCitaVistaId";

    public static int UltimaVista => Preferences.Get(Clave, 0);
    public static bool Existe => Preferences.ContainsKey(Clave);

    public static void MarcarVista(int citaId)
    {
        if (citaId > UltimaVista)
            Preferences.Set(Clave, citaId);
    }
}
