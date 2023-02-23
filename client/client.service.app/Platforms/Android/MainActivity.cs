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
        private PowerManager.WakeLock wakelock = null;
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            Window.SetFlags(Android.Views.WindowManagerFlags.TranslucentStatus, Android.Views.WindowManagerFlags.TranslucentStatus);
            Window.SetStatusBarColor(Android.Graphics.Color.Transparent);
            Window.SetNavigationBarColor(Android.Graphics.Color.Transparent);

            //PowerManager pm = (PowerManager)GetSystemService(PowerService);
            //wakelock = pm.NewWakeLock(WakeLockFlags.Partial | WakeLockFlags.OnAfterRelease, "mywakelock");
            //if (wakelock != null)
            //{
            //    wakelock.Acquire();
            //}

            Intent intent = new Intent(this, typeof(MyService));
            StartService(intent);
            //1像素广播注册
            // KeepManager.GetInstance().RegisterKeep(this);
            //前台服务保活
            //  StartService(new Intent(this, typeof(ForegroundService)));

            base.OnCreate(savedInstanceState);

            //VpnService service = new VpnService();
            //_ = new VpnService.Builder(service).SetHttpProxy(ProxyInfo.BuildPacProxy(Android.Net.Uri.Parse("http://127.0.0.1:5411/web/pac.pac"))); 
        }

        protected override void OnDestroy()
        {
            wakelock?.Release();
            base.OnDestroy();
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


    #region service

    /// <summary>
    /// 不同版本有差异，需分开处理 此种方法适用于音乐播放器保活，8.0以后会在通知栏显示
    /// </summary>
    [Service]
    public class ForegroundService : Service
    {
        private static readonly string TAG = "ForegroundService";
        private static readonly int SERVICE_ID = 1;

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public override void OnCreate()
        {
            base.OnCreate();

            if (Build.VERSION.SdkInt < BuildVersionCodes.JellyBeanMr2)
            {
                //4.3以下
                //将service设置成前台服务，并且不显示通知栏消息
                StartForeground(SERVICE_ID, new Notification());
            }
            else if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                //Android4.3-->Android7.0
                //将service设置成前台服务
                StartForeground(SERVICE_ID, new Notification());
                //删除通知栏消息
                //StartService(new Intent(this, typeof(InnerService)));
            }
            else
            {
                // 8.0 及以上
                //通知栏消息需要设置channel
                NotificationManager manager = (NotificationManager)GetSystemService(NotificationService);
                //NotificationManager.IMPORTANCE_MIN 通知栏消息的重要级别  最低，不让弹出
                //IMPORTANCE_MIN 前台时，在阴影区能看到，后台时 阴影区不消失，增加显示 IMPORTANCE_NONE时 一样的提示
                //IMPORTANCE_NONE app在前台没有通知显示，后台时有
                NotificationChannel channel = new NotificationChannel("channel", "xx", NotificationImportance.High);
                if (manager != null)
                {
                    manager.CreateNotificationChannel(channel);
                    Notification notification = new NotificationCompat.Builder(this, "channel").Build();
                    //将service设置成前台服务，8.x退到后台会显示通知栏消息，9.0会立刻显示通知栏消息
                    StartForeground(SERVICE_ID, notification);
                }
            }
            Startup.Start();
        }

        /// <summary>
        /// 内联服务
        /// </summary>
        public class InnerService : Service
        {
            public override void OnCreate()
            {
                base.OnCreate();
                // 让服务变成前台服务
                StartForeground(SERVICE_ID, new Notification());
                // 关闭自己
                StopSelf();
            }

            public override IBinder OnBind(Intent intent)
            {
                return null;
            }

            public override void OnDestroy()
            {
                base.OnDestroy();
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }
    }


    #endregion

    #region activity

    /// <summary>
    /// 用于保活的1像素activity
    /// </summary>
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class AliveActivity : Activity
    {
        private static readonly string TAG = "AliveActivity";
        private PowerManager.WakeLock wakelock = null;
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


            //PowerManager pm = (PowerManager)GetSystemService(PowerService);
            //wakelock = pm.NewWakeLock(WakeLockFlags.Partial | WakeLockFlags.OnAfterRelease, "mywakelock");
            //wakelock?.Acquire();
        }


        protected override void OnDestroy()
        {
            //wakelock?.Release();
            base.OnDestroy();
            //Log.Debug(TAG, "AliveActivity关闭");
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

        private WeakReference<Activity> mKeepActivity;

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