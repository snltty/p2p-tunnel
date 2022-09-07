using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using common.libs.extends;
using System.Net.Sockets;
using System.Net;

namespace client.service.app
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            Window.SetFlags(Android.Views.WindowManagerFlags.TranslucentStatus, Android.Views.WindowManagerFlags.TranslucentStatus);
            Window.SetStatusBarColor(Android.Graphics.Color.Transparent);
            Window.SetNavigationBarColor(Android.Graphics.Color.Transparent);

            PowerManager pm = (PowerManager)GetSystemService(PowerService);
            var wakelock = pm.NewWakeLock(WakeLockFlags.Partial | WakeLockFlags.OnAfterRelease, "mywakelock");
            if (wakelock != null)
            {
                wakelock.Acquire();
            }

            intent = new Intent(this, typeof(MyService));
            StartService(intent);

            base.OnCreate(savedInstanceState);

        }

        static Intent intent;

    }

    [Service(IsolatedProcess = false, Exported = true, Name = "client.service.app.MyService", Process = "client.service.app.myservice_process")]
    public class MyService : Service
    {
        public MyService()
        {
        }
        public override IBinder OnBind(Intent intent)
        {
            return null;
        }
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            return StartCommandResult.Sticky;

        }
        public override void OnCreate()
        {
            base.OnCreate();

            Startup.Start();
        }

    }

}