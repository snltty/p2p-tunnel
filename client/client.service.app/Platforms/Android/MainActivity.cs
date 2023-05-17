using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.App;
using Activity = Android.App.Activity;
using Intent = Android.Content.Intent;


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

            base.OnCreate(savedInstanceState);

             Intent intent = new Intent(this, typeof(ForegroundService));
             StartForegroundService(intent);
        }

        protected override void OnStart()
        {
            base.OnStart();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

    }


    //https://www.superweb999.com/article/1971766.html
    [Service(Name = "com.myapp.android.BackgroundService", Exported = true)]
    [IntentFilter(new string[] { "com.myapp.droid.BackgroundService" })]
    public sealed class ForegroundService : Service
    {
        private static readonly int SERVICE_ID = 10000;
        private static readonly string CHANNEL_ID = "service";
        private static readonly string CHANNEL_NAME = "service";
        static System.Timers.Timer timer;

        Intent intent;
        public override IBinder OnBind(Intent intent)
        {
            intent.SetFlags(ActivityFlags.NewTask);
            this.intent = intent;
            return null;
        }

        public override void OnCreate()
        {
            base.OnCreate();
            //Startup.Start();

            NotificationChannel notificationChannel = new NotificationChannel(CHANNEL_ID, CHANNEL_NAME, NotificationImportance.High);
            notificationChannel.EnableLights(true);
            notificationChannel.SetShowBadge(true);
            notificationChannel.LockscreenVisibility = NotificationVisibility.Public;
            NotificationManager manager = (NotificationManager)GetSystemService(NotificationService);
            manager.CreateNotificationChannel(notificationChannel);

            StartForeground(SERVICE_ID, CreateNotification("保活"));

            int index = 0;
            timer = new System.Timers.Timer();
            timer.Interval = 1000;
            timer.Elapsed += (sender, e) =>
            {
                manager?.Notify(SERVICE_ID, CreateNotification((++index).ToString()));
            };
            timer.Start();
        }

        private Notification CreateNotification(string content)
        {
            PendingIntent pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.Mutable);
            Notification notification = new NotificationCompat.Builder(this, CHANNEL_ID)
                .SetSmallIcon(Resource.Drawable.appiconfg)
                .SetChannelId(CHANNEL_ID)
                .SetContentIntent(pendingIntent)
                .SetContentTitle("保活")
                .SetContentText(content)
                .SetOngoing(true).SetPriority(0)
                .Build();
            notification.Flags |= NotificationFlags.NoClear;
            return notification;
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            return StartCommandResult.Sticky;
        }
    }
}