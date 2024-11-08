using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScottPlot;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BinanceApp.Models
{
    public class BinanceEarn
    { 
        public static async Task<string> GetSimpleEarnFlexibleAccountInfo()
        {
            HttpClient httpClient = new HttpClient();
            Global.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Dictionary<string, string> parameters = new Dictionary<string, string>
        {
            { "type", "SPOT" },
            { "timestamp", Global.timestamp.ToString() },
            { "recvWindow", Global.recvWindow},
        };

            string requestUrl = Utils.generateUrl("https://api.binance.com/sapi/v1/simple-earn/flexible/position?", parameters, true);

            return await Utils.sendHttpRequest(httpClient, requestUrl);
        }

        public static async Task<string> GetSimpleEarnLockedAccountInfo()
        {
            HttpClient httpClient = new HttpClient();
            Global.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Dictionary<string, string> parameters = new Dictionary<string, string>
        {
            { "type", "SPOT" },
            { "timestamp", Global.timestamp.ToString() },
            { "recvWindow", Global.recvWindow},
        };

            string requestUrl = Utils.generateUrl("https://api.binance.com/sapi/v1/simple-earn/locked/position?", parameters, true);

            return await Utils.sendHttpRequest(httpClient, requestUrl);
        }

        public static async Task<string> GetAggregatedEarnAccountInfo()
        {
            string data = "";
            Dictionary<string, decimal> cryptoCurrencies = new Dictionary<string, decimal>();
            Global.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            data = await GetSimpleEarnFlexibleAccountInfo();
            string simpleEarnFlexibleAccountInfo = data;
            data = await GetSimpleEarnLockedAccountInfo();
            string simpleEarnLockedAccountInfo = data;
            data += simpleEarnFlexibleAccountInfo;
            var regex = new Regex(@"\{""total"":\d+,""rows"":\[(.*?)\]\}");
            var matches = regex.Matches(data);
            foreach (Match match in matches)
            {
                try
                {
                    JObject jsonData = JObject.Parse(match.Value);
                    var rows = jsonData["rows"] as JArray;
                    if (rows != null)
                    {
                        foreach (var row in rows)
                        {
                            string asset = row["asset"]?.ToString();
                            string amountStr = row["amount"]?.ToString() ?? row["totalAmount"]?.ToString();

                            if (asset != null && amountStr != null)
                            {
                                amountStr = amountStr.Trim();
                                if (decimal.TryParse(amountStr, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal amount))
                                {
                                    if (cryptoCurrencies.ContainsKey(asset))
                                    {
                                        cryptoCurrencies[asset] += amount;
                                    }
                                    else
                                    {
                                        cryptoCurrencies[asset] = amount;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (JsonReaderException)
                {
                    continue;
                }
            }
            return JsonConvert.SerializeObject(cryptoCurrencies, Formatting.Indented);
        }

        public static async Task<string> ConcatenateFlexibleAndLockedEarnAccountInfo()
        {
            string value = await BinanceEarn.GetSimpleEarnFlexibleAccountInfo();
            string simpleEarnFlexibleAccountInfo = value;
            value = await BinanceEarn.GetSimpleEarnLockedAccountInfo();
            string simpleEarnLockedAccountInfo = value;
            value = "[";
            value += simpleEarnLockedAccountInfo;
            value += ",";
            value += simpleEarnFlexibleAccountInfo;
            value += "]";
            return value;
        }

        public static Dictionary<string, List<double>> GetAssetAmount(Dictionary<string, List<Double>> assetPrices, JObject json)
        {
            if (json.ContainsKey("rows"))
            {
                foreach (var row in json["rows"])
                {
                    JObject r = JObject.Parse(row.ToString());
                    if (r.ContainsKey("asset") && (r.ContainsKey("totalAmount") || r.ContainsKey("amount")))
                    {
                        double amount = 0.0;
                        string asset = r["asset"].ToString();
                        if (r.ContainsKey("totalAmount")) { amount = double.Parse(r["totalAmount"].ToString(), CultureInfo.InvariantCulture); }
                        else { amount = double.Parse(row["amount"].ToString(), CultureInfo.InvariantCulture); }

                        if (!assetPrices.ContainsKey(asset))
                        {
                            assetPrices[asset] = new List<double>();
                        }
                        assetPrices[asset].Add(amount);
                    }
                }
            }
            return assetPrices;
        }

        public static Dictionary<string, Dictionary<string, List<string>>> GetAssetAmountDate(JArray jarr)
        {
            Dictionary<string, Dictionary<string, List<string>>> assetData = new Dictionary<string, Dictionary<string, List<string>>>();
            foreach(JObject jobj in jarr)
            {
                if (jobj.ContainsKey("rows"))
                {
                    foreach (var row in jobj["rows"])
                    {
                        JObject r = JObject.Parse(row.ToString());
                        if (r.ContainsKey("asset") && (r.ContainsKey("totalAmount") || r.ContainsKey("amount")))
                        {
                            double amount = 0.0;
                            string asset = r["asset"].ToString();
                            string status = "";
                            string time = "";
            
                            if (r.ContainsKey("amount")) { amount = double.Parse(row["amount"].ToString(), CultureInfo.InvariantCulture); }
            
                            if (r.ContainsKey("status")) { status = row["status"].ToString(); }

                            if (r.ContainsKey("time")) { time = row["time"].ToString(); }

                            if (!assetData.ContainsKey(asset) && amount != 0.0 && status != "" && time != "")
                            {
                                assetData[asset] = new Dictionary<string, List<string>>();
                                assetData[asset]["amount"] = new List<string>();
                                assetData[asset]["time"] = new List<string>();
                                assetData[asset]["status"] = new List<string>();
                            }
                           if (amount != 0.0 && status != "" && time != "")
                           {
                               assetData[asset]["amount"].Add(amount.ToString());
                               assetData[asset]["time"].Add(time.ToString());
                               assetData[asset]["status"].Add(status.ToString());
                            }
                        }
                    }
                }
            }
            return assetData;
        }

        public static Dictionary<string, List<double>> ParseAssetData(string jsonResponse)
        {
            var assetPrices = new Dictionary<string, List<double>>();
            string[] jsonParts = jsonResponse.Split(new[] { "]},{" }, StringSplitOptions.None);
            JArray jsonArray1 = JArray.Parse(jsonParts[0] + "]}]");
            JArray jsonArray2 = JArray.Parse("[{" + jsonParts[1]);
            foreach (JObject json in jsonArray1) { assetPrices = GetAssetAmount(assetPrices, json); }
            foreach (JObject json in jsonArray2) { assetPrices = GetAssetAmount(assetPrices, json); }
            return assetPrices;
        }

        public static async Task<JArray> GetSubscriptionAndRedemptionRecord(string asset, string startDate, string endDate, bool isFlex, bool isSub)
        {
            HttpClient httpClient = new HttpClient();
            string requestedUrl = "";
            if (isSub && isFlex) { requestedUrl = "https://api.binance.com/sapi/v1/simple-earn/flexible/history/subscriptionRecord?"; }
            else if (isSub && !isFlex) { requestedUrl = "https://api.binance.com/sapi/v1/simple-earn/locked/history/subscriptionRecord?"; }
            else if (!isSub && isFlex) { requestedUrl = "https://api.binance.com/sapi/v1/simple-earn/flexible/history/redemptionRecord?"; }
            else if (!isSub && !isFlex) { requestedUrl = "https://api.binance.com/sapi/v1/simple-earn/locked/history/redemptionRecord?"; }
            Global.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            DateTime dateStart = (DateTime)DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(startDate)).UtcDateTime;
            DateTime dateEnd = DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(endDate)).UtcDateTime;
            DateTime interDateEnd = dateStart.AddMonths(3);
            int resultPerPage = 10;
            int current = 1;
            string data = "[";
            while (dateEnd >= interDateEnd)
            {
                while (resultPerPage == 10)
                {
                    httpClient = new HttpClient();
                    Global.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    Dictionary<string, string> parameters = new Dictionary<string, string>
                    {
                        { "timestamp", Global.timestamp.ToString() },
                        { "asset", asset },
                        { "recvWindow", Global.recvWindow},
                        { "startTime", ((DateTimeOffset)dateStart).ToUnixTimeMilliseconds().ToString() },
                        { "endTime", ((DateTimeOffset)interDateEnd).ToUnixTimeMilliseconds().ToString() },
                        { "current", current.ToString() }
                    };
                    string requestUrl = Utils.generateUrl(requestedUrl, parameters, true);

                    string result = await Utils.sendHttpRequest(httpClient, requestUrl);
                    data += result + ",";

                    dateStart = interDateEnd;
                    interDateEnd = interDateEnd.AddMonths(3);
                    current++;
                    JObject resultJobj = JObject.Parse(result);
                    if (resultJobj.ContainsKey("rows"))
                    {
                        resultPerPage = resultJobj["rows"].Count();
                    }
                    else
                    {
                        resultPerPage = 0;
                    }
                }
            }
            resultPerPage = 10;
            current = 1;
            if (dateEnd!=interDateEnd)
            {
                while (resultPerPage == 10)
                {
                    httpClient = new HttpClient();
                    Dictionary<string, string> parameters = new Dictionary<string, string>
                    {
                        { "timestamp", Global.timestamp.ToString() },
                        { "asset", asset },
                        { "recvWindow", Global.recvWindow},
                        { "startTime", ((DateTimeOffset)dateStart).ToUnixTimeMilliseconds().ToString() },
                        { "endTime", ((DateTimeOffset)dateEnd).ToUnixTimeMilliseconds().ToString() },
                        { "current", current.ToString() }
                    };
                    string requestUrl = Utils.generateUrl(requestedUrl, parameters, true);

                    string result = await Utils.sendHttpRequest(httpClient, requestUrl);
                    data += result + ",";
                    current++;
                    JObject resultJobj = JObject.Parse(result);
                    if (resultJobj.ContainsKey("rows"))
                    {
                        resultPerPage = resultJobj["rows"].Count();
                    }
                    else
                    {
                        resultPerPage = 0;
                    }
                }
            }

            data += "]";
            return JArray.Parse(data);
        }


        public static async Task<string> ProduceGraphEarnAsset(string asset, DateTime startTime, DateTime endTime, string interval)
        {
            Global.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            JArray flexSubJSON = await GetSubscriptionAndRedemptionRecord(asset, ((DateTimeOffset)startTime).ToUnixTimeMilliseconds().ToString(), ((DateTimeOffset)endTime).ToUnixTimeMilliseconds().ToString(), true, true);
            JArray lockedSubJSON = await GetSubscriptionAndRedemptionRecord(asset, ((DateTimeOffset)startTime).ToUnixTimeMilliseconds().ToString(), ((DateTimeOffset)endTime).ToUnixTimeMilliseconds().ToString(), false, true);
            JArray flexRedJSON = await GetSubscriptionAndRedemptionRecord(asset, ((DateTimeOffset)startTime).ToUnixTimeMilliseconds().ToString(), ((DateTimeOffset)endTime).ToUnixTimeMilliseconds().ToString(), true, false);
            JArray lockedRedJSON = await GetSubscriptionAndRedemptionRecord(asset, ((DateTimeOffset)startTime).ToUnixTimeMilliseconds().ToString(), ((DateTimeOffset)endTime).ToUnixTimeMilliseconds().ToString(), false, false);
            Dictionary<string, Dictionary<string, List<string>>> flexibleSubscriptionRecord = GetAssetAmountDate(flexSubJSON);
            Dictionary<string, Dictionary<string, List<string>>> lockedSubscriptionRecord = GetAssetAmountDate(lockedSubJSON);
            Dictionary<string, Dictionary<string, List<string>>> flexibleRedemptionRecord = GetAssetAmountDate(flexRedJSON);
            Dictionary<string, Dictionary<string, List<string>>> lockedRedemptionRecord = GetAssetAmountDate(lockedRedJSON);
            var dates = new List<double>();
            double startDouble = startTime.ToOADate();
            double endDouble = endTime.ToOADate();
            double delta = Utils.ParseInterval(interval);
            double amount = 0.0;
            double flexSubscriptions = 0.0;
            double lockedSubscriptions = 0.0;
            double flexRedemptions = 0.0;
            double lockedRedemptions = 0.0;
            List<string> flexSubscriptionsRecordAmounts = new List<string>();
            List<string> lockedSubscriptionsRecordAmounts = new List<string>();
            List<string> flexRedemptionsRecordAmounts = new List<string>();
            List<string> lockedRedemptionsRecordAmounts = new List<string>();
            if (flexibleSubscriptionRecord.ContainsKey(asset) && flexibleSubscriptionRecord[asset].ContainsKey("time"))
            {
                flexSubscriptions = flexibleSubscriptionRecord[asset]["time"].Count();
                flexSubscriptionsRecordAmounts = flexibleSubscriptionRecord[asset]["amount"];
                flexSubscriptionsRecordAmounts.Reverse();
            }
            if (lockedSubscriptionRecord.ContainsKey(asset) && lockedSubscriptionRecord[asset].ContainsKey("time"))
            {
                lockedSubscriptions = lockedSubscriptionRecord[asset]["time"].Count();
                lockedSubscriptionsRecordAmounts = lockedSubscriptionRecord[asset]["amount"];
                lockedSubscriptionsRecordAmounts.Reverse();
            }

            if (flexibleRedemptionRecord.ContainsKey(asset) && flexibleRedemptionRecord[asset].ContainsKey("time"))
            {
                flexRedemptions = flexibleRedemptionRecord[asset]["time"].Count();
                flexRedemptionsRecordAmounts = flexibleSubscriptionRecord[asset]["amount"];
                flexRedemptionsRecordAmounts.Reverse();
            }

            if (lockedRedemptionRecord.ContainsKey(asset) && lockedRedemptionRecord[asset].ContainsKey("time"))
            {
                lockedRedemptions = lockedRedemptionRecord[asset]["time"].Count();
                lockedRedemptionsRecordAmounts = lockedSubscriptionRecord[asset]["amount"];
                lockedRedemptionsRecordAmounts.Reverse();
            }
            int indexFlexSubscription = 0;
            int indexLockedSubscription = 0;
            int indexFlexRedemption = 0;
            int indexLockedRedemption = 0;
            for (double d = startDouble; d < endDouble; d+= delta)
            {
                dates.Add(d);
            }
            double[] xpoints = new double[2 * dates.Count];
            double[] ypoints = new double[2 * dates.Count];
            for (int i = 0; i < 2 * dates.Count(); i++)
            {
                if (i >= dates.Count()) 
                { 
                    xpoints[i] = xpoints[xpoints.Count() - 1 - (i % xpoints.Count())]; 
                    ypoints[i] = 0.0; 
                }
                else 
                { 
                    xpoints[i] = dates[i];
                    while (indexFlexSubscription < flexSubscriptions && ((DateTimeOffset)DateTime.FromOADate(dates[i])).ToUnixTimeMilliseconds() >= long.Parse(flexibleSubscriptionRecord[asset]["time"][indexFlexSubscription]))
                    {
                        amount -= double.Parse(flexSubscriptionsRecordAmounts[indexFlexSubscription]);
                        indexFlexSubscription++;
                    }
                    while (indexLockedSubscription < lockedSubscriptions && ((DateTimeOffset)DateTime.FromOADate(dates[i])).ToUnixTimeMilliseconds() >= long.Parse(lockedSubscriptionRecord[asset]["time"][indexLockedSubscription]))
                    {
                        amount -= double.Parse(lockedSubscriptionsRecordAmounts[indexLockedSubscription]);
                        indexLockedSubscription++;
                    }
                    while (indexFlexRedemption < flexRedemptions && ((DateTimeOffset)DateTime.FromOADate(dates[i])).ToUnixTimeMilliseconds() >= long.Parse(flexibleRedemptionRecord[asset]["time"][indexFlexRedemption]))
                    {
                        amount += double.Parse(flexRedemptionsRecordAmounts[indexFlexRedemption]);
                        indexFlexRedemption++;
                    }
                    while (indexLockedRedemption < lockedRedemptions && ((DateTimeOffset)DateTime.FromOADate(dates[i])).ToUnixTimeMilliseconds() >= long.Parse(lockedRedemptionRecord[asset]["time"][indexLockedRedemption]))
                    {
                        amount += double.Parse(lockedRedemptionsRecordAmounts[indexLockedRedemption]);
                        indexLockedRedemption++;
                    }
                    ypoints[i] = amount;
                }
            }
            
            int argb = (255 << 24) | (250 << 16) | (100 << 8) | 100;
            
            Color color = Color.FromARGB(argb);
            Plot plt = new Plot();
            
            var t = plt.Add.Polygon(xpoints, ypoints);
            t.LegendText = asset ;
            t.FillColor = color;
            t.LineColor = Colors.Black;
            t.LineWidth = 1;


            plt.Title("Earn wallet's " + asset + " evolution");
            plt.XLabel("Date");
            plt.YLabel("Tokens");
            plt.Axes.DateTimeTicksBottom();
            return Utils.ExtractBase64FromImageTag(plt.GetPngHtml(1200, 600));
        }
    }
}