using System.Text.RegularExpressions;

namespace StockBridge.Handler
{
    /// <summary>
    /// String extensions
    /// </summary>
    public static partial class HandleString
    {
        [GeneratedRegex("\\s+")]
        private static partial Regex MatchesAnyWhiteSpaceCharacter();

        /// <summary>
        /// Clear unwanted characters such as \n or '  '(two space) using regex
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static string? ClearUnwantedCharacters(this string? response)
        {
            if (response == null) return null;

            string cleanedStr = MatchesAnyWhiteSpaceCharacter().Replace(response, " ").Trim();
            string[] words = cleanedStr.Split(' ');
            string mergedStr = string.Join(" ", words.Where(w => w != ""));
            return mergedStr;
        }
    }
}