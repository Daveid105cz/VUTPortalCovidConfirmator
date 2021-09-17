using System;
using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using System.Net.Http;
using System.Collections.Generic;
using Android.Util;
using System.Text.RegularExpressions;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using Java.Security;
using Android.Preferences;
using Android.Content;
using System.Text;
using Android.Views.InputMethods;
using Android.Graphics;
using Android.Support.V4.App;
using Android.Text.Method;

namespace VUTPortalConfirmator
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true, 
        ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait, 
        ConfigurationChanges =Android.Content.PM.ConfigChanges.Orientation|Android.Content.PM.ConfigChanges.ScreenSize|Android.Content.PM.ConfigChanges.KeyboardHidden,
        LaunchMode =Android.Content.PM.LaunchMode.SingleInstance)]
    public class MainActivity : AppCompatActivity
    {
        Button confirmButton;
        EditText loginText;
        EditText passwordText;
        CheckBox savePasswordCheckBox;
        ProgressBar circleBar;
        ProgressBar progressBar;
        TextView statusTextView;
        RelativeLayout mainLayout;

        MySettings settings;

        protected override void OnNewIntent(Intent intent)
        {
            Intent = intent;
            CheckIntentForNotification();
            base.OnNewIntent(intent);

        }

        long lastNotifTime = 0;
        private void CheckIntentForNotification()
        {
            var bundle = Intent.Extras;
            if (bundle == null)
                return;
            bool shouldConfirm = bundle.GetBoolean("confirmForm", false);
            long notifTime = bundle.GetLong("notifTime", 0);
            if (shouldConfirm && notifTime != lastNotifTime)
            {
                lastNotifTime = notifTime;
                //Proceed with confirmation
                MyNotificationManager.CancelNotification(this);
                Confirm();
            }
        }
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            confirmButton = FindViewById<Button>(Resource.Id.confirmButton);
            loginText = FindViewById<EditText>(Resource.Id.loginEditText);
            passwordText = FindViewById<EditText>(Resource.Id.passwordEditText);
            savePasswordCheckBox = FindViewById<CheckBox>(Resource.Id.savePasswordCheckBox);
            circleBar = FindViewById<ProgressBar>(Resource.Id.circleBar);
            progressBar = FindViewById<ProgressBar>(Resource.Id.progressBar);
            statusTextView = FindViewById<TextView>(Resource.Id.statusText);
            mainLayout = FindViewById<RelativeLayout>(Resource.Id.relativeLayout1);
            TextView linkTextView = FindViewById<TextView>(Resource.Id.linkTextView);
            linkTextView.MovementMethod = LinkMovementMethod.Instance;

            FloatingActionButton notifSettingsBtn = FindViewById<FloatingActionButton>(Resource.Id.notificationSettingsFab);
            notifSettingsBtn.Click += (s, e) =>
            {
                NotificationSettingsDialog nsd = new NotificationSettingsDialog(this,settings);
                nsd.Show();
            };

            confirmButton.Click += (s, e) => { Confirm(); };


            settings = new MySettings();
            settings.Load(this);


            RecheckNotifications();
            LoadCredentials();
            CheckIntentForNotification();
        }

        private void RecheckNotifications()
        {
            if (settings.EnableNotifications &&  !MyNotificationManager.IsNotificationRegistered(this))
                MyNotificationManager.UpdateNotificationRegistration(this, settings);
        }

        private async void Confirm()
        {
            HideKeyboard();
            String login = loginText.Text;
            String password = passwordText.Text;
            bool savePsswd = savePasswordCheckBox.Checked;
            if (savePsswd)
            {
                SaveCredentials(login, password);
            }
            else
            {
                SaveCredentials(login, "");
            }
            circleBar.Visibility = ViewStates.Visible;
            statusTextView.Visibility = ViewStates.Visible;
            try
            {
                VUTPortal portal = new VUTPortal();
                portal.StateChanged += (i, s) =>
                {
                    progressBar.Progress = i;
                    statusTextView.Text = s;
                };
                var lResult = await portal.LoginAndConfirmForm(login, password);
                if (lResult.Item1 == ConfirmationResult.Success)
                {
                    MakeSnackbar(lResult.Item2, Color.ForestGreen);
                    circleBar.Visibility = ViewStates.Invisible;
                }
                else if (lResult.Item1 == ConfirmationResult.Failure)
                {
                    MakeSnackbar(lResult.Item2, Color.Red);
                    circleBar.Visibility = ViewStates.Invisible;
                    statusTextView.Visibility = ViewStates.Invisible;
                    progressBar.Progress = 0;
                }
                else
                {
                    MakeSnackbar(lResult.Item2, Color.Orange);
                    circleBar.Visibility = ViewStates.Invisible;
                    statusTextView.Visibility = ViewStates.Invisible;
                    progressBar.Progress = 0;
                }
            }
            catch (Exception ex)
            {
                circleBar.Visibility = ViewStates.Invisible;
                statusTextView.Visibility = ViewStates.Invisible;
                progressBar.Progress = 0;
                Log.Error("[VUTConf] MainActivity", ex.ToString());
                MakeSnackbar("Zkuste znovu. Kód vyhodil výjímku. To se občas stane.", Color.Orange);

            }
        }

        private void SaveCredentials(string login, string password)
        {
            settings.Load(this);
            settings.Login = login;
            settings.Password = password;
            settings.Save(this);
        }
        private void LoadCredentials()
        {
            loginText.Text = settings.Login;
            passwordText.Text = settings.Password;
            savePasswordCheckBox.Checked = settings.IsPassworBeingSaved;

        }
        private void HideKeyboard()
        {
            try
            {
                InputMethodManager imm = (InputMethodManager)GetSystemService(Context.InputMethodService);
                imm.HideSoftInputFromWindow(mainLayout.WindowToken, 0);
            }
            catch { }
        }
        private void MakeSnackbar(String text, Color backgroundColor)
        {
            var snack = Snackbar.Make(mainLayout, text, Snackbar.LengthLong)
                .SetAction(".", (Android.Views.View.IOnClickListener)null);
            snack.View.SetBackgroundColor(backgroundColor);
            snack.Show();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
	}
}
