namespace StockBridge.Entry.Handlers.Helpers
{
    /// <summary>
    /// handle console and show current status.
    /// </summary>
    public static class HandleConsole
    {
        /// <summary>
        /// Display current status to user
        /// </summary>
        /// <param name="success">is success or not</param>
        /// <param name="message">message to display user</param>
        public static void AddStatus(bool success, string message)
        {
            if (success)
            {
                ShowMessage(message, ConsoleColor.Green);
            }
            else
            {
                ShowMessage(message, ConsoleColor.Red);
            }
        }

        /// <summary>
        /// Exit the application with success status and message and change color of console text with
        /// </summary>
        /// <param name="success">is success or not</param>
        /// <param name="message">message to display user on exit</param>
        public static void Exit(bool success, string message)
        {
            if (success)
            {
                ShowMessage(message, ConsoleColor.Green, true);
            }
            else
            {
                ShowMessage(message, ConsoleColor.Red, true);
            }
        }

        /// <summary>
        /// add message to console by color, if its exit then wait to user's reaction to close.
        /// </summary>
        /// <param name="message">message to display user</param>
        /// <param name="foreignColor">color of current text</param>
        /// <param name="withExit">is exit? then get </param>
        private static void ShowMessage(string message, ConsoleColor foreignColor, bool withExit = false)
        {
            Console.ForegroundColor = foreignColor;
            Console.WriteLine(message);
            Console.ResetColor();
            if (withExit)
                Console.ReadLine();
        }
    }
}