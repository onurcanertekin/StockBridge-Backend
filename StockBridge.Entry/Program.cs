using CefSharp;
using CefSharp.OffScreen;
using StockBridge.Entry;

//Get configs
var credentials = ConfigManager.GetCredentials();
var uri = ConfigManager.GetUri();

//initialize check for cef
var cefSettings = new CefSettings();
var initializeCefCheck = Cef.Initialize(cefSettings);
if (initializeCefCheck == false)
{
    HandleConsole.Exit(false, "Cef couldn't initialized.");
}
HandleConsole.AddStatus(true, "Cef succesfully initialized.");