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
            await GetCarsResult(browser);
        }
    }

    /// <summary>
    /// Will get all cars result in first two page
    /// </summary>
    /// <param name="browser"></param>
    /// <returns></returns>
    private static async Task GetCarsResult(ChromiumWebBrowser browser)
    {
        var script = "(function() { var els = document.querySelectorAll('[data-tracking-type=\"srp-vehicle-card\"]'); return Array.from(els).map(el => el.outerHTML); })();";

        //TODO: will try to retrive data as html element but until then hard coded string will help this parse...
        //var script = "(function() { var els = document.querySelectorAll('[data-tracking-type=\"srp-vehicle-card\"]'); return Array.from(els); })();";
        //var script = "(function() { return Array.from(document.querySelectorAll('[data-tracking-type=\"srp-vehicle-card\"]')); })();";
        //var script = "(function() { try { var els = document.querySelectorAll('[data-tracking-type=\"srp-vehicle-card\"]'); return Array.from(els); } catch (error) { console.error(error); return error; } })();";
        var allCarsInFirstPage = await browser.EvaluateScriptAsync(script);
        if (allCarsInFirstPage.Success && allCarsInFirstPage.Result != null)
        {
            foreach (var singleCar in allCarsInFirstPage.Result as List<object>)
            {
            }
        }
    }

    /// <summary>
    /// Will set all filters and click search button on car filter
    /// </summary>
    /// <param name="browser"></param>
    /// <returns></returns>
    private static async Task SelectFiltersAndClickSearchButton(ChromiumWebBrowser browser)
    {
        //select from select/option element
        await SelectFilterOptions(browser, "stock-type", "used", "New/Used");
        await SelectFilterOptions(browser, "model", "", "Model");
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
        await WaitForPageLoadEnd(browser);
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
    private static async Task SelectFilterOptions(ChromiumWebBrowser browser, string dataActivityKey, string mustSelect, string optionName, bool tryAgain = false)
    {
        var optionAndElementRandom = new Random().Next();
        var selectFilterResponse = await browser.EvaluateScriptAsync(
            $@"const option_{optionAndElementRandom}=document.querySelector('[data-activitykey=""{dataActivityKey}""]')
             const element_{optionAndElementRandom}= option_{optionAndElementRandom}.querySelector('option[value=""{mustSelect}""]')
             element_{optionAndElementRandom}.focus();
             element_{optionAndElementRandom}.selected=true;
             element_{optionAndElementRandom}.dispatchEvent(new Event('change', {{ bubbles: true }}))");
        HandleConsole.AddStatus(selectFilterResponse.Success, $"On {optionName} Set {mustSelect}");

        //// Wait for the select element to fire the "change" event
        await browser.WaitForSelectorAsync($"[data-activitykey=\"{dataActivityKey}\"]");
        await EnsureDataSelected(browser, dataActivityKey, mustSelect, optionName);
    }

    /// <summary>
    /// Will check if data set properly worked
    /// </summary>
    /// <param name="browser"></param>
    /// <param name="dataActivityKey"></param>
    /// <param name="mustSelect"></param>
    /// <param name="optionName"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private static async Task EnsureDataSelected(ChromiumWebBrowser browser, string dataActivityKey, string mustSelect, string optionName)
    {
        var checkFilterResponse = await browser.EvaluateScriptAsync($@"document.querySelector('[data-activitykey=""{dataActivityKey}""]').value;");
        if (checkFilterResponse.Success && checkFilterResponse.Result as string == mustSelect)
            HandleConsole.AddStatus(true, $"On Select {optionName} Option Select Is Ensured {mustSelect}");
        else
        {
            HandleConsole.AddStatus(false, $"On Select {optionName} Option Select Is Not {mustSelect}, Will Try Again");
            await SelectFilterOptions(browser, dataActivityKey, mustSelect, optionName, true);
        }
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
        await WaitForPageLoadEnd(browser);

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
    /// Wait for the page to finish loading
    /// </summary>
    /// <param name="browser"></param>
    /// <returns></returns>
    private static async Task WaitForPageLoadEnd(ChromiumWebBrowser browser)
    {
        await WaitForPageFrameLoadEnd(browser);
        await WaitForPageLoadingStateChanged(browser);
    }

    /// <summary>
    /// Will wait for browser's frame load end
    /// </summary>
    /// <param name="browser"></param>
    /// <returns></returns>
    private static async Task WaitForPageFrameLoadEnd(ChromiumWebBrowser browser)
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
    /// Will wait for browser's loading state change
    /// </summary>
    /// <param name="browser"></param>
    /// <returns></returns>
    private static async Task WaitForPageLoadingStateChanged(ChromiumWebBrowser browser)
    {
        var frameLoadTaskCompletionSource = new TaskCompletionSource<bool>();
        void LoadingStateChangeHandler(object sender, LoadingStateChangedEventArgs args)
        {
            //Wait for the Page to finish loading
            if (args.IsLoading == false)
            {
                frameLoadTaskCompletionSource.TrySetResult(true);
            }
        };
        browser.LoadingStateChanged += LoadingStateChangeHandler;
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