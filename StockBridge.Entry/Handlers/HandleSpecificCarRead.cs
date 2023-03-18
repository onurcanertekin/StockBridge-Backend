using CefSharp;
using CefSharp.OffScreen;
using StockBridge.Dto;
using StockBridge.Entry.Handlers.Helpers;
using System.Text.RegularExpressions;

namespace StockBridge.Entry.Handlers
{
    /// <summary>
    /// To gather specific car datas
    /// </summary>
    public static class HandleSpecificCarRead
    {
        /// <summary>
        /// Select a random car from current car list and navigate its uri, then gather its data.
        /// </summary>
        /// <param name="browser"></param>
        /// <returns></returns>
        public static async Task GatherRandomCarData(ChromiumWebBrowser browser, CarListAndCarDetailWithNotableHighlights aboutCars)
        {
            CarMinimizedDto selectedCar = HandlePickRandomCar.PickRandomCar(aboutCars.CarList);
            await browser.LoadUrlAsync(selectedCar.Uri);
            HandleConsole.AddStatus(true, $"Car link: {selectedCar.Uri}");
            await GatherDetailedCarData(browser, aboutCars);
        }

        /// <summary>
        /// Start extracting data
        /// </summary>
        /// <param name="browser"></param>
        /// <returns></returns>
        private static async Task GatherDetailedCarData(ChromiumWebBrowser browser, CarListAndCarDetailWithNotableHighlights aboutCars)
        {
            short imageCount = 0;
            var getImageCount = await browser.EvaluateScriptAsync($"(function() {{ return document.querySelector('vdp-gallery').getAttribute(\"media-count\"); }})();");
            if (getImageCount.Success && getImageCount.Result != null)
                short.TryParse(Convert.ToString(getImageCount.Result), out imageCount);

            aboutCars.CarDetail = new()
            {
                ImageCount = imageCount,
                Price = await GetCarDetailDataFromSelector(browser, "[class=\"primary-price\"]"),
                SellerContactPhone = await GetCarDetailDataFromSelector(browser, "[class=\"dealer-phone\"]"),
                StockType = await GetCarDetailDataFromSelector(browser, "[class=\"new-used\"]"),
                Title = await GetCarDetailDataFromSelector(browser, "[class=\"listing-title\"]"),
                EstimatedMonthlyPayment = await GetCarDetailDataFromSelector(browser, "[class=\"js-estimated-monthly-payment-formatted-value-with-abr\"]"),
                DealBadge = await GetCarDetailDataFromSelector(browser, "[class=\"sds-badge__label\"]"),
                Mileage = await GetCarDetailDataFromSelector(browser, "[class=\"listing-mileage\"]"),
                Uri = browser.Address,
                CarBasics = await GetCarBasicsData(browser),
                CarFeatures = await GetCarFeaturesData(browser),
                CarHistory = await GetCarHistoryData(browser),
            };
        }

        private static async Task<string?> GetCarDetailDataFromSelector(ChromiumWebBrowser browser, string selector)
        {
            var fetchCarDataFromSelector = await browser.EvaluateScriptAsync($"(function() {{ return document.querySelector('{selector}').textContent; }})();");
            if (fetchCarDataFromSelector.Success && fetchCarDataFromSelector.Result != null)
                return Convert.ToString(fetchCarDataFromSelector.Result);
            return null;
        }

        private static async Task<CarHistoryDto> GetCarHistoryData(ChromiumWebBrowser browser)
        {
            var fetchCarBasicInfo = await browser.EvaluateScriptAsync("(function() { return document.querySelector('section.vehicle-history-section').getElementsByTagName(\"dl\")[0].innerHTML; })();");
            HandleConsole.AddStatus(fetchCarBasicInfo.Success, $"Fetch Car History Info");
            if (fetchCarBasicInfo.Success && fetchCarBasicInfo.Result != null)
            {
                string responseString = Convert.ToString(fetchCarBasicInfo.Result);
                return new()
                {
                    AccidentsOrDamage = ExtractCarBasicOrHistoryDataFromGivenName(responseString, "Accidents or damage"),
                    FirstOwnerVehicle = ExtractCarBasicOrHistoryDataFromGivenName(responseString, "1-owner vehicle"),
                    PersonelUseOnly = ExtractCarBasicOrHistoryDataFromGivenName(responseString, "Personal use only")
                };
            }
            return new();
        }

        /// <summary>
        /// get datas in features section
        /// </summary>
        /// <param name="browser"></param>
        /// <returns></returns>
        private static async Task<CarFeaturesDto> GetCarFeaturesData(ChromiumWebBrowser browser)
        {
            var fetchCarBasicInfo = await browser.EvaluateScriptAsync("(function() { return document.querySelector('section.features-section').getElementsByTagName(\"dl\")[0].innerHTML; })();");
            HandleConsole.AddStatus(fetchCarBasicInfo.Success, $"Fetch Car Feature Info");
            if (fetchCarBasicInfo.Success && fetchCarBasicInfo.Result != null)
            {
                string responseString = Convert.ToString(fetchCarBasicInfo.Result);
                return new()
                {
                    Convenience = ExtractCarFeatureDataFromGivenName(responseString, "Convenience"),
                    Entertainment = ExtractCarFeatureDataFromGivenName(responseString, "Entertainment"),
                    Exterior = ExtractCarFeatureDataFromGivenName(responseString, "Exterior"),
                    Safety = ExtractCarFeatureDataFromGivenName(responseString, "Safety"),
                    Seating = ExtractCarFeatureDataFromGivenName(responseString, "Seating"),
                    AdditionalPopularFeatures = await ExtractCarFeatureAdditionalData(browser)
                };
            }
            return new();
        }

        /// <summary>
        /// In specific car uri, there is a this part in end of the Feature section, retrive if there is data
        /// </summary>
        /// <param name="browser"></param>
        /// <returns></returns>
        private static async Task<List<string>> ExtractCarFeatureAdditionalData(ChromiumWebBrowser browser)
        {
            var fetchCarBasicInfo = await browser.EvaluateScriptAsync("(function() { return documentdocument.getElementsByClassName('auto-corrected-feature-list')[0].textContent; })();");
            if (fetchCarBasicInfo.Success && fetchCarBasicInfo.Result != null)
            {
                return Convert.ToString(fetchCarBasicInfo.Result).Split(',').ToList();
            }
            return new();
        }

        /// <summary>
        /// Use regex to extract that part of body, create a list from ul/li
        /// </summary>
        /// <param name="stringBody"></param>
        /// <param name="detailName"></param>
        /// <returns></returns>
        private static List<string> ExtractCarFeatureDataFromGivenName(string? stringBody, string detailName)
        {
            string regexPattern = @"<dt>\s*" + Regex.Escape(detailName) + @"\s*<\/dt>\s*<dd>\s*<ul[^>]*>(.*?)<\/ul>\s*<\/dd>";
            Match match = Regex.Match(stringBody, regexPattern, RegexOptions.Singleline);

            if (match.Success)
            {
                string ulContent = match.Groups[1].Value;
                MatchCollection liMatches = Regex.Matches(ulContent, @"<li[^>]*>(.*?)<\/li>");
                List<string> liValues = liMatches.Cast<Match>().Select(m => m.Groups[1].Value.Trim()).ToList();
                return liValues;
            }
            return null;
        }

        /// <summary>
        /// get datas in basic section
        /// </summary>
        /// <param name="browser"></param>
        /// <returns></returns>
        private static async Task<CarBasicsDto> GetCarBasicsData(ChromiumWebBrowser browser)
        {
            var fetchCarBasicInfo = await browser.EvaluateScriptAsync("(function() { return document.querySelector('section.basics-section').getElementsByTagName(\"dl\")[0].innerHTML; })();");
            HandleConsole.AddStatus(fetchCarBasicInfo.Success, $"Fetch Car Basic Info");
            if (fetchCarBasicInfo.Success && fetchCarBasicInfo.Result != null)
            {
                string responseString = Convert.ToString(fetchCarBasicInfo.Result);
                return new()
                {
                    ExteriorColor = ExtractCarBasicOrHistoryDataFromGivenName(responseString, "Exterior color"),
                    InteriorColor = ExtractCarBasicOrHistoryDataFromGivenName(responseString, "Interior color"),
                    DriveTrain = ExtractCarBasicOrHistoryDataFromGivenName(responseString, "Drivetrain"),
                    FuelType = ExtractCarBasicOrHistoryDataFromGivenName(responseString, "Fuel type"),
                    Transmission = ExtractCarBasicOrHistoryDataFromGivenName(responseString, "Transmission"),
                    Engine = ExtractCarBasicOrHistoryDataFromGivenName(responseString, "Engine"),
                    VIN = ExtractCarBasicOrHistoryDataFromGivenName(responseString, "VIN"),
                    Stock = ExtractCarBasicOrHistoryDataFromGivenName(responseString, "Stock #"),
                    Mileage = ExtractCarBasicOrHistoryDataFromGivenName(responseString, "Mileage"),
                };
            }
            return new();
        }

        /// <summary>
        /// Extract data from whole string
        /// </summary>
        /// <param name="stringBody"></param>
        /// <param name="detailName"></param>
        /// <returns></returns>
        private static string ExtractCarBasicOrHistoryDataFromGivenName(string stringBody, string detailName)
        {
            var regex = new Regex(string.Format(@"<dt>{0}<\/dt>\s*<dd[^>]*>(.*?)<\/dd>", Regex.Escape(detailName)));
            var match = regex.Match(stringBody);
            if (match.Success)
            {
                string result = match.Groups[1].Value.Trim();
                return result;
            }
            return string.Empty;
        }
    }
}