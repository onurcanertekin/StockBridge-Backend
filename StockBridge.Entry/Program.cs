using CefSharp.OffScreen;
using StockBridge.Dto;
using StockBridge.Entry;
using StockBridge.Entry.Handlers;
using StockBridge.Entry.Handlers.Helpers;

internal class Program
{
    private static ResultDto _result = new();

    private static async Task Main(string[] args)
    {
        //Get configs
        SiteUriDto siteUri = ConfigManager.GetUri();
        HandleCefInitialize.InitializeCef();

        //Create Chromium instance with given uri
        using (var browser = new ChromiumWebBrowser(siteUri.SiteUri))
        {
            //Try to reach site
            var initialLoadResponse = await browser.WaitForInitialLoadAsync();
            if (initialLoadResponse.Success == false)
            {
                HandleConsole.Exit(false, "Couldn't reach to site");
            }
            HandleConsole.AddStatus(true, "Site initial load is successfull");

            //Handle login with credentials,
            //NOTE: Do not forget to configure app.config files username and password sections
            HandleLogin.SetCredentials(ConfigManager.GetCredentials());
            await HandleLogin.LoginToSite(browser);

            //To have one random instance.
            Random rnd = new Random();
            HandleFiltering.SetRandom(rnd);
            HandlePickRandomCar.SetRandom(rnd);

            //Set site uri
            HandleCarListRead.SetSiteUri(siteUri);

            //Set filters from filter section
            await HandleFiltering.SelectFiltersAndClickSearchButton(browser);

            //All Tesla's without model filter
            await HandleCarListRead.GetCarsResult(browser, _result.AllTesla.CarList);
            await HandleSpecificCarRead.GatherRandomCarData(browser, _result.AllTesla);
            await HandleNotableHighlightRead.GetNotableHighlights(browser, _result.AllTesla);

            //Set model x filter
            await HandleFiltering.FilterForModelX(browser);

            //Only Model X Tesla's
            await HandleCarListRead.GetCarsResult(browser, _result.TeslaModelX.CarList);
            await HandleSpecificCarRead.GatherRandomCarData(browser, _result.TeslaModelX);
            await HandleNotableHighlightRead.GetNotableHighlights(browser, _result.TeslaModelX);
        }
        HandleNewtonsoft.ExportResultAsJsonFile(_result);
        HandleConsole.Exit(true, "Everyting is good. Result must be written in your desktop named 'Result.json'");
    }
}
