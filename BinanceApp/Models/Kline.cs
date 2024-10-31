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
    }
}