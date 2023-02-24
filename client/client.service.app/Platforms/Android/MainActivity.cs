using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Text;
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

            // Intent intent = new Intent(this, typeof(MyService));
            // StartService(intent);
            //1像素广播注册
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


    #region service

    /// <summary>
    /// 不同版本有差异，需分开处理 此种方法适用于音乐播放器保活，8.0以后会在通知栏显示
    /// </summary>
    [Service]
    public class ForegroundService : Service
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

          //  PendingIntent pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.Mutable);
            Notification notification = new NotificationCompat.Builder(this, "service")
                  .SetSmallIcon(Resource.Drawable.appiconfg)
                //  .SetContentIntent(pendingIntent)
                  .SetContentTitle("通知")
                  .SetContentText("通知内容")
                  .SetOngoing(true)
                  .Build();
            notification.Flags |= NotificationFlags.NoClear;
            StartForeground(SERVICE_ID, notification);

            Startup.Start();
            return StartCommandResult.Sticky;
        }
    }


    #endregion

    #region activity

    /// <summary>
    /// 用于保活的1像素activity
    /// </summary>
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class AliveActivity : Android.App.Activity
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

    /// <summary>
    /// 息屏广播监听
    /// </summary>
    public class KeepAliveReceiver : BroadcastReceiver
    {
        private static readonly string TAG = "KeepAliveReceiver";
        public override void OnReceive(Context context, Intent intent)
        {
            String action = intent.Action;
            if (TextUtils.Equals(action, Intent.ActionScreenOff))
            {
                //息屏 开启
                KeepManager.GetInstance().StartKeep(context);
            }
            else if (TextUtils.Equals(action, Intent.ActionScreenOn))
            {
                //开屏 关闭
                KeepManager.GetInstance().FinishKeep();
            }

        }
    }

    /// <summary>
    ///  1像素activity保活管理类
    /// </summary>
    public class KeepManager
    {
        private static readonly KeepManager mInstance = new KeepManager();

        private KeepAliveReceiver mKeepAliveReceiver;

        private WeakReference<Android.App.Activity> mKeepActivity;

        public KeepManager()
        {

        }

        public static KeepManager GetInstance()
        {
            return mInstance;
        }

        /// <summary>
        /// 注册 开屏 关屏 广播
        /// </summary>
        /// <param name="context"></param>
        public void RegisterKeep(Context context)
        {
            IntentFilter filter = new IntentFilter();

            filter.AddAction(Intent.ActionScreenOn);
            filter.AddAction(Intent.ActionScreenOff);


            mKeepAliveReceiver = new KeepAliveReceiver();
            context.RegisterReceiver(mKeepAliveReceiver, filter);
            //StartKeep(context);
        }

        /// <summary>
        /// 注销 广播接收者
        /// </summary>
        /// <param name="context"></param>
        public void UnregisterKeep(Context context)
        {
            if (mKeepAliveReceiver != null)
            {
                context.UnregisterReceiver(mKeepAliveReceiver);
            }
        }

        /// <summary>
        /// 开启1像素Activity
        /// </summary>
        /// <param name="context"></param>
        public void StartKeep(Context context)
        {
            Intent intent = new Intent(context, typeof(AliveActivity));
            // 结合 taskAffinity 一起使用 在指定栈中创建这个activity
            intent.SetFlags(ActivityFlags.NewTask);
            context.StartActivity(intent);
        }

        /// <summary>
        /// 关闭1像素Activity
        /// </summary>
        public void FinishKeep()
        {
            if (mKeepActivity != null)
            {
                mKeepActivity.TryGetTarget(out Activity activity);
                if (activity != null)
                {
                    activity.Finish();
                }
                mKeepActivity = null;
            }
        }

        /// <summary>
        /// 设置弱引用
        /// </summary>
        /// <param name="keep"></param>
        public void SetKeep(AliveActivity keep)
        {
            mKeepActivity = new WeakReference<Activity>(keep);
        }
    }

    #endregion
}