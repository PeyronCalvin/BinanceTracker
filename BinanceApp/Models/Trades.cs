using Newtonsoft.Json.Linq;

namespace BinanceApp.Models
{
    public class Trades
    {


        public static async Task<string> GetTrades(string symbol)
        {
            HttpClient httpClient = new HttpClient();
            Global.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Dictionary<string, string> parameters = new Dictionary<string, string>
        {
            { "symbol", symbol},
            { "timestamp", Global.timestamp.ToString()},

        };

            string requestUrl = Utils.generateUrl("https://api.binance.com/api/v3/myTrades?", parameters, true);
            string response = await Utils.sendHttpRequest(httpClient, requestUrl);
            return response;
        }
    }
}