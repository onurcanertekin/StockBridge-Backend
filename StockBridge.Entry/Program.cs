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
            await SelectFiltersAndClickSearchButton(browser);
        }
    }

    private static async Task SelectFiltersAndClickSearchButton(ChromiumWebBrowser browser)
    {
        //select from select/option element
        await SelectFilterOptions(browser, "stock-type", "used", "New/Used");
        await SelectFilterOptions(browser, "make", "tesla", "Make");
        await SelectFilterOptions(browser, "price", "100000", "Price");
        await SelectFilterOptions(browser, "distance", "all", "Distance");

        //set text of zip input
        var selectFilterResponse = await browser.EvaluateScriptAsync("document.querySelector('[data-activitykey=zip]').value=94596");
        HandleConsole.AddStatus(selectFilterResponse.Success, $"On Select Zip Option Selected 94596");

        //click search button on filter section
        var searchButtonElement = await browser.EvaluateScriptAsync("document.querySelector('[data-searchtype=\"make\"]').click()");
        HandleConsole.AddStatus(searchButtonElement.Success, $"On Search Button Click");

        //wait until page reload
        await WaitForPageReloadEnd(browser);
        HandleConsole.AddStatus(true, $"Filters selected and result page fetched");
    }

    /// <summary>
    /// Will select from select/option input
    /// </summary>
    /// <param name="browser"></param>
    /// <param name="dataActivityKey">to get specific querySelector</param>
    /// <param name="mustSelect">which data should be picked</param>
    /// <param name="optionName">to show on console</param>
    /// <returns></returns>
    private static async Task SelectFilterOptions(ChromiumWebBrowser browser, string dataActivityKey, string mustSelect, string optionName)
    {
        var selectFilterResponse = await browser.EvaluateScriptAsync(
            $@"const options_{mustSelect} = document.querySelector('[data-activitykey=""{dataActivityKey}""]').options;
            for (let i = 0; i < options_{mustSelect}.length; i++) {{
              if (options_{mustSelect}[i].value === '{mustSelect}') {{
                  options_{mustSelect}[i].selected = true;
                  break;
              }}
            }}");
        HandleConsole.AddStatus(selectFilterResponse.Success, $"On Select {optionName} Option Selected {mustSelect}");
    }

    /// <summary>
    /// Will login to site step by step using credentials readed from config.
    /// </summary>
    /// <param name="browser"></param>
    /// <returns></returns>
    private static async Task LoginToSite(ChromiumWebBrowser browser)
    {
        //Click top right Menu button
        var topbarMenuButtonElement = await browser.EvaluateScriptAsync("document.getElementsByClassName(\"nav-user-menu-button\")[0].click()");
        HandleConsole.AddStatus(topbarMenuButtonElement.Success, $"On Click Menu Button");

        //Click sign in in Menu button's modal popup
        var signInButtonElement = await browser.EvaluateScriptAsync("document.querySelector('cars-global-header').shadowRoot.querySelector('spark-button').click()");
        HandleConsole.AddStatus(signInButtonElement.Success, $"On Click Sign In Button");

        //Input user credentials to opened popup
        var addUsernameCredentialToElement = await browser.EvaluateScriptAsync($"document.querySelector('cars-auth-modal').querySelector(\"[id=auth-modal-email]\").value='{credentials.Username}'");
        HandleConsole.AddStatus(addUsernameCredentialToElement.Success, $"On Add Username to Email Input");
        var addPasswordCredentialToElement = await browser.EvaluateScriptAsync($"document.querySelector('cars-auth-modal').querySelector(\"[id=auth-modal-current-password]\").value='{credentials.Password}'");
        HandleConsole.AddStatus(addPasswordCredentialToElement.Success, $"On Add Password to Email Input");

        //Click sign in button in sign in titled popup
        var signInModalSignInButtonElement = await browser.EvaluateScriptAsync("document.querySelector('cars-auth-modal').shadowRoot.querySelector('spark-button').click()");
        HandleConsole.AddStatus(signInModalSignInButtonElement.Success, $"On Click Sign In Button In Sign In Modal");

        //wait until page reload
        await WaitForPageReloadEnd(browser);

        //check if we correctly logged in
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
        //cookies
        var cookieManager = browser.GetCookieManager();
        var cookieList = await cookieManager.VisitAllCookiesAsync();

        //Local storage
        var localStorage = await browser.GetMainFrame().EvaluateScriptAsync("(function() { return window.localStorage; })();");
        if (localStorage.Success)
            foreach (var item in localStorage.Result as System.Dynamic.ExpandoObject)
            {
            }
    }
}