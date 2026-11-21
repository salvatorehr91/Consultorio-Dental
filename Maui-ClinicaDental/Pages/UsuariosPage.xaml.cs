using Maui_ClinicaDental.Models;
using Maui_ClinicaDental.Service;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Maui_ClinicaDental.Pages;

// UsuariosPage — administración de usuarios (2026-11-20). Solo la
// abre un Admin (el menú del Shell lo revisa y el servidor devuelve
// 403 si no lo es). Sin datos de ejemplo: un usuario inventado en
// la lista sería peligroso. Los usuarios no se eliminan, se
// desactivan (Activo = false): así la bitácora puede seguir
// diciendo quién hizo cada cambio.
public partial class UsuariosPage : ContentPage
{
    public UsuariosPage()
    {
        InitializeComponent();
        BindingContext = new UsuariosViewModel();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is UsuariosViewModel vm)
            await vm.CargarDesdeApiAsync();
    }
}

public class UsuarioItem
{
    public required int Id { get; init; }
    public required string NombreUsuario { get; init; }
    public required string Rol { get; init; }
    public required bool Activo { get; init; }
    public string Inicial => string.IsNullOrWhiteSpace(NombreUsuario)
        ? "?"
        : NombreUsuario[..1].ToUpperInvariant();
    public string EstadoTexto => Activo ? "Activo" : "Inactivo";
    public string EstadoColor => Activo ? "#0E7C7B" : "#B3261E";
}

public class UsuariosViewModel : INotifyPropertyChanged
{
    private readonly ApiService _api;
    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<UsuarioItem> Usuarios { get; } = [];

    public string TotalUsuariosTexto => $"{Usuarios.Count} usuario(s)";

    private string _avisoApi = string.Empty;
    public string AvisoApi
    {
        get => _avisoApi;
        private set { _avisoApi = value; OnPropertyChanged(); OnPropertyChanged(nameof(TieneAvisoApi)); }
    }
    public bool TieneAvisoApi => !string.IsNullOrWhiteSpace(AvisoApi);

    public ICommand NuevoUsuarioCommand { get; }
    public ICommand VerUsuarioCommand { get; }

    public UsuariosViewModel(ApiService? api = null)
    {
        _api = api ?? new ApiService();
        NuevoUsuarioCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(NuevoUsuarioPage)));
        VerUsuarioCommand = new Command<UsuarioItem>(async u => await VerUsuarioAsync(u));
    }

    public async Task CargarDesdeApiAsync()
    {
        var usuarios = await _api.GetUsuariosAsync();
        if (usuarios is null)
        {
            AvisoApi = _api.UltimoError ?? "Sin conexión al API.";
            return;
        }

        AvisoApi = string.Empty;
        Usuarios.Clear();
        foreach (var u in usuarios)
        {
            Usuarios.Add(new UsuarioItem
            {
                Id = u.UsuarioId,
                NombreUsuario = u.NombreUsuario,
                Rol = u.Rol,
                Activo = u.Activo
            });
        }
        OnPropertyChanged(nameof(TotalUsuariosTexto));
    }

    private async Task VerUsuarioAsync(UsuarioItem? usuario)
    {
        if (usuario is null) return;

        var accion = await Shell.Current.DisplayActionSheetAsync(
            usuario.NombreUsuario,
            "Cerrar",
            null,
            "Restablecer contraseña",
            usuario.Activo ? "Desactivar" : "Reactivar");

        switch (accion)
        {
            case "Restablecer contraseña":
                await RestablecerPasswordAsync(usuario);
                break;
            case "Desactivar":
                await CambiarEstadoAsync(usuario, false);
                break;
            case "Reactivar":
                await CambiarEstadoAsync(usuario, true);
                break;
        }
    }

    private async Task RestablecerPasswordAsync(UsuarioItem usuario)
    {
        var nueva = await Shell.Current.DisplayPromptAsync(
            "Restablecer contraseña",
            $"Contraseña nueva para {usuario.NombreUsuario} (mínimo 8 caracteres). Dile que la cambie en cuanto entre.",
            "Guardar",
            "Cancelar",
            "Mínimo 8 caracteres",
            100);
        if (string.IsNullOrWhiteSpace(nueva)) return;

        var (ok, error) = await _api.RestablecerPasswordAsync(usuario.Id, nueva.Trim());
        if (!ok)
        {
            await Shell.Current.DisplayAlertAsync("No se pudo cambiar", error ?? "El servidor no cambió la contraseña.", "OK");
            return;
        }

        await Shell.Current.DisplayAlertAsync(
            "Contraseña actualizada",
            $"La contraseña de {usuario.NombreUsuario} ya es otra. \nValidar nuevamente con la nueva contraseña.",
            "OK");
    }

    private async Task CambiarEstadoAsync(UsuarioItem usuario, bool activo)
    {
        var accion = activo ? "reactivar" : "desactivar";
        var confirmar = await Shell.Current.DisplayAlertAsync(
            activo ? "Reactivar usuario" : "Desactivar usuario",
            activo
                ? $"¿Reactivar a {usuario.NombreUsuario}? Volverá a poder entrar."
                : $"¿Desactivar a {usuario.NombreUsuario}? Ya no podrá entrar, pero sus cambios anteriores se conservan.",
            activo ? "Reactivar" : "Desactivar",
            "Cancelar");
        if (!confirmar) return;

        var (ok, error) = await _api.CambiarEstadoUsuarioAsync(usuario.Id, activo);
        if (!ok)
        {
            await Shell.Current.DisplayAlertAsync("No se pudo cambiar", error ?? "El servidor no cambió el estado.", "OK");
            return;
        }

        await CargarDesdeApiAsync();
    }

    private void OnPropertyChanged([CallerMemberName] string? nombre = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
}
