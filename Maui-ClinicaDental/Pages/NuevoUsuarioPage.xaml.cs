using Maui_ClinicaDental.Service;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Maui_ClinicaDental.Pages;

// NuevoUsuarioPage — formulario para crear un usuario (2026-11-20).
// Solo la abre un Admin, desde UsuariosPage. El usuario nuevo debe
// cambiar su contraseña en cuanto entre (POST /auth/cambiar-password).
public partial class NuevoUsuarioPage : ContentPage
{
    public NuevoUsuarioPage()
    {
        InitializeComponent();
        BindingContext = new NuevoUsuarioViewModel();
    }
}

public class NuevoUsuarioViewModel : INotifyPropertyChanged
{
    private readonly ApiService _api;
    public event PropertyChangedEventHandler? PropertyChanged;

    public List<string> Roles { get; } = ["Admin", "Recepcionista"];

    private string _nombreUsuario = string.Empty;
    public string NombreUsuario
    {
        get => _nombreUsuario;
        set { _nombreUsuario = value; OnPropertyChanged(); }
    }

    private string _password = string.Empty;
    public string Password
    {
        get => _password;
        set { _password = value; OnPropertyChanged(); }
    }

    private string _rol = "Recepcionista";
    public string Rol
    {
        get => _rol;
        set { _rol = value; OnPropertyChanged(); }
    }

    private string _mensajeError = string.Empty;
    public string MensajeError
    {
        get => _mensajeError;
        set { _mensajeError = value; OnPropertyChanged(); OnPropertyChanged(nameof(TieneError)); }
    }

    public bool TieneError => !string.IsNullOrWhiteSpace(MensajeError);

    public ICommand GuardarCommand { get; }
    public ICommand CancelarCommand { get; }

    public NuevoUsuarioViewModel(ApiService? api = null)
    {
        _api = api ?? new ApiService();
        GuardarCommand = new Command(async () => await GuardarAsync());
        CancelarCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }

    private async Task GuardarAsync()
    {
        MensajeError = string.Empty;

        if (string.IsNullOrWhiteSpace(NombreUsuario))
        {
            MensajeError = "El nombre de usuario es obligatorio.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password) || Password.Length < 8)
        {
            MensajeError = "La contraseña debe tener al menos 8 caracteres.";
            return;
        }

        if (Rol != "Admin" && Rol != "Recepcionista")
        {
            MensajeError = "Elige un rol: Admin o Recepcionista.";
            return;
        }

        var (ok, error) = await _api.CrearUsuarioAsync(NombreUsuario.Trim(), Password, Rol);
        if (!ok)
        {
            MensajeError = error ?? "El servidor no creó el usuario.";
            return;
        }

        await Shell.Current.DisplayAlertAsync(
            "Usuario creado",
            $"{NombreUsuario.Trim()} ({Rol}). Dile que cambie su contraseña en cuanto entre.",
            "OK");
        await Shell.Current.GoToAsync("..");
    }

    private void OnPropertyChanged([CallerMemberName] string? nombre = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
}
