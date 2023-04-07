using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.App;

namespace client.service.app
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            Window.SetFlags(WindowManagerFlags.TranslucentStatus, WindowManagerFlags.TranslucentStatus);
            Window.SetStatusBarColor(Android.Graphics.Color.Transparent);
            Window.SetNavigationBarColor(Android.Graphics.Color.Transparent);

            KeepManager.GetInstance().RegisterKeep(this);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                StartForegroundService(new Intent(this, typeof(ForegroundService)));
            }

            else
            {
                StartService(new Intent(this, typeof(ForegroundService)));
            }


            base.OnCreate(savedInstanceState);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }

    [Service]
    public sealed class ForegroundService : Service
    {
        private static readonly int SERVICE_ID = 10000;

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            NotificationChannel notificationChannel;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                notificationChannel = new NotificationChannel("service", "service", NotificationImportance.High);
                notificationChannel.EnableLights(true);
                notificationChannel.SetShowBadge(true);
                notificationChannel.LockscreenVisibility = NotificationVisibility.Private;
                NotificationManager manager = (NotificationManager)GetSystemService(NotificationService);
                if (manager != null)
                {
                    manager.CreateNotificationChannel(notificationChannel);
                }
            }

            //PendingIntent pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.Mutable);
            Notification notification = new NotificationCompat.Builder(this, "service")
                  .SetSmallIcon(Resource.Drawable.appiconfg)
                  //.SetContentIntent(pendingIntent)
                  .SetContentTitle("保活")
                  .SetContentText("保活")
                  .SetOngoing(true)
                  .Build();
            notification.Flags |= NotificationFlags.NoClear;
            StartForeground(SERVICE_ID, notification);

            Startup.Start();
            return StartCommandResult.Sticky;
        }
    }

    public class AliveActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Android.Views.Window window = this.Window;
            window.SetGravity(GravityFlags.Start | GravityFlags.Top);
            WindowManagerLayoutParams @params = window.Attributes;

            //宽高
            @params.Width = 1;
            @params.Height = 1;
            //设置位置
            @params.X = 0;
            @params.Y = 0;
            window.Attributes = @params;

            KeepManager.GetInstance().SetKeep(this);
        }


        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
    public sealed class KeepManager
    {
        private static readonly KeepManager mInstance = new KeepManager();

        private WeakReference<Android.App.Activity> mKeepActivity;

        public KeepManager()
        {

        }

        public static KeepManager GetInstance()
        {
            return mInstance;
        }

        public void RegisterKeep(Context context)
        {
            StartKeep(context);
        }

        public void StartKeep(Context context)
        {
            Intent intent = new Intent(context, typeof(AliveActivity));
            // 结合 taskAffinity 一起使用 在指定栈中创建这个activity
            intent.SetFlags(ActivityFlags.NewTask);
            context.StartActivity(intent);
        }

        public void SetKeep(AliveActivity keep)
        {
            mKeepActivity = new WeakReference<Activity>(keep);
        }
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