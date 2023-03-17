using CefSharp;
using CefSharp.OffScreen;
using StockBridge.Dto;
using StockBridge.Entry;

internal class Program
{
    private static CredentialsDto credentials = ConfigManager.GetCredentials();

    private static async Task Main(string[] args)
    {
        //Get configs
        SiteUriDto uri = ConfigManager.GetUri();

        //initialize check for cef
        var cefSettings = new CefSettings();
        var initializeCefCheck = Cef.Initialize(cefSettings);
        if (initializeCefCheck == false)
        {
            HandleConsole.Exit(false, "Cef couldn't initialized.");
        }
        HandleConsole.AddStatus(true, "Cef succesfully initialized.");

        //Create Chromium instance with given uri
        using (var browser = new ChromiumWebBrowser(uri.SiteUri))
        {
            //Try to reach site
            var initialLoadResponse = await browser.WaitForInitialLoadAsync();
            if (initialLoadResponse.Success == false)
            {
                HandleConsole.Exit(false, "Couldn't reach to site");
            }
            HandleConsole.AddStatus(true, "Site initial load is successfull");

            await LoginToSite(browser);
        }
    }

    private static async Task LoginToSite(ChromiumWebBrowser browser)
    {
        var topbarMenuButtonElement = await browser.EvaluateScriptAsync("document.getElementsByClassName(\"nav-user-menu-button\")[0].click()");
        HandleConsole.AddStatus(topbarMenuButtonElement.Success, $"On Click Menu Button");

        var signInButtonElement = await browser.EvaluateScriptAsync("document.querySelector('cars-global-header').shadowRoot.querySelector('spark-button').click()");
        HandleConsole.AddStatus(signInButtonElement.Success, $"On Click Sign In Button");

        var addUsernameCredentialToElement = await browser.EvaluateScriptAsync($"document.querySelector('cars-auth-modal').querySelector(\"[id=auth-modal-email]\").value='{credentials.Username}'");
        HandleConsole.AddStatus(addUsernameCredentialToElement.Success, $"On Add Username to Email Input");
        var addPasswordCredentialToElement = await browser.EvaluateScriptAsync($"document.querySelector('cars-auth-modal').querySelector(\"[id=auth-modal-current-password]\").value='{credentials.Password}'");
        HandleConsole.AddStatus(addPasswordCredentialToElement.Success, $"On Add Password to Email Input");

        var signInModalSignInButtonElement = await browser.EvaluateScriptAsync("document.querySelector('cars-auth-modal').shadowRoot.querySelector('spark-button').click()");
        HandleConsole.AddStatus(signInModalSignInButtonElement.Success, $"On Click Sign In Button In Sign In Modal");

        await WaitForPageReloadEnd(browser);

        var checkIsLoginStatePersist = await browser.EvaluateScriptAsync("document.querySelector('cars-global-header').shadowRoot.querySelector('[class=nav-user-name]').innerText");
        if (checkIsLoginStatePersist.Success)
        {
            if ((checkIsLoginStatePersist.Result as string).Contains("Hi,"))
                HandleConsole.AddStatus(true, $"Login State Persist, Can Continue");
            else
                HandleConsole.AddStatus(false, $"Login State Does Not Persist, Lost Current State");
        }
        else
            HandleConsole.AddStatus(false, $"Login State Does Not Persist, Lost Current State");
    }

    /// <summary>
    /// Wait for the page to finish reloading
    /// </summary>
    /// <param name="browser"></param>
    /// <returns></returns>
    private static async Task WaitForPageReloadEnd(ChromiumWebBrowser browser)
    {
        var frameLoadTaskCompletionSource = new TaskCompletionSource<bool>();
        void FrameLoadEndHandler(object sender, FrameLoadEndEventArgs e)
        {
            if (e.Frame.IsMain)
            {
                frameLoadTaskCompletionSource.TrySetResult(true);
                browser.FrameLoadEnd -= FrameLoadEndHandler;
            }
        }
        browser.FrameLoadEnd += FrameLoadEndHandler;
        await frameLoadTaskCompletionSource.Task;
    }

    /// <summary>
    /// Retrive both cookies and local storage datas
    /// </summary>
    /// <param name="browser"></param>
    /// <returns></returns>
    private static async Task SaveCookiesAndLocalStorage(ChromiumWebBrowser browser)
    {
        var cookieManager = browser.GetCookieManager();
        var cookieList = await cookieManager.VisitAllCookiesAsync();
        var localStorage = await browser.GetMainFrame().EvaluateScriptAsync("(function() { return window.localStorage; })();");
        if (localStorage.Success)
            foreach (var item in localStorage.Result as System.Dynamic.ExpandoObject)
            {
            }
    }
}