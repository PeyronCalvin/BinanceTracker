using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using ScottPlot;

namespace BinanceApp.Models
{
    public class Kline
    {
        public DateTime OpenTime { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public double Volume { get; set; }
        public DateTime CloseTime { get; set; }

        public override string ToString()
        {
            return "OpenTime: " + OpenTime.ToString() + " | Open: " + Open.ToString() + " | High: " + High.ToString() + " | Low: " + Low.ToString() + " | Close: " + Close.ToString() + " | Volume: " + Volume + " | CloseTime: " + CloseTime.ToString();
        }



        public static async Task<List<Kline>> RetrieveKlineCandles(string symbol, string startTime, string endTime, string interval)
        {
            HttpClient httpClient = new HttpClient();
            Global.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Dictionary<string, string> parameters = new Dictionary<string, string> { };
            if (symbol != null && symbol != "") { parameters["symbol"] = symbol + "USDT"; }
            if (startTime != null && startTime != "") { parameters["startTime"] = startTime; }
            if (endTime != null && endTime != "") { parameters["endTime"] = endTime; }
            if (interval != null && interval != "") { parameters["interval"] = interval; }

            string requestUrl = Utils.generateUrl("https://api.binance.com/api/v3/klines?", parameters, false);


            string response = await Utils.sendHttpRequest(httpClient, requestUrl);

            var klines = JsonConvert.DeserializeObject<List<List<object>>>(response);
            List<Kline> klineData = new List<Kline>();


            foreach (var item in klines)
            {
                var kline = new Kline
                {
                    OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(System.Convert.ToInt64(item[0])).UtcDateTime,
                    Open = Convert.ToDouble(item[1], System.Globalization.CultureInfo.InvariantCulture),
                    High = Convert.ToDouble(item[2], System.Globalization.CultureInfo.InvariantCulture),
                    Low = Convert.ToDouble(item[3], System.Globalization.CultureInfo.InvariantCulture),
                    Close = Convert.ToDouble(item[4], System.Globalization.CultureInfo.InvariantCulture),
                    Volume = Convert.ToDouble(item[5], System.Globalization.CultureInfo.InvariantCulture),
                    CloseTime = DateTimeOffset.FromUnixTimeMilliseconds(System.Convert.ToInt64(item[6])).UtcDateTime
                };
                klineData.Add(kline);
            }
            return klineData;

        }

        public static async Task<string> CreateKlineCandles(string symbol, string startTime, string endTime, string interval)
        {
            await Kline.RetrieveKlineCandles(symbol, startTime, endTime, interval);
            List<Kline> klineData = new List<Kline>();
            var plt = new Plot();

            DateTime[] dates = new DateTime[klineData.Count];
            double[] opens = new double[klineData.Count];
            double[] highs = new double[klineData.Count];
            double[] lows = new double[klineData.Count];
            double[] closes = new double[klineData.Count];

            for (int i = 0; i < klineData.Count; i++)
            {
                dates[i] = klineData[i].OpenTime;
                opens[i] = klineData[i].Open;
                highs[i] = klineData[i].High;
                lows[i] = klineData[i].Low;
                closes[i] = klineData[i].Close;
            }
            List<OHLC> ohlcData = new List<OHLC>();
            for (int i = 0; i < dates.Length; i++)
            {
                var ohlc = new OHLC(opens[i], closes[i], lows[i], highs[i], dates[i], TimeSpan.FromDays(0.01));
                ohlcData.Add(ohlc);
            }
            var candlestickPlot = plt.Add.Candlestick(ohlcData);

            plt.Title("Binance Candlestick Chart");
            plt.YLabel("Price (EUR)");
            plt.XLabel("Date");
            plt.Axes.DateTimeTicksBottom();
            return Utils.ExtractBase64FromImageTag(plt.GetPngHtml(800, 600));
        }
    }
}