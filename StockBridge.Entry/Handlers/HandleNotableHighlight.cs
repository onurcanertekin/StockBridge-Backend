using CefSharp;
using CefSharp.OffScreen;
using StockBridge.Dto;
using StockBridge.Entry.Handlers.Helpers;
using StockBridge.Handler;

namespace StockBridge.Entry.Handlers
{
    /// <summary>
    /// Handle Home Delivery Button and its content
    /// </summary>
    public static class HandleNotableHighlightRead
    {
        /// <summary>
        /// try to get Home Delivery and notable highlights from current car, if not found then search all cars in list to find working one
        /// </summary>
        /// <param name="browser"></param>
        /// <param name="aboutCars"></param>
        /// <returns></returns>
        public static async Task GetNotableHighlights(ChromiumWebBrowser browser, CarListAndCarDetailWithNotableHighlights aboutCars)
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
        /// navigate to another car page to find Home Delivery badge
        /// </summary>
        /// <param name="browser"></param>
        /// <param name="aboutCars"></param>
        /// <returns></returns>
        private static async Task CheckAnotherPageForHomeDelivery(ChromiumWebBrowser browser, CarListAndCarDetailWithNotableHighlights aboutCars)
        {
            CarMinimizedDto selectedCar = HandlePickRandomCar.PickRandomCar(aboutCars.CarList);
            await browser.LoadUrlAsync(selectedCar.Uri);
            HandleConsole.AddStatus(false, $"Home Delivery Badge Coul'nt Found For This Car. New Car Be Selected.");
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
                var response = Convert.ToString(notableHighlightData.Result).ClearUnwantedCharacters();
                return response;
            }
            return null;
        }
    }
}