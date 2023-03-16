namespace StockBridge.Dto
{
    /// <summary>
    /// Gets data from config and keep them to use auth in cars.com
    /// <br></br>
    /// With record declaration this data shouldn't allowed to change.
    /// </summary>
    /// <param name="Username">username</param>
    /// <param name="Password">password</param>
    public record CredentialsDto(string Username, string Password)
    {
    }
}