using StockBridge.Dto;
using System.Configuration;

//Get Username and Password from config to auth in cars.com
string username = ConfigurationManager.AppSettings["username"];
string password = ConfigurationManager.AppSettings["password"];

//Initialize new record for auth purposes
var credentials = new CredentialsDto(username, password);

return;