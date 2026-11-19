#if DEBUG
using Android.App;
using Android.Runtime;
using Maui_ClinicaDental;

[Application(UsesCleartextTraffic = true)]
#else
using Android.App;
using Android.Runtime;
using Maui_ClinicaDental;

[Application]
#endif
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}