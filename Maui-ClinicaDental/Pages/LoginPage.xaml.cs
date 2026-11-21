using Maui_ClinicaDental.Service;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Maui_ClinicaDental.Pages;

// LoginPage — inicio de sesión (2026-11-20). Si no hay sesión
// guardada, App.xaml.cs abre esta página primero. Al entrar, el
// token queda en SecureStorage (ApiService) y la página se
// reemplaza por AppShell.
public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
        BindingContext = new LoginViewModel();
    }
}

public class LoginViewModel : INotifyPropertyChanged
{
    private readonly ApiService _api;
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _usuario = string.Empty;
    public string Usuario
    {
        get => _usuario;
        set { _usuario = value; OnPropertyChanged(); }
    }

    private string _password = string.Empty;
    public string Password
    {
        get => _password;
        set { _password = value; OnPropertyChanged(); }
    }

    private string _mensajeError = string.Empty;
    public string MensajeError
    {
        get => _mensajeError;
        set { _mensajeError = value; OnPropertyChanged(); OnPropertyChanged(nameof(TieneError)); }
    }

    public bool TieneError => !string.IsNullOrWhiteSpace(MensajeError);

    public ICommand EntrarCommand { get; }

    public LoginViewModel(ApiService? api = null)
    {
        _api = api ?? new ApiService();
        EntrarCommand = new Command(async () => await EntrarAsync());
    }

    private async Task EntrarAsync()
    {
        MensajeError = string.Empty;

        if (string.IsNullOrWhiteSpace(Usuario) || string.IsNullOrWhiteSpace(Password))
        {
            MensajeError = "Escribe tu usuario y tu contraseña.";
            return;
        }

        var (ok, error) = await _api.LoginAsync(Usuario.Trim(), Password);
        if (!ok)
        {
            MensajeError = error ?? "No se pudo iniciar sesión.";
            return;
        }

        // Sesión guardada: la app entra directo al Shell.
        Application.Current!.MainPage = new AppShell();
    }

    private void OnPropertyChanged([CallerMemberName] string? nombre = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
}
