using CefSharp;
using CefSharp.OffScreen;

namespace StockBridge.Entry.Handlers.Helpers
{
    /// <summary>
    /// Handler for browser to wait until current page load end
    /// </summary>
    public static class HandleLoad
    {
        /// <summary>
        /// Wait for the page to finish loading
        /// </summary>
        /// <param name="browser"></param>
        /// <returns></returns>
        public static async Task WaitForPageLoadEnd(ChromiumWebBrowser browser)
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
}