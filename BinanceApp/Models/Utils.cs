using Newtonsoft.Json.Linq;
using ScottPlot.Colormaps;

namespace BinanceApp.Models
{
    public class Utils
    {
        public static string ExtractBase64FromImageTag(string imgTag)
        {
            const string base64Prefix = "data:image/png;base64,";
            int startIndex = imgTag.IndexOf(base64Prefix);

            if (startIndex == -1)
                throw new ArgumentException("The provided string does not contain a Base64 image.");

            startIndex += base64Prefix.Length;
            int endIndex = imgTag.IndexOf("\"", startIndex);

            if (endIndex == -1)
                throw new ArgumentException("Invalid image tag format.");

            string base64Data = imgTag.Substring(startIndex, endIndex - startIndex);
            return base64Data;
        }



        public static string generateUrl(string baseUrl, Dictionary<string, string> parameters, bool signatureMandatory)
        {
            var queryString = string.Join("&", parameters.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            baseUrl += queryString;
            if (signatureMandatory)
            {
                string signature = Encryption.Sign(EncryptionConstants.RSAPrivateKey, queryString);
                string finalUrl = baseUrl + "&signature=" + signature;
                return finalUrl;
            }
            else
            {
                return baseUrl;
            }
        }

        public static async Task<string> sendHttpRequest(HttpClient httpClient, string requestUrl)
        {
            string result = "";
            try
            {
                using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUrl))
                {
                    httpClient.DefaultRequestHeaders.Add("X-MBX-APIKEY", EncryptionConstants.APIKey);
                    var response = await httpClient.SendAsync(requestMessage);
                    if (response.IsSuccessStatusCode)
                    {
                        result = await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        result = $"Error: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}" + " | " + requestUrl;
                    }
                }
            }
            catch (Exception ex)
            {
                result = "Error fetching: " + ex.Message;
            }
            return result;
        }

        public static double ParseInterval(string interval)
        {
            double delta = 0.0;
            double coeff = double.Parse(interval[0].ToString());
            if (interval[interval.Count() - 1] == 's') { delta = coeff / 86400; }
            else if (interval[interval.Count() - 1] == 'm') { delta = coeff / 1440; }
            else if (interval[interval.Count() - 1] == 'h') { delta = coeff / 24; }
            else if (interval[interval.Count() - 1] == 'd') { delta = coeff; }
            else if (interval[interval.Count() - 1] == 'w') { delta = coeff * 7; }
            else if (interval[interval.Count() - 1] == 'M') { delta = coeff * 30; }
            return delta;
        }
    }
}