using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScottPlot;
using System;
using System.Collections.Generic;
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

        public static Dictionary<string, List<decimal>> GetAssetAmount(Dictionary<string, List<decimal>> assetPrices, JObject json)
        {
            if (json.ContainsKey("rows"))
            {
                foreach (var row in json["rows"])
                {
                    JObject r = JObject.Parse(row.ToString());
                    if (r.ContainsKey("asset") && (r.ContainsKey("totalAmount") || r.ContainsKey("amount")))
                    {
                        decimal amount = 0.0m;
                        string asset = r["asset"].ToString();
                        if (r.ContainsKey("totalAmount")) { amount = decimal.Parse(r["totalAmount"].ToString(), CultureInfo.InvariantCulture); }
                        else { amount = decimal.Parse(row["amount"].ToString(), CultureInfo.InvariantCulture); }

                        if (!assetPrices.ContainsKey(asset))
                        {
                            assetPrices[asset] = new List<decimal>();
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
                               assetData[asset]["amount"].Add(amount.ToString("F16"));
                               assetData[asset]["time"].Add(time.ToString());
                               assetData[asset]["status"].Add(status.ToString());
                            }
                        }
                    }
                }
            }
            return assetData;
        }

        public static Dictionary<string, List<decimal>> ParseAssetData(string jsonResponse)
        {
            var assetPrices = new Dictionary<string, List<decimal>>();
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
            Global.value = "";
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
            decimal amount = 0.0m;
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


                var sortedIndices = flexibleSubscriptionRecord[asset]["time"].Select((time, index) => new { time, index }).OrderBy(x => x.time).Select(x => x.index).ToList();

                flexibleSubscriptionRecord[asset]["amount"] = sortedIndices.Select(i => flexibleSubscriptionRecord[asset]["amount"][i]).ToList();
                flexibleSubscriptionRecord[asset]["time"] = sortedIndices.Select(i => flexibleSubscriptionRecord[asset]["time"][i]).ToList();
                flexibleSubscriptionRecord[asset]["status"] = sortedIndices.Select(i => flexibleSubscriptionRecord[asset]["status"][i]).ToList();

                flexSubscriptionsRecordAmounts = flexibleSubscriptionRecord[asset]["amount"];
            }
            if (lockedSubscriptionRecord.ContainsKey(asset) && lockedSubscriptionRecord[asset].ContainsKey("time"))
            {
                lockedSubscriptions = lockedSubscriptionRecord[asset]["time"].Count();

                var sortedIndices = lockedSubscriptionRecord[asset]["time"].Select((time, index) => new { time, index }).OrderBy(x => x.time).Select(x => x.index).ToList();

                lockedSubscriptionRecord[asset]["amount"] = sortedIndices.Select(i => lockedSubscriptionRecord[asset]["amount"][i]).ToList();
                lockedSubscriptionRecord[asset]["time"] = sortedIndices.Select(i => lockedSubscriptionRecord[asset]["time"][i]).ToList();
                lockedSubscriptionRecord[asset]["status"] = sortedIndices.Select(i => lockedSubscriptionRecord[asset]["status"][i]).ToList();

                lockedSubscriptionsRecordAmounts = lockedSubscriptionRecord[asset]["amount"];
            }

            if (flexibleRedemptionRecord.ContainsKey(asset) && flexibleRedemptionRecord[asset].ContainsKey("time"))
            {
                flexRedemptions = flexibleRedemptionRecord[asset]["time"].Count();

                var sortedIndices = flexibleRedemptionRecord[asset]["time"].Select((time, index) => new { time, index }).OrderBy(x => x.time).Select(x => x.index).ToList();

                flexibleRedemptionRecord[asset]["amount"] = sortedIndices.Select(i => flexibleRedemptionRecord[asset]["amount"][i]).ToList();
                flexibleRedemptionRecord[asset]["time"] = sortedIndices.Select(i => flexibleRedemptionRecord[asset]["time"][i]).ToList();
                flexibleRedemptionRecord[asset]["status"] = sortedIndices.Select(i => flexibleRedemptionRecord[asset]["status"][i]).ToList();

                flexRedemptionsRecordAmounts = flexibleRedemptionRecord[asset]["amount"];
            }

            if (lockedRedemptionRecord.ContainsKey(asset) && lockedRedemptionRecord[asset].ContainsKey("time"))
            {
                lockedRedemptions = lockedRedemptionRecord[asset]["time"].Count();

                var sortedIndices = lockedRedemptionRecord[asset]["time"].Select((time, index) => new { time, index }).OrderBy(x => x.time).Select(x => x.index).ToList();

                lockedRedemptionRecord[asset]["amount"] = sortedIndices.Select(i => lockedRedemptionRecord[asset]["amount"][i]).ToList();
                lockedRedemptionRecord[asset]["time"] = sortedIndices.Select(i => lockedRedemptionRecord[asset]["time"][i]).ToList();
                lockedRedemptionRecord[asset]["status"] = sortedIndices.Select(i => lockedRedemptionRecord[asset]["status"][i]).ToList();

                lockedRedemptionsRecordAmounts = lockedRedemptionRecord[asset]["amount"];
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
            decimal parsedValue;


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
                        if (decimal.TryParse(flexSubscriptionsRecordAmounts[indexFlexSubscription], out parsedValue)){
                            amount += parsedValue;
                        }
                        indexFlexSubscription++;
                    }
                    while (indexLockedSubscription < lockedSubscriptions && ((DateTimeOffset)DateTime.FromOADate(dates[i])).ToUnixTimeMilliseconds() >= long.Parse(lockedSubscriptionRecord[asset]["time"][indexLockedSubscription]))
                    {
                        if (decimal.TryParse(lockedSubscriptionsRecordAmounts[indexLockedSubscription], out parsedValue)){
                            amount += parsedValue;
                        }
                        indexLockedSubscription++;
                    }
                    while (indexFlexRedemption < flexRedemptions && ((DateTimeOffset)DateTime.FromOADate(dates[i])).ToUnixTimeMilliseconds() >= long.Parse(flexibleRedemptionRecord[asset]["time"][indexFlexRedemption]))
                    {
                        if (decimal.TryParse(flexRedemptionsRecordAmounts[indexFlexRedemption], out parsedValue)){
                            amount -= parsedValue;
                        }
                        indexFlexRedemption++;
                    }
                    while (indexLockedRedemption < lockedRedemptions && ((DateTimeOffset)DateTime.FromOADate(dates[i])).ToUnixTimeMilliseconds() >= long.Parse(lockedRedemptionRecord[asset]["time"][indexLockedRedemption]))
                    {
                        if (decimal.TryParse(lockedRedemptionsRecordAmounts[indexLockedRedemption], out parsedValue)){
                            amount -= parsedValue;
                        }
                        indexLockedRedemption++;
                    }
                    ypoints[i] = decimal.ToDouble(amount);
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

        public static async Task<List<string>> GetEarnCoinList()
        {
            List<string> coinList = new List<string>();
            string[] requestedUrls = ["https://api.binance.com/sapi/v1/simple-earn/flexible/position?", "https://api.binance.com/sapi/v1/simple-earn/locked/position?"];
            Dictionary<string, string> parameters = new Dictionary<string, string>{{ "size", "100" }};
            foreach(var requestedUrl in requestedUrls)
            {
                HttpClient httpClient = new HttpClient();
                Global.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                parameters["timestamp"] = Global.timestamp.ToString();
                string requestUrl = Utils.generateUrl(requestedUrl, parameters, true);
                string result = await Utils.sendHttpRequest(httpClient, requestUrl);
                
                JObject resultJobj = JObject.Parse(result);
                if (resultJobj.ContainsKey("rows"))
                {
                    JArray rows = JArray.Parse(resultJobj["rows"].ToString());
                    foreach (var row in rows)
                    {
                        JObject json = JObject.Parse(row.ToString());
                        if (json.ContainsKey("asset") && !coinList.Contains(json["asset"].ToString()))
                        {
                            coinList.Add(json["asset"].ToString());
                        }
                    }
                }
            }

            return coinList;
        }
    }
}