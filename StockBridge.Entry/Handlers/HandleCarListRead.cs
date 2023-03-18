using CefSharp;
using CefSharp.OffScreen;
using StockBridge.Dto;
using StockBridge.Entry.Handlers.Helpers;
using StockBridge.Handler;

namespace StockBridge.Entry.Handlers
{
    public static class HandleCarListRead
    {
        private static SiteUriDto _siteUri;

        /// <summary>
        /// Setter for _siteUri
        /// </summary>
        /// <param name="siteUri"></param>
        public static void SetSiteUri(SiteUriDto siteUri)
        {
            _siteUri = siteUri;
        }

        /// <summary>
        /// get all cars result in first two page
        /// </summary>
        /// <param name="browser"></param>
        /// <returns></returns>
        public static async Task GetCarsResult(ChromiumWebBrowser browser, List<CarMinimizedDto> carList)
        {
            HandleConsole.AddStatus(true, "Firts Page Cars Trying To Get And Parse");

            carList.AddRange(await GetCurrentPagesCarResult(browser));
            HandleConsole.AddStatus(true, $"In First Page {carList.Count} Amount Of Car Added To List");

            //Click on second page pagination item
            var goToSecondPage = await browser.EvaluateScriptAsync("document.querySelector('[id=\"pagination-direct-link-2\"]').click()");
            HandleConsole.AddStatus(goToSecondPage.Success, $"Second Page Clicked");

            //Wait until page refresh ok
            //await WaitForPageLoadEnd(browser, onlyFrameLoad: true);
            //TODO: Thread.Sleep is a bad practice, it lock program. try to implement javascript setTimeout system
            Thread.Sleep(1000);
            //Use same script as page one to fetch all cars in this page.
            carList.AddRange(await GetCurrentPagesCarResult(browser));
            HandleConsole.AddStatus(true, $"In Total Cars Amount In List Is: {carList.Count}");
        }

        /// <summary>
        /// This method get all cars detail in current page
        /// </summary>
        /// <param name="browser"></param>
        /// <param name="carList"></param>
        /// <param name="getCarIdsScript"></param>
        /// <returns></returns>
        private static async Task<List<CarMinimizedDto>> GetCurrentPagesCarResult(ChromiumWebBrowser browser)
        {
            List<CarMinimizedDto> result = new();
            var getCarIdsScript = "(function() { return Array.from(document.querySelectorAll('[data-tracking-type=\"srp-vehicle-card\"]')).map(x=>x.id) })();";
            var allCarsInSecondPage = await browser.EvaluateScriptAsync(getCarIdsScript);
            if (allCarsInSecondPage.Success && allCarsInSecondPage.Result != null)
            {
                foreach (var singleCar in allCarsInSecondPage.Result as List<object>)
                {
                    string singleCarId = Convert.ToString(singleCar);
                    HandleConsole.AddStatus(true, $"Car Id Fetched: {singleCarId}");

                    var car = await ParseCarDataFromString(browser, singleCarId);
                    result.Add(car);
                }
            }
            return result;
        }

        /// <summary>
        /// Bad string parse coding.
        /// </summary>
        /// <remarks>TODO: try to get datas from browser.</remarks>
        /// <param name="singleCarId"></param>
        /// <returns></returns>
        private static async Task<CarMinimizedDto> ParseCarDataFromString(ChromiumWebBrowser browser, string singleCarId)
        {
            //Get image count
            short.TryParse(await GetCarListDetailBySelector(browser, singleCarId, "cars-filmstrip", true, "totalcount"), out short imageCount);

            //Get dealer rating
            decimal.TryParse(await GetCarListDetailBySelector(browser, singleCarId, "span[class=\"sds-rating__count\"]"), out decimal dealerRating);

            //Get dealer name
            string dealerName = await GetCarListDetailBySelector(browser, singleCarId, "div[class=\"dealer-name\"]");
            if (string.IsNullOrWhiteSpace(dealerName))
                dealerName = await GetCarListDetailBySelector(browser, singleCarId, "div[class=\"seller-name\"]");

            //Get deal badge
            string dealBadge = await GetCarListDetailBySelector(browser, singleCarId, "[class=\"sds-badge__label\"]");
            if (string.IsNullOrWhiteSpace(dealBadge))
                dealBadge = "No Deal";

            //Get car element's Id attr.

            CarMinimizedDto result = new()
            {
                Id = singleCarId,
                Uri = _siteUri.SiteUri + await GetCarListDetailBySelector(browser, singleCarId, "a[href]", true, "href"),
                ImageCount = imageCount,
                IsSponsored = singleCarId.Contains("sponsored"),
                StockType = await GetCarListDetailBySelector(browser, singleCarId, "p[class=\"stock-type\"]"),
                Title = await GetCarListDetailBySelector(browser, singleCarId, "h2[class=\"title\"]"),
                Mileage = await GetCarListDetailBySelector(browser, singleCarId, "div[class=\"mileage\"]"),
                Price = await GetCarListDetailBySelector(browser, singleCarId, "span[class=\"primary-price\"]"),
                DealBadge = dealBadge,
                Dealer = new DealerDto()
                {
                    Name = dealerName,
                    Rating = dealerRating,
                }
            };

            return result;
        }

        /// <summary>
        /// Search given carId's using selector to get data, it can be attrbute
        /// </summary>
        /// <param name="browser"></param>
        /// <param name="carId"></param>
        /// <param name="selector"></param>
        /// <param name="isAttribute"></param>
        /// <param name="attributeName"></param>
        /// <returns></returns>
        private static async Task<string> GetCarListDetailBySelector(ChromiumWebBrowser browser, string carId, string selector, bool isAttribute = false, string attributeName = null)
        {
            var script = $"document.querySelector('div[id=\"{carId}\"]').querySelector('{selector}').";
            if (isAttribute && attributeName != null)
            {
                script += $"getAttribute(\"{attributeName}\")";
            }
            else
            {
                script += "textContent";
            }

            var carListDetailRequest = await browser.EvaluateScriptAsync($"(function() {{ return {script}; }})();");
            if (carListDetailRequest.Success && carListDetailRequest.Result != null)
            {
                var response = Convert.ToString(carListDetailRequest.Result).ClearUnwantedCharacters();
                return response;
            }
            return string.Empty;
        }
    }
}