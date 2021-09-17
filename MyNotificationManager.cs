using Android.App;
using Android.Content;
using Android.Icu.Util;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VUTPortalConfirmator
{
    public static class MyNotificationManager
    {
        static string name = "Periodické oznámení";
        static String description = "Připomíná uživateli potvrzení o bezinfekčnosti na základě jeho vlastního nastavení.";
        static String channelID = "CHANNEL_REMINDER";
        public static void TryShowingNotification(Context context, MySettings settings)
        {
            if (settings.LastAppResetTime.Date == DateTime.Now.Date)
            {
                //Resets the "dont show notification today" flag to "you can show the notification today"
                settings.LastAppResetTime = DateTime.Now.AddDays(-2);
                settings.Save(context);
                return;
            }
            if (settings.EnableNotifications)
            {
                DateTime today = DateTime.Now;
                var day = today.DayOfWeek;
                MyDayOfWeek enabledDays = settings.EnabledDays;

                if (day == DayOfWeek.Monday && enabledDays.HasFlag(MyDayOfWeek.Monday))
                    ShowNotification(context);
                else if (day == DayOfWeek.Tuesday && enabledDays.HasFlag(MyDayOfWeek.Tuesday))
                    ShowNotification(context);
                else if (day == DayOfWeek.Wednesday && enabledDays.HasFlag(MyDayOfWeek.Wednesday))
                    ShowNotification(context);
                else if (day == DayOfWeek.Thursday && enabledDays.HasFlag(MyDayOfWeek.Thursday))
                    ShowNotification(context);
                else if (day == DayOfWeek.Friday && enabledDays.HasFlag(MyDayOfWeek.Friday))
                    ShowNotification(context);

            }
        }
        public static void ShowNotification(Context context)
        {
            createNotificationChannel(context);

            var builder = new NotificationCompat.Builder(context, channelID);
            builder.SetContentTitle("Potvrzení bezinfekčnosti")
            .SetAutoCancel(true)
            .SetContentText("Cítíte se dnes bez-covidově?")
            .SetSmallIcon(Resource.Drawable.ic_stat_vutconfirmatornotificationicon);

            Intent notifyIntent = new Intent(context, typeof(MainActivity));
            Bundle bundle = new Bundle();
            bundle.PutBoolean("confirmForm", true);
            bundle.PutLong("notifTime", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            notifyIntent.PutExtras(bundle);
            PendingIntent pendingIntent = PendingIntent.GetActivity(context, 2, notifyIntent, PendingIntentFlags.UpdateCurrent);
            //to be able to launch your activity from the notification 
            builder.AddAction(Resource.Drawable.ic_mtrl_chip_checked_black, "Potvrdit", pendingIntent);

            var built = builder.Build();

            NotificationManager mc = (NotificationManager)context.GetSystemService(Context.NotificationService);
            mc.Notify(1, built);
        }
        public static void CancelNotification(Context context)
        {
            NotificationManager mc = (NotificationManager)context.GetSystemService(Context.NotificationService);
            mc.Cancel(1);
        }
        private static void createNotificationChannel(Context context)
        {
            // Code yoinked from official android docs https://developer.android.com/training/notify-user/build-notification
            // Create the NotificationChannel, but only on API 26+ because
            // the NotificationChannel class is new and not in the support library

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                //Anyway, what is this android 8 trash wtfff

                NotificationImportance importance = NotificationImportance.Default;
                NotificationChannel channel = new NotificationChannel(channelID, new Java.Lang.String(name), importance);
                channel.Description = description;
                // Register the channel with the system; you can't change the importance
                // or other notification behaviors after this
                NotificationManager mc = (NotificationManager)context.GetSystemService(Context.NotificationService);
                mc.CreateNotificationChannel(channel);
                Log.Info("[VUTConf] MyNotificationManager", "Notification channel created");
            }
        }

        public static void UpdateNotificationRegistration(Context context, MySettings settings)
        {
            if(settings.EnableNotifications)
            {
                var now = DateTime.Now;
                var showTime = settings.NotificationTime;
                DateTime st = DateTime.Today;
                st = st.AddHours(showTime.Hour).AddMinutes(showTime.Minute);
                if(now>st)
                {
                    //Set a "dont show notification today" flag
                    settings.LastAppResetTime = DateTime.Now;
                    settings.Save(context);
                    Log.Info("[VUTConf] MyNotificationManager", "NoShow flag set for "+settings.LastAppResetTime);
                }
                else
                {
                    //Doesnt set a "dont show notification today" flag
                    settings.LastAppResetTime = DateTime.Now.AddDays(-2);
                    settings.Save(context);
                }
                Log.Info("[VUTConf] MyNotificationManager", "Registering notifications");
                RegisterPeriodicNotifications(context, settings.NotificationTime.Hour, settings.NotificationTime.Minute);
            }
            else
            {
                Log.Info("[VUTConf] MyNotificationManager", "Unregistering notifications");
                CancelPeriodicNotifications(context);
            }
        }
        public static bool IsNotificationRegistered(Context context)
        {
            var pendingIntent = CreatePendingIntent(context, PendingIntentFlags.NoCreate);
            Log.Info("[VUTConf] MyNotificationManager", "IsRegistered: " + (pendingIntent != null));
            return pendingIntent != null;
        }

        private static void RegisterPeriodicNotifications(Context context, int hour, int minute)
        {

            Calendar calendar = Calendar.GetInstance(ULocale.Germany);
            calendar.Set(CalendarField.HourOfDay, hour);
            calendar.Set(CalendarField.Minute, minute);
            calendar.Set(CalendarField.Second, 0);

            AlarmManager alarmManager = (AlarmManager)context.GetSystemService(Context.AlarmService);
            alarmManager.SetRepeating(AlarmType.Rtc, calendar.TimeInMillis, AlarmManager.IntervalDay, CreatePendingIntent(context, PendingIntentFlags.UpdateCurrent));
        }
        private static void CancelPeriodicNotifications(Context context)
        {
            var pendingIntent = CreatePendingIntent(context, PendingIntentFlags.UpdateCurrent);
            AlarmManager alarmManager = (AlarmManager)context.GetSystemService(Context.AlarmService);
            alarmManager.Cancel(pendingIntent);
            pendingIntent.Cancel();
        }
        private static PendingIntent CreatePendingIntent(Context context, PendingIntentFlags flags)
        {
            Intent notifyIntent = new Intent(context, typeof(AlarmReceiver));
            PendingIntent pendingIntent = PendingIntent.GetBroadcast
                        (context, 0, notifyIntent, flags);
            return pendingIntent;
        }
    }
}