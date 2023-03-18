using CefSharp;
using CefSharp.OffScreen;

namespace StockBridge.Entry.Handlers.Helpers
{
    /// <summary>
    /// handle cef initialize.
    /// </summary>
    public static class HandleCefInitialize
    {
        /// <summary>
        /// Try to initialize Cef
        /// </summary>
        public static void InitializeCef()
        {
            var cefSettings = new CefSettings();
            var initializeCefCheck = Cef.Initialize(cefSettings);
            if (initializeCefCheck == false)
            {
                HandleConsole.Exit(false, "Cef couldn't initialized.");
            }
            HandleConsole.AddStatus(true, "Cef succesfully initialized.");
        }
    }
}