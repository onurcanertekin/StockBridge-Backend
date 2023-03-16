namespace StockBridge.Dto
{
    /// <summary>
    /// Gets data from config and keep them to use connection for cars.com
    /// <br></br>
    /// With record declaration this data shouldn't allowed to change.
    /// </summary>
    /// <param name="SiteUri">site to connect uri</param>
    public record SiteUriDto(string SiteUri)
    {
    }
}