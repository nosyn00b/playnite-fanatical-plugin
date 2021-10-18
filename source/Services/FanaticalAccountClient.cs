using FanaticalLibrary.Models;
using Playnite.Common;
using Playnite.SDK;
using Playnite.SDK.Data;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FanaticalLibrary.Services
{
    /*public class TokenException : Exception
    {
        public TokenException(string message) : base(message)
        {
        }
    }*/

    public class JavascriptException : Exception
    {
        public JavascriptException(string message) : base(message)
        {
        }
    }

    public class FanaticalAccountClient
    {
        private ILogger logger = LogManager.GetLogger();
        private IPlayniteAPI api;
        private string tokensPath;
        private static readonly string loginUrl = "https://www.fanatical.com/en/";
        private static readonly string accountUrl = "https://www.fanatical.com/en/account";
        private static readonly string gamesUrl = "https://www.fanatical.com/api/user/keys";

        private readonly string loginscript = @"console.info('External script launched');
                                        function startAllThis(){
                                            //window.alert('This is injected javascript from the GOG Galaxy embedded browser (home page)');
                                            console.log('Embedded Automating Login script launched');

                                            //it seems user is not authenticated, so navigate so proceed simulating cliks to login dialog
                                            var sideBarTargetNode = document.getElementById('navbar-side');
                                            if (sideBarTargetNode != null)
                                            {
                                                //window.alert('Sidebarreference IS present');
                                                console.log('Sidebarreference Element Was found');
                                            } //Get reference to the sidebar

                                            // CallBack function to call when sidebar elements change
                                            var callbackElementChanged = function(mutationsList, observer){
                                                //window.alert('DOMSubtreeModified!');
                                                console.log('DOM element changed!');
                                                completedLogin();
                                            };

                                            // Observer Options (describe changes to monitor)
                                            var config = { attributes: true, childList: true, subtree: true };

                                            // Monitoring instance binded to callbackfunction (still not armed)
                                            var observer = new MutationObserver(callbackElementChanged);

                                            //function to arm the observer on SidebarNode
                                            function ArmSideBarChangeObservation()
                                            {
                                                // Inizio del monitoraggio del nodo target riguardo le mutazioni configurate
                                                observer.observe(sideBarTargetNode, config);
                                            }

                                            //window.alert('Navbarside has to be  operated to get to login dialog (div opening): here is the HTMLcontent of navbar-side: ' + sideBarTargetNode.innerText);
                                            //consider the login completed only if userneme element is present in the sidebar
                                            function completedLogin()
                                            {
                                                if (document.getElementsByClassName('logged-in-as').length > 0)
                                                {
                                                    observer.disconnect();
                                                    //window.alert('Elemento di login trovato: '+ document.getElementsByClassName('logged-in-as')[0].textContent);
                                                    console.log('Elemento di login trovato: ' + document.getElementsByClassName('logged-in-as')[0].textContent);
                                                    //navigate to library writing cookies and to scrape game names from the page
                                                    //window.alert(window.localStorage.getItem('bsauth'));
                                                    window.location.href = 'https://www.fanatical.com/en/account';
                                                }
                                            }

                                            //window.alert('Going to login dialog....');
                                            console.log('Going to login dialog....');
                                                // This to open side bar
                                                document.getElementsByClassName('mobile-nav-button')[0].click();
                                            //This to open login dialog
                                            document.getElementsByClassName('sign-in-btn')[0].click();
                                            //start to track sidebar DOM changes
                                            ArmSideBarChangeObservation();
                                        }
                                        startAllThis();
            ";


        private readonly string loginscriptbackup = @"console.info('External script launched');
                                        function pollDOM () {
                                          const el = document.getElementById('navbar-side');
                                          if (el!=null) {
                                            startAllThis();
                                          } else {
                                            setTimeout(pollDOM, 300); // try again in 300 milliseconds
                                          }
                                        }
                                        function startAllThis(){
                                            //window.alert('This is injected javascript from the GOG Galaxy embedded browser (home page)');
                                            console.log('Embedded Automating Login script launched');

                                            //it seems user is not authenticated, so navigate so proceed simulating cliks to login dialog
                                            var sideBarTargetNode = document.getElementById('navbar-side');
                                            if (sideBarTargetNode != null)
                                            {
                                                //window.alert('Sidebarreference IS present');
                                                console.log('Sidebarreference Element Was found');
                                            } //Get reference to the sidebar

                                            // CallBack function to call when sidebar elements change
                                            var callbackElementChanged = function(mutationsList, observer){
                                                //window.alert('DOMSubtreeModified!');
                                                console.log('DOM element changed!');
                                                completedLogin();
                                            };

                                            // Observer Options (describe changes to monitor)
                                            var config = { attributes: true, childList: true, subtree: true };

                                            // Monitoring instance binded to callbackfunction (still not armed)
                                            var observer = new MutationObserver(callbackElementChanged);

                                            //function to arm the observer on SidebarNode
                                            function ArmSideBarChangeObservation()
                                            {
                                                // Inizio del monitoraggio del nodo target riguardo le mutazioni configurate
                                                observer.observe(sideBarTargetNode, config);
                                            }

                                            //window.alert('Navbarside has to be  operated to get to login dialog (div opening): here is the HTMLcontent of navbar-side: ' + sideBarTargetNode.innerText);
                                            //consider the login completed only if userneme element is present in the sidebar
                                            function completedLogin()
                                            {
                                                if (document.getElementsByClassName('logged-in-as').length > 0)
                                                {
                                                    observer.disconnect();
                                                    //window.alert('Elemento di login trovato: '+ document.getElementsByClassName('logged-in-as')[0].textContent);
                                                    console.log('Elemento di login trovato: ' + document.getElementsByClassName('logged-in-as')[0].textContent);
                                                    //navigate to library writing cookies and to scrape game names from the page
                                                    window.location.href = 'https://www.fanatical.com/en/account';
                                                }
                                            }

                                            //window.alert('Going to login dialog....');
                                            console.log('Going to login dialog....');
                                                // This to open side bar
                                                document.getElementsByClassName('mobile-nav-button')[0].click();
                                            //This to open login dialog
                                            document.getElementsByClassName('sign-in-btn')[0].click();
                                            //start to track sidebar DOM changes
                                            ArmSideBarChangeObservation();
                                        }
                                        pollDOM();
            ";


        private static HttpClientHandler handler = new HttpClientHandler()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };

        private static HttpClient httpClient = new HttpClient(handler){};

        public FanaticalAccountClient(IPlayniteAPI api, string tokensPath)
        {
            this.api = api;
            this.tokensPath = tokensPath;
        }

        public void Login()
        {
            //var loggedIn = false;
            //var apiRedirectContent = string.Empty;
            string authToken=null;
            File.Delete(tokensPath);

            authToken = AskForlogin().GetAwaiter().GetResult();

            /*
            //TODO get token managing login web browser windows
            using (var view = api.WebViews.CreateView(580, 700))
            {
                //TODO 7navigation and authentication.
                view.DeleteDomainCookies(".fanatical.com");
                view.LoadingChanged += async (s, e) =>
                {
                    
                    var address = view.GetCurrentAddress();
                    if (view.CanExecuteJavascriptInMainFrame && !e.IsLoading)
                    {
                        if (address.StartsWith(loginUrl)  ) { 
                            var res = view.EvaluateScriptAsync("document.getElementById('navbar-side');").GetAwaiter().GetResult();
                            authToken = res.Result.ToString();
                        }
                        else { 

                            authToken =  view.EvaluateScriptAsync("window.localStorage.getItem(\"bsauth\");").GetAwaiter().GetResult().ToString();
                        }
                    }
  
                    if (authToken != null)
                    {
                        loggedIn = true;
                        view.Close();
                        return;
                    }

                    //                    if (address.StartsWith(loginUrl))
                    //                    {
                    //                        apiRedirectContent = await view.GetPageTextAsync();
                    //                        loggedIn = true;
                    //                        view.Close();
                    //                    }
                    //
                };

                view.Navigate(loginUrl);
                view.OpenDialog();

            if (!loggedIn)
            {
                return;
            }

            FileSystem.DeleteFile(tokensPath);

             //TODO Execute script to get auth token
            authToken = view.EvaluateScriptAsync("window.localStorage.getItem(\"bsauth\");").Result.ToString();
            }
            */

            if (string.IsNullOrEmpty(authToken))
            {
                logger.Error("Failed to get login token for fanatical account.");
                return;
            }

            // make as auth token 
            try
            { 

            //authToken = "{\"sentEmail\":false,\"authenticated\":true,\"email\":\"rvanzo1971 @gmail.com\",\"error\":null,\"challenge\":null,\"magicSuccess\":null,\"magicSummoned\":null,\"_id\":\"58d301b9dc0d8214008dca83\",\"role\":\"customer\",\"created\":\"2017 - 03 - 22T22: 59:05.252Z\",\"language\":{\"code\":\"en\",\"label\":\"English\",\"nativeLabel\":\"English\"},\"email_confirmed\":true,\"twoFactorEnabled\":false,\"email_newsletter\":true,\"steam\":{},\"epic\":{},\"wishlist_notifications\":true,\"cart_notifications\":true,\"review_reminders\":true,\"user_review_reminders\":true,\"date_last_email_redeem_confirm\":false,\"alreadyHasAccount\":false,\"billing\":{\"customerName\":null,\"address1\":null,\"address2\":null,\"locality\":null,\"administrativeArea\":null,\"postalCode\":null,\"countryCode\":null},\"token\":\"58d301b9dc0d8214008dca83.cca42250 - 05c5 - 4f39 - ae87 - 4b635c787a3d\"}";
            //authToken = "{\"sentEmail\":false,\"authenticated\":true,\"email\":\"rvanzo1971 @gmail.com\",\"error\":null,\"challenge\":null,\"magicSuccess\":null,\"magicSummoned\":null,\"_id\":\"58d301b9dc0d8214008dca83\",\"role\":\"customer\",\"created\":\"2017 - 03 - 22T22: 59:05.252Z\",\"language\":\"en-EN\",\"email_confirmed\":true,\"twoFactorEnabled\":false,\"email_newsletter\":true,\"steam\":\"\",\"epic\":\"\",\"wishlist_notifications\":true,\"cart_notifications\":true,\"review_reminders\":true,\"user_review_reminders\":true,\"date_last_email_redeem_confirm\":false,\"alreadyHasAccount\":false,\"billing\":\"customerName\",\"token\":\"58d301b9dc0d8214008dca83.cca42250 - 05c5 - 4f39 - ae87 - 4b635c787a3d\"}";

                FileSystem.CreateDirectory(Path.GetDirectoryName(tokensPath));
                Encryption.EncryptToFile(
                    tokensPath,
                    authToken,
                    Encoding.UTF8,
                    WindowsIdentity.GetCurrent().User.Value);
//                loggedIn = true;
            }

            catch (Exception e)
            {
                logger.Error(e, "Failed to write token.");
            }

        }


        public async Task<string> AskForlogin()
        {

            using (var webView = api.WebViews.CreateView(500,800))
            {
                //var loadComplete = new AutoResetEvent(false);
                var processingPage = false;
                JavaScriptEvaluationResult res = null;

                webView.LoadingChanged += async (_, e) =>
                {
                    var address = webView.GetCurrentAddress();
                    if (address == loginUrl && !e.IsLoading)
                    {
                        if (processingPage)
                        {
                            return;
                        }

                        processingPage = true;
                        var numberOfTries = 0;
                        while (numberOfTries < 6)
                        {
                            // Don't know how to reliable tell if the data are ready because they are laoded post page load
                            if (!webView.CanExecuteJavascriptInMainFrame)
                            {
                                logger.Warn("Fanatical site not ready yet.");
                                await Task.Delay(1000);
                                continue;
                            }

                            //res = await webView.EvaluateScriptAsync("window.alert('ciao'+document.getElementById('navbar-side'));");//window.document.getElementById('navbar-side');
                            res = await webView.EvaluateScriptAsync(loginscript);//window.document.getElementById('navbar-side');

                            //res = await webView.EvaluateScriptAsync("function returnValue(){return 'Ciao';} returnValue()");//window.document.getElementById('navbar-side'); //function () { var data=document.getElementById('navbar-side'); 
                            //res = await webView.EvaluateScriptAsync("function returnValue(){return window.localStorage.getItem('bsauth');} returnValue()");
                            //res = await webView.EvaluateScriptAsync("window.localStorage.getItem('bsauth');");
                            
                            if (!res.Success)
                            {
                                logger.Warn("LoginScript Failed when managing login dialog");
//                                throw new JavascriptException("LoginScriptFailed");
                            }

                            //loadComplete.Set();
                            //webView.Close();
                            break;
                        }
                        processingPage = false;
                    }

                    if (address == accountUrl && !e.IsLoading)
                    {
                        if (processingPage)
                        {
                            return;
                        }

                        processingPage = true;
                        var numberOfTries = 0;
                        while (numberOfTries < 3)
                        {
                            if (!webView.CanExecuteJavascriptInMainFrame)
                            {
                                logger.Warn("Fanatical site not ready yet.");
                                continue;
                            }

                            res = await webView.EvaluateScriptAsync("window.localStorage.getItem('bsauth');");

                            var strRes = (string)res.Result;
                            if (strRes.IsNullOrEmpty())
                            {
                                numberOfTries++;
                                await Task.Delay(1000);
                                continue;
                            }

                            //loadComplete.Set();
                            webView.Close();
                            break;
                        }
                        processingPage = false;
                    }
                };

                webView.Navigate(loginUrl);
                webView.OpenDialog();
                return (string)res.Result;
            }
        }




        public bool GetIsUserLoggedIn()
        {
            var token = getToken();

            if (token == null)
            {
                return false;
            }
/* //TODO Token validation on accout url
            try
            {
                var account = InvokeRequest<AccountResponse>(accountUrl + tokens.account_id, tokens).GetAwaiter().GetResult().Item2;
                return account.id == tokens.account_id;
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to validation Fanatical authentication.");
                return false;
            }
*/
           return true;
        }

        public List<FanaticalLibraryItem> GetLibraryItems()
        {
            if (!GetIsUserLoggedIn())
            {
                throw new Exception("User is not authenticated.");
            }

            var jsonResponse = InvokeAuthenticatedRequest(gamesUrl).GetAwaiter().GetResult(); 

            return Serialization.FromJson<List<FanaticalLibraryItem>>(jsonResponse); 
        }

        private async Task<string> InvokeAuthenticatedRequest(string url) 
        {
            string str;

            SetHeaders(httpClient.DefaultRequestHeaders);

            try
            {
                var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseContentRead);

                if (response.StatusCode == HttpStatusCode.OK) { 
                    str = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new Exception("HTTP request failed with error code "+ response.StatusCode);
                }


            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to get games from web.");
                return null;
            }

            return str;
        }

        private void SetHeaders(HttpRequestHeaders headers)
        {
            headers.Clear();

            headers.Accept.ParseAdd("application/json");
            headers.Add("authorization", getToken());
            headers.AcceptEncoding.ParseAdd("gzip,deflate"); 
            headers.AcceptLanguage.ParseAdd("en-GB,en-us;q=0.8,en;q=0.6");
            headers.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AcmeInc/1.0)");
            //headers.Connection.TryParseAdd("keep-alive");
            //headers.Host = new Uri(LoginUrl).Host;
            //headers.Add("X-Requested-With", "XMLHttpRequest");
        }


        private String getToken()
        {
            if (File.Exists(tokensPath))
            {
                try
                {

                    var str = Encryption.DecryptFromFile(
                            tokensPath,
                            Encoding.UTF8,
                            WindowsIdentity.GetCurrent().User.Value);

                    return Serialization.FromJson <FanaticalToken>(str).token.Replace(" ", string.Empty);
                }
                catch (Exception e)
                {
                    logger.Error(e, "Failed to load saved tokens.");
                }
            }

            return null;
        }

    }
}