namespace BinanceApp.Models
{
    public class Spot
    {
        public static async Task GetHistoricalSpotOrders()
        {
            HttpClient httpClient = new HttpClient();
            Global.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Dictionary<string, string> parameters = new Dictionary<string, string>
        {
            { "timestamp", Global.timestamp.ToString() },
            { "recvWindow", Global.recvWindow},
        };

            Global.requestUrl = Utils.generateUrl("https://api.binance.com/sapi/v1/algo/spot/historicalOrders?", parameters, true);

            Global.value = await Utils.sendHttpRequest(httpClient, Global.requestUrl);
        }
    }
}