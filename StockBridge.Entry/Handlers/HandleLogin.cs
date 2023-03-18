using CefSharp;
using CefSharp.OffScreen;
using StockBridge.Dto;
using StockBridge.Entry.Handlers.Helpers;

namespace StockBridge.Entry.Handlers
{
    /// <summary>
    /// Handle login operations
    /// </summary>
    public static class HandleLogin
    {
        private static CredentialsDto _credentials;

        public static void SetCredentials(CredentialsDto credentialsDto)
        {
            _credentials = credentialsDto;
        }

        /// <summary>
        /// login to site step by step using credentials readed from config.
        /// </summary>
        /// <param name="browser"></param>
        /// <returns></returns>
        public static async Task LoginToSite(ChromiumWebBrowser browser)
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
            await HandleLoad.WaitForPageLoadEnd(browser);

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
    }
}