// Service/CargandoScope.cs — Indicador de carga (2026-11-25)
// Uso en cualquier método que llama al API:
//     using var _ = CargandoScope.Activar(v => EstaCargando = v);
//
// Pone EstaCargando en true al entrar al método y lo vuelve a
// false al salir, aunque haya un return antes o una excepción:
// "using var" llama a Dispose siempre al salir del método, así
// que el indicador nunca se queda prendido.

namespace Maui_ClinicaDental.Service;

public static class CargandoScope
{
    public static IDisposable Activar(Action<bool> poner)
    {
        poner(true);
        return new AlTerminar(() => poner(false));
    }

    private sealed class AlTerminar : IDisposable
    {
        private readonly Action _accion;
        public AlTerminar(Action accion) => _accion = accion;
        public void Dispose() => _accion();
    }
}
