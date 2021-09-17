using Android.App;
using Android.Content;
using Android.OS;
using Android.Preferences;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VUTPortalConfirmator
{
    [BroadcastReceiver(Enabled = true, Exported = false)]
    public class AlarmReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            Log.Info("[VUTConf] AlarmReceiver", "OnReceived");
            MySettings settings = new MySettings();
            settings.Load(context);

            MyNotificationManager.TryShowingNotification(context,settings);
        }
    }
    [BroadcastReceiver(Enabled = true, Exported = false, Permission = "android.permission.RECEIVE_BOOT_COMPLETED")]
    [IntentFilter(new String[] { "android.intent.action.BOOT_COMPLETED", "android.intent.action.MY_PACKAGE_REPLACED" })]
    public class ReregisterReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            Log.Info("[VUTConf] ReregisterReceiver", "OnReceived");


            MySettings settings = new MySettings();
            settings.Load(context);
            if(settings.EnableNotifications)
            {
                Log.Info("[VUTConf] ReregisterReceiver", "Enabling notifications after app reset");

                MyNotificationManager.UpdateNotificationRegistration(context, settings);

            }

        }
    }
}