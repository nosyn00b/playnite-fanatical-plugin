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
        private static bool oldPlayniteSdk = SdkVersions.SDKVersion <= new Version(6, 0, 0, 0);

        //This scripts waits for a Welcome Back dialog and only then returns control to Palynite when authentication is completed.
        private readonly string waitforreallogin = @"console.info('Waiting For Login Token');
                                        function startWaitingForLogin(){
                                            //window.alert('This is injected javascript from the Playnite FanmaticalPlugin');
                                            console.log('Waiting for login dialog to appear');
                                            //it seems user is not authenticated, so navigate so proceed simulating cliks to login dialog
                                            var loginDialogTarget=null;
                                            var loginDialogStart=false;
                                            // CallBack function to call when some elements in DOM change
                                            var callbackElementChanged = function(mutationsList, observer){
                                                //window.alert('DOMSubtreeModified!');
                                                //console.log('DOM element changed!');
                                                loginDialogTarget=document.getElementsByClassName('modal-dialog signup-modal login-pane')
                                                if (loginDialogTarget!=null && loginDialogTarget.length>0){
                                                    //window.alert('loginDialogTargetNode IS present');
                                                    //console.log('loginDialogTargetNode Element Was found');
                                                    loginDialogStart=true;
                                                    checkForCompletedLogin();
                                                }
                                                else{
                                                    if(loginDialogStart) {
                                                        CefSharp.PostMessage(localStorage.getItem('bsauth'));
                                                    };
                                                    //window.alert('loginDialogTargetNode NOT still open, waiting for login');
                                                    //console.log('loginDialogTargetNode NOT still open, waiting for login');
                                                    //return;
                                                }
                                            };

                                            // Observer Options (describe changes to monitor)
                                            var config = { attributes: false, childList: true, subtree: true };

                                            // Monitoring instance binded to callbackfunction (still not armed)
                                            var observer = new MutationObserver(callbackElementChanged);

                                            //function to arm the observer on SidebarNode
                                            function ArmLoginDialogObservation()
                                            {
                                                // Inizio del monitoraggio del nodo target riguardo le mutazioni configurate
                                                observer.observe(document.getRootNode(), config);
                                            }

                                            function checkForCompletedLogin()
                                            {
                                                //Only consider success login dialog
                                                if (loginDialogTarget.item(0) != null && loginDialogTarget.item(0).getElementsByClassName('auth-title').item(0).textContent=='Welcome back')
                                                {
                                                    var bsauth=JSON.parse(window.localStorage.getItem('bsauth'));
                                                    if (bsauth.authenticated){
                                                        //authentication comes after some time.
                                                        observer.disconnect();
                                                        //window.alert(window.localStorage.getItem('bsauth'));
                                                        //window.alert('Calling .net: '+localStorage.getItem('bsauth'));
                                                        //console.log('Calling .net: '+localStorage.getItem('bsauth'));
                                                        CefSharp.PostMessage(localStorage.getItem('bsauth'));
                                                        //window.location.href = 'https://www.fanatical.com/en/account'; 
                                                    }
                                                    else{
                                                        //window.alert('Still not authenticated');
                                                        //console.log('Still not authenticated);
                                                    }
                                                }
                                            }

                                            //start to track LoginDialogChanges
                                            ArmLoginDialogObservation();
                                        }
                                        startWaitingForLogin();
            ";

        //This script waits opens Sign-in Dialog
        private readonly string simpleloginscript = @"console.info('External script launched');
                                        function openSignInDialog(){
                                            //window.alert('Going to login dialog....');
                                            console.log('Going to login dialog....');
                                            // This to open side bar
                                            document.getElementsByClassName('mobile-nav-button')[0].click();
                                            //This to open login dialog
                                            document.getElementsByClassName('sign-in-btn')[0].click();
                                            //start to track sidebar DOM changes
                                        }
                                        openSignInDialog();
            ";


        //This scripts waits for login and in that case navigates to user profile url.
        private readonly string loginscript_oldSDKVersion = @"console.info('External script launched');
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
                                                    //navigate to library writing cookies.
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
        /*
        private readonly string logoutscript = @"
            if (window.localStorage.getItem('bsauth')!= null ){
                    authJson=JSON.parse(window.localStorage.getItem('bsauth'));
                    anonid=JSON.parse(window.localStorage.getItem('bsanonymous'));
                    if (authJson.authenticated){
                        let request = new XMLHttpRequest();
                        request.open('DELETE', 'https://www.fanatical.com/api/auth/logout');
                        request.setRequestHeader('accept','application/json');
                        request.setRequestHeader('authorization', authJson.token);
//                      request.setRequestHeader('anonid:', anonid.id);
                        request.send();
                    }
            }
            location.reload();
            ";
        */
        private readonly string simplelogoutscript = @"
            function logOff(){
                //window.alert('Going to login dialog....');
                console.log('Going to login dialog....');
                    // This to open side bar
                if(document.getElementsByClassName('side-bar-reveal').length==0){
                    document.getElementsByClassName('mobile-nav-button')[0].click();
                }
                if (document.getElementsByClassName('logged-in-as').length==1){
                    Array.from(document.getElementsByClassName('navbar-side-item side-dropdown nav-link')).filter(el=> el.innerText=='MY ACCOUNT')[0].click();
                    Array.from(document.getElementsByClassName('navbar-side-item collapse-links nav-link nav-link')).filter(el => el.innerText=='SIGN OUT')[0].click();
                    return true;
                }
                else{
                    if(document.getElementsByClassName('side-bar-reveal').length==1){
                        document.getElementsByClassName('mobile-nav-button')[0].click();
                    }
                    return false;
                }
            }
            logOff();
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
            File.Delete(tokensPath); //from now on User is not considered already authenticated/authorized
            //FileSystem.DeleteFile(tokensPath);

            //TODO use if async
            //string authToken = AskForlogin().GetAwaiter().GetResult
            string authToken = AskForlogin();

            if (string.IsNullOrEmpty(authToken))
            {
                logger.Error("Failed to get authorization token for fanatical account.");
                return;
            }

            try
            { 

                FileSystem.CreateDirectory(Path.GetDirectoryName(tokensPath));
                Encryption.EncryptToFile(
                    tokensPath,
                    authToken,
                    Encoding.UTF8,
                    WindowsIdentity.GetCurrent().User.Value);
            }

            catch (Exception e)
            {
                logger.Error(e, "Failed to write token.");
            }

        }


        // TODO       public async Task<string> AskForlogin()
        public string AskForlogin()
        {
            using (var webView = api.WebViews.CreateView(500, 800))
            {
                //var loadComplete = new AutoResetEvent(false);
                var processingLogin = false;
                var processingAuth = false;
                var processingMessage = false;
                JavaScriptEvaluationResult res = null;
                string token = "{ \"athenticated\" : false}";
                webView.DeleteDomainCookies(".fanatical.com");

                //only used with new Playnite SDK
                webView.JavascriptMessageReceived += async (o, e) =>
                {
                    if (processingMessage)
                    {
                        return;
                    }

                    processingMessage = true;
                    try
                    {
                        token = e.message;
                        if (Serialization.FromJson<FanaticalToken>(e.message).authenticated)
                        {
                            logger.Info("Authorization acquired, closing browser...");
                            webView.Close(); //this resturns from OpenDialog call!
                        }
                        else
                        {
                            logger.Info("Authorization not completed, closing browser...");
                            webView.Close(); //this resturns from OpenDialog call!
                        }
                    }
                    catch
                    {
                        logger.Warn("Error while parsing browser message");
                    }
                    finally
                    {
                        processingMessage = false;
                    }

                    return;

                };
                webView.LoadingChanged += async (_, e) =>
                {
                    var address = webView.GetCurrentAddress();
                    if (address == loginUrl && !e.IsLoading)
                    {
                        if (processingLogin) //does not allow parallel same-event processing
                        {
                            return;
                        }

                        processingLogin = true;
                        var numberOfTries = 0;
                        while (numberOfTries < 6)
                        {
                            numberOfTries++;
                            // Don't know how to reliable tell if the data are ready because they are laoded post page load
                            if (!webView.CanExecuteJavascriptInMainFrame)
                            {
                                logger.Warn("Fanatical site not ready yet on try " + numberOfTries.ToString());
                                await Task.Delay(1000);
                                continue;
                            }

                            /* //TODO Remove
                            if(numberOfTries ==0)
                            {
                                //rese auth status the firs time ia already authenitcated
                                res = await webView.EvaluateScriptAsync("window.localStorage.setItem('bsauth', '{\"authenticated\":false}');");
                                if (res.Success)
                                {
                                    logger.Warn("Authentication State was reset");
                                }

                            }
                            else { //from now on every change to the page chech for authentication
                                res = await webView.EvaluateScriptAsync("window.localStorage.getItem('bsauth');");//
                                if (res.Success && res.Result!=null)
                                {
                                    if (Serialization.FromJson<FanaticalToken>(res.Result.ToString()).authenticated)
                                    {
                                        token = res.Result.ToString();
                                        webView.Close(); //this resturn from OpenDialog call!
                                        break; //return if  authenticated
                                    }
                                }
                            }*/

                            logger.Debug("Fanatical Site i Ready to run login script on try " + numberOfTries.ToString());
                            res = await webView.EvaluateScriptAsync(simplelogoutscript); //log out if still logged-in
                            if (!res.Success)
                            {
                                logger.Warn("LogOut Failed");
                                    
                            }
                            else
                            {
                                if ((bool)res.Result)
                                {
                                    logger.Info("Logout not necessary (use not already logged-in)");
                                }
                                else
                                {
                                    logger.Info("User already loggged-in, logout done");

                                }

                            }


                            //Executed only once if CanExecuteJavascriptInMainFrame is true
                            if (oldPlayniteSdk)
                            {
                                res = await webView.EvaluateScriptAsync(loginscript_oldSDKVersion);
                            }
                            else
                            {
                                res = await webView.EvaluateScriptAsync(simpleloginscript);
                                res = await webView.EvaluateScriptAsync(waitforreallogin);
                            }

                            if (!res.Success)
                            {
                                logger.Warn("LoginScript Failed to manage login dialog");
                                //                                throw new JavascriptException("LoginScriptFailed");
                            }
                            break; //scripts successfully launched
                        }
                        logger.Info("LoginScript executed on event LoadingChanged after waiting ExecutingJavascript " + numberOfTries.ToString() + " times");
                        //processingLogin = false;

                    }

                    //if old sdk then wait for redirect to user account before getting authentication.
                    if (oldPlayniteSdk && address == accountUrl && !e.IsLoading)
                    {
                        if (processingAuth)  //does not allow parallel same-event processing 
                        {
                            return;
                        }

                        processingAuth = true;
                        var numberOfTries = 0;
                        while (numberOfTries < 3)
                        {
                            numberOfTries++;
                            if (!webView.CanExecuteJavascriptInMainFrame)
                            {
                                logger.Debug("Fanatical site not ready yet when getting to user profile URL on try " + numberOfTries.ToString());
                                await Task.Delay(500);
                                continue;
                            }

                            res = await webView.EvaluateScriptAsync("window.localStorage.getItem('bsauth');");
                            var strRes = (string)res.Result;
                            if (strRes.IsNullOrEmpty())
                            {
                                logger.Info("AuthScript had not success on try number " + numberOfTries.ToString());
                                await Task.Delay(500);
                                continue;
                            }
                            token = strRes;
                            webView.Close();
                            break;
                        }

                        logger.Info("AuthScript executed on event LoadingChanged after waiting ExecutingJavascript " + numberOfTries.ToString() + " times");
                        //processingAuth = false;
                    }
                };

                webView.Navigate(loginUrl);
                webView.OpenDialog();
                return token;//retruning token written by event handlers fired by cef sharp instance (string)res.Result;
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


        private string getToken()
        {
            if (File.Exists(tokensPath))
            {
                try
                {

                    var jsonStr = Serialization.FromJson<FanaticalToken>(Encryption.DecryptFromFile(
                            tokensPath,
                            Encoding.UTF8,
                            WindowsIdentity.GetCurrent().User.Value));

                    if (jsonStr.authenticated)
                    {
                        return jsonStr.token.Replace(" ", string.Empty);
                    }

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