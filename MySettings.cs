using Android.App;
using Android.Content;
using Android.OS;
using Android.Preferences;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VUTPortalConfirmator
{
    public enum MyDayOfWeek
    {
        None = 0,
        Monday = 1,
        Tuesday =2,
        Wednesday=4,
        Thursday=8,
        Friday=16,
        Saturday=32,
        Sunday=64

}
    public struct TimeOfDay
    {
        public int Hour { get; set; }
        public int Minute { get; set; }

        public static implicit operator long(TimeOfDay t) => (long)t.Hour << 32 | (long)(uint)t.Minute;
        public static implicit operator TimeOfDay(long l)
        {
            int h = (int)(l >> 32);
            int m = (int)(l&uint.MaxValue);
            return new TimeOfDay() { Hour = h, Minute = m };
        }
    }
    public class MySettings
    {
        private readonly MyDayOfWeek allDaysSelected = MyDayOfWeek.Monday | MyDayOfWeek.Tuesday | MyDayOfWeek.Wednesday | MyDayOfWeek.Thursday | MyDayOfWeek.Friday;
        public String Login { get; set; }
        public String Password { get; set; }
        public bool IsPassworBeingSaved{ get { return Password != string.Empty; } }
        public bool EnableNotifications { get; set; }
        public MyDayOfWeek EnabledDays { get; set; }
        public TimeOfDay NotificationTime { get; set; }
        public DateTime LastAppResetTime { get; set; }
        
        public void Load(Context context)
        {
            ISharedPreferences sharedPref = PreferenceManager.GetDefaultSharedPreferences(context);
            Login = sharedPref.GetString("login", "");
            Password = sharedPref.GetString("password", "");
            EnableNotifications = sharedPref.GetBoolean("notificationsEnable", false);
            NotificationTime = sharedPref.GetLong("notificationsTime", new TimeOfDay() { Hour=7,Minute=0});
            EnabledDays = (MyDayOfWeek)sharedPref.GetInt("notificationsDays", (int)allDaysSelected);
            LastAppResetTime = DateTimeOffset.FromUnixTimeMilliseconds( sharedPref.GetLong("lastAppResetTime", 0)).LocalDateTime;
        }
        public void Save(Context context)
        {
            ISharedPreferences sharedPref = PreferenceManager.GetDefaultSharedPreferences(context);
            ISharedPreferencesEditor editor = sharedPref.Edit();
            editor.PutString("login", Login)
                .PutString("password", Password)
                .PutBoolean("notificationsEnable", EnableNotifications)
                .PutLong("notificationsTime", NotificationTime)
                .PutInt("notificationsDays", (int)EnabledDays)
                .PutLong("lastAppResetTime", ((DateTimeOffset)LastAppResetTime).ToUnixTimeMilliseconds());
            editor.Apply();
        }
    }
}