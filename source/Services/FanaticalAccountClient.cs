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
using System.Reflection;

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
        private FanaticalUserTraits userTraits;
        private static bool newEventSDK;
        private static readonly string loginUrl = "https://www.fanatical.com/en/";
        private static readonly string accountUrl = "https://www.fanatical.com/en/account";
        private static readonly string gamesUrl = "https://www.fanatical.com/api/user/keys";
        private static readonly string refreshUrl = "https://www.fanatical.com/api/user/refresh-auth";

        private static string GetLoginScript()
        {
            //This scripts waits for a Welcome Back dialog and only then returns control to Palynite when authentication is completed.
            return @"
            console.info('Waiting For Login Token');
            function startWaitingForNodeChanges(){
                //window.alert('This is injected javascript from the Playnite FanmaticalPlugin');
                console.log('Waiting for login dialog to appear');
                //it seems user is not authenticated, so navigate so proceed simulating cliks to login dialog
                var loginDialogTarget=null;
                var loginDialogStart=false;
                var signInButton=null;                
                var signInButtonClicked=false;
                // CallBack function to call when some elements in DOM change
                var callbackElementChanged = function(mutationsList, observer){
                    //window.alert('DOMSubtreeModified!');
                    //console.log('DOM element changed!');
                    signInButton=document.getElementsByClassName('sign-in-btn');
                    //signInButton = Array.from(document.getElementsByClassName('sign-in-btn')).filter(el=> el.innerText=='SIGN IN');
                    if (signInButton!=null && signInButton.length>0 && !signInButtonClicked){
                        //window.alert('signInButton:'+signInButton[0].className);
                        signInButtonClicked=true;
                        signInButton[0].click();
                        //return;
                    }
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
                    observer.observe(document.body, config);//getRootNode()
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
                            " + (newEventSDK ? "CefSharp.PostMessage(localStorage.getItem('bsauth'))" : "window.location.href = '" + accountUrl + "'; ") +
                            @"
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
            startWaitingForNodeChanges();
            // This to open side bar
            document.body.appendChild(document.createComment('This is under playnitecontrol now'));
            //document.getElementsByClassName('mobile-nav-button')[0].click();
        ";
        }

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

            Assembly assembly = typeof(IWebView).Assembly;
            newEventSDK = Array.Exists(assembly.GetExportedTypes(), ty => ty.Name == "WebViewJavascriptMessageReceivedEventArgs");
            //newEventSDK = false;//uncommento to force to test the old way

            logger.Info("Current " + assembly.GetName() + (newEventSDK ? " supports" : " does not support") + " Javascript Event Messaging. Using " + (newEventSDK ? "new otptimized " : "old way unoptimized") + " strategy");
        }

        public void Login()
        {
            File.Delete(tokensPath); //from now on User is not considered already authenticated/authorized
                                     //FileSystem.DeleteFile(tokensPath);

            //TODO use if async
            //string authToken = AskForlogin().GetAwaiter().GetResult

            string authToken = newEventSDK? AskForlogin() : AskForlogin_old();

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


        //Using CEF events you can have a more clean exeperience/implementation
        public string AskForlogin()
        {
            using (var webView = api.WebViews.CreateView(500, 800))
            {
                //var loadComplete = new AutoResetEvent(false);
                var processingLogin = false;
                var processingMessage = false;
                JavaScriptEvaluationResult res = null;
                string token = "{ \"athenticated\" : false}";
                webView.DeleteDomainCookies(".fanatical.com");


                //Only used when javascrievents can be passed by Browser to Managed code 
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

                            logger.Debug("Fanatical Site is ready to run login script on try " + numberOfTries.ToString());
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

                            res = await webView.EvaluateScriptAsync(GetLoginScript());//waitforreallogin

                            if (!res.Success)
                            {
                                logger.Warn("LoginScript Failed to manage login dialog");
                                //  throw new JavascriptException("LoginScriptFailed");
                            }
                            break; //scripts successfully launched
                        }
                        logger.Info("LoginScript executed on event LoadingChanged after waiting ExecutingJavascript " + numberOfTries.ToString() + " times");
                        //processingLogin = false;

                    }

                };

                webView.Navigate(loginUrl);
                webView.OpenDialog();
                return token;//returning token written by event handlers fired by cef sharp instance (string)res.Result;
            }
        }

        public string AskForlogin_old()
        {
            using (var webView = api.WebViews.CreateView(500, 800))
            {
                //var loadComplete = new AutoResetEvent(false);
                var processingLogin = false;
                var processingAuth = false;
                JavaScriptEvaluationResult res = null;
                string token = "{ \"athenticated\" : false}";
                webView.DeleteDomainCookies(".fanatical.com");

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
                        int numberOfTries = 0;
                        while (numberOfTries < 6)
                        {
                            numberOfTries++;
                            // Don't know how to reliable tell if the data are ready because they are laoded post page load
                            if (!webView.CanExecuteJavascriptInMainFrame)
                            {
                                logger.Debug("Fanatical site not ready yet when getting to user profile URL on try " + numberOfTries.ToString());
                                await Task.Delay(1000);
                                continue;
                            }

                            logger.Debug("Fanatical Site is ready to run login script on try " + numberOfTries.ToString());
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
                            res = await webView.EvaluateScriptAsync(GetLoginScript()); //loginscript_oldSDKVersion//waitforreallogin_old

                            if (!res.Success)
                            {
                                logger.Warn("LoginScript Failed to manage login dialog");
                                // throw new JavascriptException("LoginScriptFailed");
                            }
                            break; //scripts successfully launched
                        }
                        logger.Info("LoginScript executed on event LoadingChanged after waiting ExecutingJavascript " + numberOfTries.ToString() + " times");
                        //processingLogin = false;

                    }

                    //if old sdk then wait for redirect to user account before getting authentication.
                    if (address != loginUrl && !e.IsLoading) //address == accountUrl
                    {
                        if (processingAuth)  //does not allow parallel same-event processing 
                        {
                            return;
                        }

                        processingAuth = true;
                        int numberOfTries = 0;
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
                return token;//retruning token read from the account page.
            }
        }

        public bool GetIsUserLoggedIn()
        {
            var token = getToken();

            if (token == null)
            {
                return false;
            }

            //Token validation (is not necessary)
            try
            {
                var jsonResponse = InvokeAuthenticatedRequest(refreshUrl).GetAwaiter().GetResult();
                userTraits= Serialization.FromJson<FanaticalUserTraits>(jsonResponse);
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to validation Fanatical authentication.");
                return false;
            }
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