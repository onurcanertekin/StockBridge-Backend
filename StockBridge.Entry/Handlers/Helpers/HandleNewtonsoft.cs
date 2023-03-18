using Newtonsoft.Json;
using StockBridge.Dto;

namespace StockBridge.Entry.Handlers.Helpers
{
    /// <summary>
    /// handle newtonsoft.
    /// </summary>
    public static class HandleNewtonsoft
    {
        /// <summary>
        /// Export data as json
        /// </summary>
        public static void ExportResultAsJsonFile(ResultDto result)
        {
            // Serialize the object to a JSON string
            string jsonString = JsonConvert.SerializeObject(result);

            //Get desktop Path
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            // Write the JSON string to a file
            File.WriteAllText(Path.Combine(desktopPath, "Result.json"), jsonString);
        }
    }
}