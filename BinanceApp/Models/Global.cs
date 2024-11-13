namespace BinanceApp.Models
{
    public class Global
    {
        private static long _timestamp;
        private static string _recvWindow = "50000";
        private static string[] _intervals = ["1s", "1m", "3m", "5m", "15m", "30m", "1h", "2h", "4h", "6h", "8h", "12h", "1d", "3d", "1w", "1M"]; 
        private static List<string> _coinList = new List<string>(); 
        private static string _coin = ""; 
        private static string _dateMin = "2000-01-01";
        private static string _dateMinOr200DaysBeforeEnd = "";
        private static string _dateMaxOr200DaysAfterStart = "";
        private static string _dateMax = DateTime.Now.ToString("yyyy-MM-dd");
        private static string _symbol = "BTCEUR";
        private static string _base64Image = "";
        private static string _value = "";
        private static string _requestUrl = "";
        private static string _interval = "1d";

        public static long timestamp
        {
            get => _timestamp;
            set => _timestamp = value;
        }

        public static string recvWindow
        {
            get => _recvWindow;
            set => _recvWindow = value;
        }

        public static string[] intervals
        {
            get => _intervals;
            set => _intervals = value;
        }

        public static List<string> coinList
        {
            get => _coinList;
            set => _coinList = value;
        }

        public static string coin
        {
            get => _coin;
            set => _coin = value;
        }

        public static string dateMin
        {
            get => _dateMin;
            set => _dateMin = value;
        }

        public static string dateMinOr200DaysBeforeEnd
        {
            get => _dateMinOr200DaysBeforeEnd;
            set => _dateMinOr200DaysBeforeEnd = value;
        }

        public static string dateMaxOr200DaysAfterStart
        {
            get => _dateMaxOr200DaysAfterStart;
            set => _dateMaxOr200DaysAfterStart = value;
        }

        public static string dateMax
        {
            get => _dateMax;
            set => _dateMax = value;
        }

        public static string symbol
        {
            get => _symbol;
            set => _symbol = value;
        }

        public static string base64Image
        {
            get => _base64Image;
            set => _base64Image = value;
        }

        public static string value
        {
            get => _value;
            set => _value = value;
        }

        public static string requestUrl
        {
            get => _requestUrl;
            set => _requestUrl = value;
        }

        public static string interval
        {
            get => _interval;
            set => _interval = value;
        }
    }
}
