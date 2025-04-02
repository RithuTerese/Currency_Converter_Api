using System.Collections.Generic;

namespace ExchangeRatesAPI.Models
{
    public class HistoricalRate
    {
        public string Date { get; set; }
        public Dictionary<string, decimal> Rates { get; set; }
    }
}
