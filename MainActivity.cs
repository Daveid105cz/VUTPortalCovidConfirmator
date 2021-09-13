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

namespace VUTPortalConfirmator
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        Button confirmButton;
        EditText loginText;
        EditText passwordText;
        CheckBox savePasswordCheckBox;
        ProgressBar circleBar;
        ProgressBar progressBar;
        TextView statusTextView;
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

            confirmButton.Click += ConfirmButton_Click;

            TryLoadCredentials();
        }

        private async void ConfirmButton_Click(object sender, EventArgs e)
        {
            loginText.ClearFocus();
            passwordText.ClearFocus();
            savePasswordCheckBox.RequestFocus();
            String login = loginText.Text;
            String password = passwordText.Text;
            bool savePsswd = savePasswordCheckBox.Checked;
            if(savePsswd)
            {
                SaveCredentials(login, password);
            }
            else
            {
                SaveCredentials(login, "");
            }
            circleBar.Visibility = ViewStates.Visible;
            statusTextView.Visibility = ViewStates.Visible;
            statusTextView.Text = "Stahování VUT stránky";
            progressBar.Progress = 1;

            try
            {
                var result = await LoginToVUT(login, password);
                if (result == null)
                {
                    View view = (View)sender;
                    Snackbar.Make(view, "Přihlášení selhalo. Nejspíš špatný heslo. Možná taky moje chyba. Who cares, já rozhodně ne. Zkus web.", Snackbar.LengthIndefinite)
                        .SetAction("pepega", (Android.Views.View.IOnClickListener)null).Show();

                    circleBar.Visibility = ViewStates.Invisible;
                    statusTextView.Visibility = ViewStates.Invisible;
                    progressBar.Progress = 0;
                }
                else
                {
                    statusTextView.Text = "Stahování potvrzovacího formuláře";
                    progressBar.Progress = 3;
                    var isConfirmed = await ConfirmCovidForm(result);
                    if(!String.IsNullOrEmpty(isConfirmed))
                    {
                        circleBar.Visibility = ViewStates.Invisible;
                        statusTextView.Text = isConfirmed;
                        progressBar.Progress = 4;
                    }
                    else
                    {
                        circleBar.Visibility = ViewStates.Invisible;
                        statusTextView.Visibility = ViewStates.Invisible;
                        progressBar.Progress = 0;
                        View view = (View)sender;
                        Snackbar.Make(view, "Nepodařilo se potvrdit formulář", Snackbar.LengthIndefinite)
                            .SetAction("pepega", (Android.Views.View.IOnClickListener)null).Show();
                    }

                }
            }
            catch(Exception ex)
            {
                circleBar.Visibility = ViewStates.Invisible;
                statusTextView.Visibility = ViewStates.Invisible;
                progressBar.Progress = 0;
                Log.Error("Chyba",ex.ToString());
                View view = (View)sender;
                Snackbar.Make(view, "Zkuste znovu. Kód vyhodil výjímku. To se občas stane.", Snackbar.LengthIndefinite)
                    .SetAction("pepega", (Android.Views.View.IOnClickListener)null).Show();
            }
        }

        private async Task<string> ConfirmCovidForm(CookieContainer cookies)
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.CookieContainer = cookies;
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            using (HttpClient confirmClient = new HttpClient(handler))
            {
                var xs_prohl_id = await FetchPageAndGetRegexMatch(confirmClient, "https://www.vut.cz/studis/student.phtml?sn=prohlaseni_studenta", @"<input type=""hidden"" id=""xs_prohlaseni__o__bezinfekcnosti__2"" name=""xs_prohlaseni__o__bezinfekcnosti__2"" value=""([A-z0-9]*?)"" \/>");

                if (xs_prohl_id == null)
                    return String.Empty;

                var values = new Dictionary<string, string>
                {
                    { "formID", "prohlaseni-o-bezinfekcnosti-2" },
                    { "xs_prohlaseni__o__bezinfekcnosti__2", xs_prohl_id },
                    { "prijezdNa24h-2","0"},
                    { "btnPodepsat-2","1" }   //change this for "podepsat" in the end
                };
                statusTextView.Text = "Posílání potvrzovacího formuláře";
                progressBar.Progress = 4;
                var content = new FormUrlEncodedContent(values);

                var response = await confirmClient.PostAsync("https://www.vut.cz/studis/student.phtml?sn=prohlaseni_studenta", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                var alertText = RegexCapture(responseContent, @"<div class=""alert-text""><div>(.*?)<\/div><\/div>");
                if (alertText == null)
                    return String.Empty;
                //var responseString = await response.Content.ReadAsStringAsync();
                return alertText;
            }
            return String.Empty;
        }
        private async Task<CookieContainer> LoginToVUT(string login, string password)
        {
            CookieContainer cookies = new CookieContainer();
            HttpClientHandler handler = new HttpClientHandler();
            handler.CookieContainer = cookies;
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            using (HttpClient loginClient = new HttpClient(handler))
            {
                var fdkey = await FetchPageAndGetRegexMatch(loginClient, "https://www.vut.cz/login/", @"<input type=""hidden"" name=""sv\[fdkey\]"" value=""(.*)"">");

                if (fdkey == null)
                    return null;
                statusTextView.Text = "Přihlašování do intraportálu";
                progressBar.Progress = 2;
                var values = new Dictionary<string, string>
                {
                    { "special_p4_form", "1" },
                    { "login_form", "1" },
                    { "sentTime",DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()},
                    {"LDAPlogin",login },
                    {"LDAPpasswd" ,password},
                    {"sv[fdkey]",fdkey }
                };

                var content = new FormUrlEncodedContent(values);

                var response = await loginClient.PostAsync("https://www.vut.cz/login/in", content);

                var cook = cookies.GetCookies(new Uri("https://www.vut.cz"));
                var isLoggedIn = cook["portal_is_logged_in"];
                if(isLoggedIn != null && isLoggedIn.Value=="1")
                {
                    //Login success
                    return cookies;
                }
                else
                {
                    //Login fail
                    return null;
                }
            }

        }
        private async Task<String> FetchPageAndGetRegexMatch(HttpClient client,String uri, String regexPattern)
        {
            var page = await client.GetAsync(uri);
            var pageContent = await page.Content.ReadAsStringAsync();

            return RegexCapture(pageContent, regexPattern);
        }
        private String RegexCapture(String text, String pattern)
        {
            var regexResult = Regex.Match(text, pattern, RegexOptions.RightToLeft);

            if (regexResult.Groups.Count != 2)
            {
                return null;
            }
            return regexResult.Groups[1].Value;
        }
        private void SaveCredentials(string login, string password)
        {
            ISharedPreferences sharedPref = PreferenceManager.GetDefaultSharedPreferences(this);
            ISharedPreferencesEditor editor = sharedPref.Edit();
            editor.PutString("login", login).PutString("password", password).Apply();
        }
        private void TryLoadCredentials()
        {

            ISharedPreferences sharedPref = PreferenceManager.GetDefaultSharedPreferences(this);
            loginText.Text = sharedPref.GetString("login", "");
            String passwd = sharedPref.GetString("password", "");
            passwordText.Text = passwd;
            if(passwd!=String.Empty)
            {
                savePasswordCheckBox.Checked = true;
            }

        }


        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
	}
}
