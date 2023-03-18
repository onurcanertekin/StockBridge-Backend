using CefSharp;
using CefSharp.OffScreen;
using StockBridge.Entry.Handlers.Helpers;

namespace StockBridge.Entry.Handlers
{
    /// <summary>
    /// Handle filtering operations
    /// </summary>
    public static class HandleFiltering
    {
        private static string _searchResultUri = string.Empty;

        private static Random _rnd;

        /// <summary>
        /// Setter for _rnd
        /// </summary>
        /// <param name="rnd"></param>
        public static void SetRandom(Random rnd)
        {
            _rnd = rnd;
        }

        /// <summary>
        /// set all filters and click search button on car filter
        /// </summary>
        /// <param name="browser"></param>
        /// <returns></returns>
        public static async Task SelectFiltersAndClickSearchButton(ChromiumWebBrowser browser)
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
            await HandleLoad.WaitForPageLoadEnd(browser);
            HandleConsole.AddStatus(true, $"Filters selected and result page fetched");
            _searchResultUri = browser.Address;
        }

        /// <summary>
        /// select from select/option input
        /// </summary>
        /// <param name="browser"></param>
        /// <param name="dataActivityKey">to get specific querySelector</param>
        /// <param name="mustSelect">which data should be picked</param>
        /// <param name="optionName">to show on console</param>
        /// <returns></returns>
        private static async Task SelectFilterOptions(ChromiumWebBrowser browser, string dataActivityKey, string mustSelect, string optionName, bool tryAgain = false)
        {
            var optionAndElementRandom = _rnd.Next();
            var selectFilterResponse = await browser.EvaluateScriptAsync(
                $@"const option_{optionAndElementRandom}=document.querySelector('[data-activitykey=""{dataActivityKey}""]')
             const element_{optionAndElementRandom}= option_{optionAndElementRandom}.querySelector('option[value=""{mustSelect}""]')
             element_{optionAndElementRandom}.focus();
             element_{optionAndElementRandom}.selected=true;
             element_{optionAndElementRandom}.dispatchEvent(new Event('change', {{ bubbles: true }}))");
            HandleConsole.AddStatus(selectFilterResponse.Success, $"On {optionName} Set {mustSelect}");

            //// Wait for the select element to fire the "change" event
            await browser.WaitForSelectorAsync($"[data-activitykey=\"{dataActivityKey}\"]");
            await EnsureDataIsSelected(browser, dataActivityKey, mustSelect, optionName);
        }

        /// <summary>
        /// check if data set properly worked
        /// </summary>
        /// <param name="browser"></param>
        /// <param name="dataActivityKey"></param>
        /// <param name="mustSelect"></param>
        /// <param name="optionName"></param>
        /// <returns></returns>
        private static async Task EnsureDataIsSelected(ChromiumWebBrowser browser, string dataActivityKey, string mustSelect, string optionName)
        {
            var checkFilterResponse = await browser.EvaluateScriptAsync($@"document.querySelector('[data-activitykey=""{dataActivityKey}""]').value;");
            if (checkFilterResponse.Success && checkFilterResponse.Result as string == mustSelect)
                HandleConsole.AddStatus(true, $"On Select {optionName} Option Select Is Ensured {mustSelect}");
            else
            {
                HandleConsole.AddStatus(false, $"On Select {optionName} Option Select Is Not {mustSelect}, Try Again");
                await SelectFilterOptions(browser, dataActivityKey, mustSelect, optionName, true);
            }
        }

        /// <summary>
        /// Go back to search result page and click model X filter on filter section
        /// </summary>
        /// <param name="browser"></param>
        /// <returns></returns>
        public static async Task FilterForModelX(ChromiumWebBrowser browser)
        {
            //TODO: refactor this method...
            await browser.LoadUrlAsync(_searchResultUri);
            await browser.WaitForInitialLoadAsync();

            //TODO: remove Thread.sleep
            Thread.Sleep(1000);

            var response = await browser.EvaluateScriptAsync($"document.querySelector('[class=\"sds-field filter refinement-simple available \"]').querySelector('input[value=\"tesla-model_x\"]').click()");
            HandleConsole.AddStatus(response.Success, $"Click Model X on Model Filter in Search Result Page");

            //TODO: remove Thread.sleep
            Thread.Sleep(1000);
            await browser.WaitForInitialLoadAsync();
        }
    }
}