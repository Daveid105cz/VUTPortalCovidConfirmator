using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VUTPortalConfirmator
{
    public enum ConfirmationResult
    {
        Success,
        Failure, 
        Exception
    }
    public enum FormDownloadResult
    {
        Success, 
        AlreadySigned, 
        Failure
    }
    public class VUTPortal
    {
        public delegate void StateChangedHandler(int state, String statusText);
        public event StateChangedHandler StateChanged;


        CookieContainer cookies;
        HttpClient client;
        public VUTPortal()
        {
            cookies = new CookieContainer();
            HttpClientHandler handler = new HttpClientHandler();
            handler.CookieContainer = cookies;
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            client = new HttpClient(handler);
        }

        public async Task<Tuple<ConfirmationResult,String>> LoginAndConfirmForm(String login, String password)
        {
            //Getting FDKey phase
            StateChanged?.Invoke(1, "Stahování VUT stránky");
            String fdKey = await GetLoginFDKey();
            if(String.IsNullOrEmpty(fdKey))
            {
                return new Tuple<ConfirmationResult, string>(ConfirmationResult.Failure, "VUT login formulář neobsahuje důležitou věc. (to je fail btw)");
            }

            //Logging into VUT intraportal
            StateChanged?.Invoke(2, "Přihlašování do intraportálu");
            bool loginResult = await TryLogin(login, password, fdKey);
            if (!loginResult)
                return new Tuple<ConfirmationResult, string>(ConfirmationResult.Failure, "Přihlášení selhalo. Nejspíš špatný heslo. Možná taky moje chyba. Who cares, já rozhodně ne. Zkus web.");

            //Getting XS whatever ID from the form
            StateChanged?.Invoke(3, "Stahování potvrzovacího formuláře");
            var frameXSIDresult = await GetConfirmationFrameXSId();
            if(frameXSIDresult.Item1==FormDownloadResult.AlreadySigned)
            {
                StateChanged?.Invoke(5, frameXSIDresult.Item2);
                return new Tuple<ConfirmationResult, string>(ConfirmationResult.Success, frameXSIDresult.Item2);
            }
            else if(frameXSIDresult.Item1 == FormDownloadResult.Failure)
            {
                return new Tuple<ConfirmationResult, string>(ConfirmationResult.Failure, "Potvrzovací formulář neobsahuje důležitou věc. (to je fail btw)");
            }

            //Posting the non-infection confirmation form
            StateChanged?.Invoke(4, "Posílání potvrzovacího formuláře");
            var confirmationResult = await SendConfirmationForm(frameXSIDresult.Item2);
            if(confirmationResult==String.Empty)
            {
                return new Tuple<ConfirmationResult, string>(ConfirmationResult.Failure, "Nepodařilo se potvrdit formulář. Zkus web.");
            }
            else
            {
                StateChanged?.Invoke(5, confirmationResult);
                return new Tuple<ConfirmationResult, string>(ConfirmationResult.Success, confirmationResult);
            }
        }
        private async Task<String> GetLoginFDKey()
        {
            var fdkey = RegexCapture(await FetchPage("https://www.vut.cz/login/"), @"<input type=""hidden"" name=""sv\[fdkey\]"" value=""(.*)"">");

            if (fdkey == null)
                return String.Empty;
            else return fdkey;
        }
        private async Task<bool> TryLogin(String login, String password, String fdkey)
        {
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

            var loginResponse = await client.PostAsync("https://www.vut.cz/login/in", content);

#if DEBUG
            var responseString = await loginResponse.Content.ReadAsStringAsync();
#endif

            var cook = cookies.GetCookies(new Uri("https://www.vut.cz"));
            var isLoggedIn = cook["portal_is_logged_in"];
            if (isLoggedIn != null && isLoggedIn.Value == "1")
            {
                //Login success
                return true;
            }
            else
            {
                //Login fail
                return false;
            }
        }
        private async Task<Tuple<FormDownloadResult, string>> GetConfirmationFrameXSId()
        {
            var formPage = await FetchPage("https://www.vut.cz/studis/student.phtml?sn=prohlaseni_studenta");
            var xs_prohl_id = RegexCapture(formPage, @"<input type=""hidden"" id=""xs_prohlaseni__o__bezinfekcnosti__2"" name=""xs_prohlaseni__o__bezinfekcnosti__2"" value=""([A-z0-9]*?)"" \/>");

            if (xs_prohl_id == null)
            {
                var potencialAlreadyConfirmedMsg = RegexCapture(formPage, @"<span class=""icon-info2""   aria-hidden=""true""><\/span><\/div><div class=""alert-text""><div>(.*?\.)<\/div>");
                if (potencialAlreadyConfirmedMsg != null)
                    return new Tuple<FormDownloadResult, string>(FormDownloadResult.AlreadySigned,potencialAlreadyConfirmedMsg);
                else
                    return new Tuple<FormDownloadResult, string>(FormDownloadResult.Failure, "");
            }
            return new Tuple<FormDownloadResult, string>(FormDownloadResult.Success, xs_prohl_id);
        }
        private async Task<String> SendConfirmationForm(String xsid)
        {
            var values = new Dictionary<string, string>
                {
                    { "formID", "prohlaseni-o-bezinfekcnosti-2" },
                    { "xs_prohlaseni__o__bezinfekcnosti__2", xsid },
                    { "prijezdNa24h-2","0"},
                    { "btnPodepsat-2","1" }   //change this for "podepsat" in the end
                };

            StateChanged?.Invoke(4, "Posílání potvrzovacího formuláře");
            var content = new FormUrlEncodedContent(values);

            var response = await client.PostAsync("https://www.vut.cz/studis/student.phtml?sn=prohlaseni_studenta", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            Log.Info("FormResponse", responseContent);
            var alertText = RegexCapture(responseContent, @"<span class=""icon-info2""   aria-hidden=""true""><\/span><\/div><div class=""alert-text""><div>(.*?\.)<\/div>");

            if (alertText == null)
                return String.Empty;
            else
                return alertText;
        }

        private async Task<string> FetchPage(string uri)
        {
            var page = await client.GetAsync(uri);
            return await page.Content.ReadAsStringAsync();
        }
        private String RegexCapture(String text, String pattern)
        {
            var regexResult = Regex.Match(text, pattern);

            if (regexResult.Groups.Count != 2)
            {
                return null;
            }
            return regexResult.Groups[1].Value;
        }
    }
}