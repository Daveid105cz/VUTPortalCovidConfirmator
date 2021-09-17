using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VUTPortalConfirmator
{
    public class NotificationSettingsDialog : BaseSmartDialog
    {
        public NotificationSettingsDialog(Context Context, MySettings settings) : base(Context, Resource.Layout.notificationSettingsDialog)
        {
            this.settings = settings;
            PositiveButtonText = "Uložit";
            NegativeButtonText = "Zrušit";
            SetTitle("Nastavení periodické notifikace");
        }
        CheckBox enabledCheckbox;
        CheckBox cb1;
        CheckBox cb2;
        CheckBox cb3;
        CheckBox cb4;
        CheckBox cb5;
        TimePicker timePicker;
        MySettings settings;
        protected override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            enabledCheckbox = FindViewById<CheckBox>(Resource.Id.notificationEnabledCheckBox);
            cb1 = FindViewById<CheckBox>(Resource.Id.cb1);
            cb2 = FindViewById<CheckBox>(Resource.Id.cb2);
            cb3 = FindViewById<CheckBox>(Resource.Id.cb3);
            cb4 = FindViewById<CheckBox>(Resource.Id.cb4);
            cb5 = FindViewById<CheckBox>(Resource.Id.cb5);
            timePicker = FindViewById<TimePicker>(Resource.Id.timePicker1);
            timePicker.SetIs24HourView(new Java.Lang.Boolean(true));
            enabledCheckbox.CheckedChange += (s, e) => { UpdateEnableState(); };

            LoadSettings();
            UpdateEnableState();
        }
        private void UpdateEnableState()
        {
            bool isEnabled = enabledCheckbox.Checked; cb1.Enabled = cb2.Enabled = cb3.Enabled = cb4.Enabled = cb5.Enabled = timePicker.Enabled = isEnabled;
        }
        protected override void PositiveButtonClicked(object sender, DialogClickEventArgs args)
        {
            base.PositiveButtonClicked(sender, args);

            settings.EnableNotifications = enabledCheckbox.Checked;
            settings.NotificationTime = new TimeOfDay() { Hour = timePicker.Hour, Minute = timePicker.Minute };

            MyDayOfWeek output = MyDayOfWeek.None;
            if (cb1.Checked)
                output |= MyDayOfWeek.Monday;
            if (cb2.Checked)
                output |= MyDayOfWeek.Tuesday;
            if (cb3.Checked)
                output |= MyDayOfWeek.Wednesday;
            if (cb4.Checked)
                output |= MyDayOfWeek.Thursday;
            if (cb5.Checked)
                output |= MyDayOfWeek.Friday;
            settings.EnabledDays = output;

            settings.Save(this.Context);

            MyNotificationManager.UpdateNotificationRegistration(Context, settings);
        }
        private void LoadSettings()
        {
            settings.Load(this.Context);

            enabledCheckbox.Checked = settings.EnableNotifications;
            timePicker.Minute = settings.NotificationTime.Minute;
            timePicker.Hour = settings.NotificationTime.Hour;

            MyDayOfWeek setDays = settings.EnabledDays;
            cb1.Checked = setDays.HasFlag(MyDayOfWeek.Monday);
            cb2.Checked = setDays.HasFlag(MyDayOfWeek.Tuesday);
            cb3.Checked = setDays.HasFlag(MyDayOfWeek.Wednesday);
            cb4.Checked = setDays.HasFlag(MyDayOfWeek.Thursday);
            cb5.Checked = setDays.HasFlag(MyDayOfWeek.Friday);

        }
        
    }
    public class BaseSmartDialog : AlertDialog
    {
        protected int LayoutId { get; set; } = 0;
        public String PositiveButtonText { get; set; }
        public String NegativeButtonText { get; set; }
        public String NeutralButtonText { get; set; }

        public BaseSmartDialog(Context Context, int LayoutResourceId) : base(Context)
        {
            LayoutId = LayoutResourceId;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            LayoutInflater layoutInflater = LayoutInflater.From(Context);
            View view = layoutInflater.Inflate(LayoutId, null);
            SetView(view);

            if (!String.IsNullOrEmpty(PositiveButtonText))
                SetButton(-1, PositiveButtonText, PositiveButtonClicked);

            if (!String.IsNullOrEmpty(NegativeButtonText))
                SetButton(-2, NegativeButtonText, NegativeButtonClicked);

            if (!String.IsNullOrEmpty(NeutralButtonText))
                SetButton(-3, NeutralButtonText, NeutralButtonClicked);

            base.OnCreate(savedInstanceState);

            OnViewCreated(view, savedInstanceState);
        }
        protected virtual void OnViewCreated(View view, Bundle savedInstanceState)
        {

        }
        protected virtual void PositiveButtonClicked(object sender, DialogClickEventArgs args)
        {

        }
        protected virtual void NegativeButtonClicked(object sender, DialogClickEventArgs args)
        {

        }
        protected virtual void NeutralButtonClicked(object sender, DialogClickEventArgs args)
        {

        }
    }
}