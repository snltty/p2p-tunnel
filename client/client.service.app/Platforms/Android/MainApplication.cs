using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;

namespace client.service.app
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();

            //KeepManager.GetInstance().RegisterKeep(this);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                StartForegroundService(new Intent(this, typeof(ForegroundService)));
            }

            else
            {
                StartService(new Intent(this, typeof(ForegroundService)));
            }

        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }


    public sealed class BootReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            Intent toIntent = context.PackageManager.GetLaunchIntentForPackage(context.PackageName);
            context.StartActivity(toIntent);
        }
    }

}