using Newtonsoft.Json.Linq;
using ScottPlot;

namespace BinanceApp.Models
{
    public class Account
    {

        public static async Task<string> GetAccountInfo()
        {
            HttpClient httpClient = new HttpClient();
            Global.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Dictionary<string, string> parameters = new Dictionary<string, string>
        {
            { "recvWindow", Global.recvWindow},
            { "timestamp", Global.timestamp.ToString()},

        };

            string requestUrl = Utils.generateUrl("https://api.binance.com/api/v3/account?", parameters, true);
            string response = await Utils.sendHttpRequest(httpClient, requestUrl);
            JObject jsonObject = JObject.Parse(response);
            var balances = jsonObject["balances"];
            var filteredBalances = balances
                .Where(balance => (balance["free"].ToString() != "0.00000000" && balance["free"].ToString() != "0.00" && balance["free"].ToString() != "0.0") ||
                                  (balance["locked"].ToString() != "0.00000000" && balance["locked"].ToString() != "0.00" && balance["locked"].ToString() != "0.0"))
                .Select(balance => new
                {
                    Asset = balance["asset"].ToString(),
                    Free = balance["free"].ToString(),
                    Locked = balance["locked"].ToString()
                })
                .ToList();
            return String.Join(Environment.NewLine, filteredBalances.Select(b => $"Asset: {b.Asset}, Free: {b.Free}, Locked: {b.Locked}"));
        }

        public static async Task<string> GetDepositHistory()
        {
            HttpClient httpClient = new HttpClient();
            Global.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Dictionary<string, string> parameters = new Dictionary<string, string>
        {
            { "recvWindow", Global.recvWindow},
            { "timestamp", Global.timestamp.ToString()},

        };
            string requestUrl = Utils.generateUrl("https://api.binance.com/sapi/v1/localentity/deposit/history?", parameters, true);
            string response = await Utils.sendHttpRequest(httpClient, requestUrl);
            return response;
        }
        public static async Task<string> ProduceGraphAccountInfo(DateTime startTime, DateTime endTime, string interval)
        {
            Global.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            string accountInfo = await BinanceEarn.ConcatenateFlexibleAndLockedEarnAccountInfo();

            var assetPrices = BinanceEarn.ParseAssetData(accountInfo);
            Random random = new Random();
            var plt = new Plot();
            List<Kline> klineData = await Kline.RetrieveKlineCandles(assetPrices.Keys.First().ToString(), (((DateTimeOffset)startTime).ToUnixTimeMilliseconds()).ToString(), (((DateTimeOffset)endTime).ToUnixTimeMilliseconds()).ToString(), interval);
            var dates = new List<double>();
            double startDouble = startTime.ToOADate();
            double delta = Utils.ParseInterval(interval);
            for (int i = 0; i < klineData.Count; i++)
            {
                dates.Add(startDouble + i * delta);
            }
            double[] xpoints = new double[2 * klineData.Count];
            decimal[] ypoints = new decimal[2 * klineData.Count];
            for (int i = 0; i < 2 * klineData.Count(); i++)
            {
                if (i >= dates.Count()) { xpoints[i] = xpoints[xpoints.Count() - 1 - (i % xpoints.Count())]; }
                else { xpoints[i] = dates[i]; }
            }


            decimal minY = -1.0m;
            bool isMinFirstLoop = true;

            foreach (var asset in assetPrices)
            {
                klineData.Clear();
                klineData = await Kline.RetrieveKlineCandles(asset.Key.ToString(), (((DateTimeOffset)startTime).ToUnixTimeSeconds() * 1000).ToString(), (((DateTimeOffset)endTime).ToUnixTimeSeconds() * 1000).ToString(), interval);
                for (int i = 0; i < dates.Count; i++)
                {
                    decimal stackedValue = ypoints[i] + klineData[i].Low * asset.Value.ToArray()[0];
                    if (isMinFirstLoop)
                    {
                        ypoints[2 * dates.Count() - 1 - i] = stackedValue;
                    }
                    else
                    {
                        ypoints[2 * dates.Count() - 1 - i] = ypoints[i];
                    }
                    ypoints[i] = stackedValue;
                }
                if (minY == -1.0m || ypoints.Where(y => y != 0).Min() < minY)
                {
                    minY = ypoints.Where(y => y != 0).Min() * 0.98m;
                }
                if (isMinFirstLoop)
                {
                    for (int i = 0; i < dates.Count; i++)
                    {
                        ypoints[dates.Count() - 1 + i] = minY;
                    }
                    isMinFirstLoop = false;
                }
                int alpha = 255;
                int red = random.Next(256);
                int green = random.Next(256);
                int blue = random.Next(256);

                int argb = (alpha << 24) | (red << 16) | (green << 8) | blue;

                Color color = Color.FromARGB(argb);

                var t = plt.Add.Polygon(xpoints, ypoints);
                t.LegendText = asset.Key.ToString() + " (earn wallet)";
                t.FillColor = color;
                t.LineColor = Colors.Black;
                t.LineWidth = 1;

            }
            plt.Title("Wallet evolution");
            plt.XLabel("Date");
            plt.YLabel("Price ($)");
            plt.Axes.DateTimeTicksBottom();
            return Utils.ExtractBase64FromImageTag(plt.GetPngHtml(1200, 600));
        }

    }
}