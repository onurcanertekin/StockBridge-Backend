using CefSharp;
using CefSharp.OffScreen;
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
            await GetCarsResult(browser, _result.AllTesla.CarList);
            await GatherRandomCarData(browser, _result.AllTesla);
            await GetNotableHighlights(browser, _result.AllTesla);
        }
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
        HandleConsole.AddStatus(true, $"Car link: {selectedCar.Uri}");
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
        if (notableHighlightData.Success && notableHighlightData.Result != null)
        {
            var response = Convert.ToString(notableHighlightData.Result);

            //Remove unwanted characters from response
            string cleanedStr = Regex.Replace(response, @"\s+", " ").Trim();
            string[] words = cleanedStr.Split(' ');
            string mergedStr = string.Join(" ", words.Where(w => w != ""));
            return mergedStr;
        }
        return null;
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
        await GatherDetailedCarData(browser, aboutCars.CarDetail);
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
    private static async Task GatherDetailedCarData(ChromiumWebBrowser browser, CarDetailedDto carDetail)
    {
        short imageCount = 0;
        var getImageCount = await browser.EvaluateScriptAsync($"(function() {{ return document.querySelector('vdp-gallery').getAttribute(\"media-count\"); }})();");
        if (getImageCount.Success && getImageCount.Result != null)
            short.TryParse(Convert.ToString(getImageCount.Result), out imageCount);

        carDetail = new()
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
                var carData = Convert.ToString(singleCar);
                var car = ParseCarDataFromString(carData);
                carList.Add(car);
            }
        }
        HandleConsole.AddStatus(true, $"In First Page {carList.Count} Amount Of Car Added To List");

        //Click on second page pagination item
        var goToSecondPage = await browser.EvaluateScriptAsync("document.querySelector('[id=\"pagination-direct-link-2\"]').click()");
        HandleConsole.AddStatus(goToSecondPage.Success, $"Second Page Clicked");

        //Wait until page refresh ok
        //await WaitForPageLoadEnd(browser, onlyFrameLoad: true);
        //TODO: Thread.Sleep is a bad practice, it lock program. Will try to implement javascript setTimeout system
        Thread.Sleep(1000);
        //Use same script as page one to fetch all cars in this page.
        var allCarsInSecondPage = await browser.EvaluateScriptAsync(script);
        if (allCarsInSecondPage.Success && allCarsInSecondPage.Result != null)
        {
            foreach (var singleCar in allCarsInSecondPage.Result as List<object>)
            {
                var carData = Convert.ToString(singleCar);
                var car = ParseCarDataFromString(carData);
                carList.Add(car);
            }
        }
        HandleConsole.AddStatus(true, $"In Total Cars Amount In List Is: {carList.Count}");
    }

    /// <summary>
    /// Bad string parse coding.
    /// </summary>
    /// <remarks>TODO: will try to get datas from browser.</remarks>
    /// <param name="singleCar"></param>
    /// <returns></returns>
    private static CarMinimizedDto ParseCarDataFromString(string singleCar)
    {
        //Get image count
        short.TryParse(singleCar.Split("cars-filmstrip totalcount=\"")[1].Split("\"")[0], out short imageCount);

        //Get dealer rating
        decimal dealerRating = 0;
        if (singleCar.Split("span class=\"sds-rating__count\">").Length > 2)
            decimal.TryParse(singleCar.Split("span class=\"sds-rating__count\">")[1].Split("</span>")[0], out dealerRating);

        //Get dealer name
        string dealerName = string.Empty;
        if (singleCar.Split("<div class=\"dealer-name\">").Length > 1)
            dealerName = singleCar.Split("<div class=\"dealer-name\">")[1].Split("<strong>")[1].Split("</strong>")[0];
        else
            dealerName = singleCar.Split("<div class=\"seller-name\">")[1].Split("<strong>")[1].Split("</strong>")[0];

        //Get deal badge
        string dealBadge = string.Empty;
        if (singleCar.Split("class=\"sds-badge__label\">").Length > 1)
            dealBadge = singleCar.Split("class=\"sds-badge__label\">")[1].Split("<span ")[0];
        else
            dealBadge = "No Deal";

        //Get car element's Id attr.
        string id = singleCar.Split("id=\"")[1].Split("\"")[0];
        HandleConsole.AddStatus(true, id);

        CarMinimizedDto result = new()
        {
            Id = id,
            Uri = ConfigManager.GetUri().SiteUri + singleCar.Split("<a href=\"/")[1].Split("\"")[0],
            ImageCount = imageCount,
            IsSponsored = id.Contains("sponsored"),
            StockType = singleCar.Split("class=\"stock-type\">")[1].Split("<")[0],
            Title = singleCar.Split("h2 class=\"title\">")[1].Split("</h2>")[0],
            Mileage = singleCar.Split("<div class=\"mileage\">")[1].Split("</div>")[0],
            Price = singleCar.Split("span class=\"primary-price\">")[1].Split("</span>")[0],
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
    private static async Task WaitForPageLoadEnd(ChromiumWebBrowser browser, bool onlyFrameLoad = false)
    {
        await WaitForPageFrameLoadEnd(browser);
        if (onlyFrameLoad == false)
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