using CefSharp;
using CefSharp.OffScreen;
using Newtonsoft.Json;
using StockBridge.Dto;
using StockBridge.Entry;
using System.Text.RegularExpressions;

internal class Program
{
    private static CredentialsDto _credentials = ConfigManager.GetCredentials();
    private static ResultDto _result = new();
    private static string _searchResultUri = string.Empty;
    private static Random _rnd = new Random();

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

            ////All Tesla's without model filter
            await GetCarsResult(browser, _result.AllTesla.CarList);
            await GatherRandomCarData(browser, _result.AllTesla);
            await GetNotableHighlights(browser, _result.AllTesla);

            //Set model x filter
            await FilterForModelX(browser);

            //Only Model X Tesla's
            await GetCarsResult(browser, _result.TeslaModelX.CarList);
            await GatherRandomCarData(browser, _result.TeslaModelX);
            await GetNotableHighlights(browser, _result.TeslaModelX);
        }
        ExportResultAsJsonFile();
        HandleConsole.Exit(true, "All good. Result must be written in your desktop named 'Result.json'");
    }

    /// <summary>
    /// Export data as json
    /// </summary>
    private static void ExportResultAsJsonFile()
    {
        // Serialize the object to a JSON string
        string jsonString = JsonConvert.SerializeObject(_result);

        //Get desktop Path
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        // Write the JSON string to a file
        File.WriteAllText(Path.Combine(desktopPath, "Result.json"), jsonString);
    }

    /// <summary>
    /// Go back to search result page and click model X filter on filter section
    /// </summary>
    /// <param name="browser"></param>
    /// <returns></returns>
    private static async Task FilterForModelX(ChromiumWebBrowser browser)
    {
        //TODO: refactor this method...
        await browser.WaitForInitialLoadAsync();
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

    /// <summary>
    /// Will try to get Home Delivery and notable highlights from current car, if not found then will search all cars in list to find working one
    /// </summary>
    /// <param name="browser"></param>
    /// <param name="aboutCars"></param>
    /// <returns></returns>
    private static async Task GetNotableHighlights(ChromiumWebBrowser browser, CarListAndCarDetailWithNotableHighlights aboutCars)
    {
        var findHomeDeliveryButton = await browser.EvaluateScriptAsync($"document.querySelector('[class=\"sds-badge sds-badge--home-delivery\"]').click()");
        if (findHomeDeliveryButton.Success)
        {
            var getNotableHighlightModalDatas = await browser.EvaluateScriptAsync($"(function() {{ return document.querySelector('[class=\"sds-modal sds-modal-visible\"]').querySelector('[class=\"sds-modal__content-body\"]').textContent; }})();");
            if (getNotableHighlightModalDatas.Success && getNotableHighlightModalDatas.Result != null)
                await ParseNotableHighlightData(browser, aboutCars);
            else
                await CheckAnotherPageForHomeDelivery(browser, aboutCars);
        }
        else
            await CheckAnotherPageForHomeDelivery(browser, aboutCars);
    }

    /// <summary>
    /// Will navigate to another car page to find Home Delivery badge
    /// </summary>
    /// <param name="browser"></param>
    /// <param name="aboutCars"></param>
    /// <returns></returns>
    private static async Task CheckAnotherPageForHomeDelivery(ChromiumWebBrowser browser, CarListAndCarDetailWithNotableHighlights aboutCars)
    {
        CarMinimizedDto selectedCar = PickRandomCar(aboutCars);
        await browser.LoadUrlAsync(selectedCar.Uri);
        HandleConsole.AddStatus(false, $"Home Delivery Badge Coul'nt Found For This Car. New Car Will Be Selected.");
        HandleConsole.AddStatus(true, $"New Car Link: {selectedCar.Uri}");
        await GetNotableHighlights(browser, aboutCars);
    }

    /// <summary>
    /// Parse notable highlight data from string
    /// </summary>
    /// <param name="body"></param>
    /// <param name="notableHighlights"></param>
    /// <returns></returns>
    private static async Task ParseNotableHighlightData(ChromiumWebBrowser browser, CarListAndCarDetailWithNotableHighlights aboutCars)
    {
        aboutCars.NotableHighlights = new()
        {
            Deal = await GetPartOfNotableHighlightDatas(browser, "price-badge"),
            HomeDelivery = await GetPartOfNotableHighlightDatas(browser, "home_delivery-badge"),
            VirtualAppointments = await GetPartOfNotableHighlightDatas(browser, "virtual_appointments-badge"),
        };
        HandleConsole.AddStatus(true, $"Notable Highlights fetched");
    }

    /// <summary>
    /// Extract notable highlight data in modal popup with given selector
    /// </summary>
    /// <param name="browser"></param>
    /// <param name="selector"></param>
    /// <returns></returns>
    private static async Task<string> GetPartOfNotableHighlightDatas(ChromiumWebBrowser browser, string selector)
    {
        var notableHighlightData = await browser.EvaluateScriptAsync($"(function() {{ return document.querySelector('[class=\"sds-modal sds-modal-visible\"]').querySelector('[class=\"sds-modal__content-body\"]').querySelector('[class=\"{selector}\"]').getElementsByClassName(\"badge-description\")[0].textContent; }})();");
        HandleConsole.AddStatus(notableHighlightData.Success, $"Notable Highlights Part, Selector:{selector}");
        if (notableHighlightData.Success && notableHighlightData.Result != null)
        {
            var response = Convert.ToString(notableHighlightData.Result);
            string mergedStr = ClearUnwantedCharacters(response);
            return mergedStr;
        }
        return null;
    }

    //Clear unwanted characters such as \n or '  '(two space)
    private static string ClearUnwantedCharacters(string? response)
    {
        //Remove unwanted characters from response
        string cleanedStr = Regex.Replace(response, @"\s+", " ").Trim();
        string[] words = cleanedStr.Split(' ');
        string mergedStr = string.Join(" ", words.Where(w => w != ""));
        return mergedStr;
    }

    /// <summary>
    /// Will select a random car from current car list and navigate its uri, then will gather its data.
    /// </summary>
    /// <param name="browser"></param>
    /// <returns></returns>
    private static async Task GatherRandomCarData(ChromiumWebBrowser browser, CarListAndCarDetailWithNotableHighlights aboutCars)
    {
        CarMinimizedDto selectedCar = PickRandomCar(aboutCars);
        await browser.LoadUrlAsync(selectedCar.Uri);
        HandleConsole.AddStatus(true, $"Car link: {selectedCar.Uri}");
        await GatherDetailedCarData(browser, aboutCars);
    }

    /// <summary>
    /// Picks a random car from given car list
    /// </summary>
    /// <param name="aboutCars"></param>
    /// <returns></returns>
    private static CarMinimizedDto PickRandomCar(CarListAndCarDetailWithNotableHighlights aboutCars)
    {
        int r = _rnd.Next(aboutCars.CarList.Count);
        CarMinimizedDto selectedCar = aboutCars.CarList[r];
        return selectedCar;
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
    /// Will get datas in features section
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
    /// Will get datas in basic section
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

    /// <summary>
    /// Will get all cars result in first two page
    /// </summary>
    /// <param name="browser"></param>
    /// <returns></returns>
    private static async Task GetCarsResult(ChromiumWebBrowser browser, List<CarMinimizedDto> carList)
    {
        HandleConsole.AddStatus(true, "Firts Page Cars Trying To Get And Parse");

        carList.AddRange(await GetCurrentPagesCarResult(browser));
        HandleConsole.AddStatus(true, $"In First Page {carList.Count} Amount Of Car Added To List");

        //Click on second page pagination item
        var goToSecondPage = await browser.EvaluateScriptAsync("document.querySelector('[id=\"pagination-direct-link-2\"]').click()");
        HandleConsole.AddStatus(goToSecondPage.Success, $"Second Page Clicked");

        //Wait until page refresh ok
        //await WaitForPageLoadEnd(browser, onlyFrameLoad: true);
        //TODO: Thread.Sleep is a bad practice, it lock program. Will try to implement javascript setTimeout system
        Thread.Sleep(1000);
        //Use same script as page one to fetch all cars in this page.
        carList.AddRange(await GetCurrentPagesCarResult(browser));
        HandleConsole.AddStatus(true, $"In Total Cars Amount In List Is: {carList.Count}");
    }

    /// <summary>
    /// This method will get all cars detail in current page
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
    /// <remarks>TODO: will try to get datas from browser.</remarks>
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
            Uri = ConfigManager.GetUri().SiteUri + await GetCarListDetailBySelector(browser, singleCarId, "a[href]", true, "href"),
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
            var response = ClearUnwantedCharacters(Convert.ToString(carListDetailRequest.Result));
            return response;
        }
        return string.Empty;
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
        _searchResultUri = browser.Address;
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
        var addUsernameCredentialToElement = await browser.EvaluateScriptAsync($"document.querySelector('cars-auth-modal').querySelector(\"[id=auth-modal-email]\").value='{_credentials.Username}'");
        HandleConsole.AddStatus(addUsernameCredentialToElement.Success, $"On Add Username to Email Input");
        var addPasswordCredentialToElement = await browser.EvaluateScriptAsync($"document.querySelector('cars-auth-modal').querySelector(\"[id=auth-modal-current-password]\").value='{_credentials.Password}'");
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
}