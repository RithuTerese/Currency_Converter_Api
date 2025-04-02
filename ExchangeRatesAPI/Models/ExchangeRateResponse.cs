using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ExchangeRatesAPI.Models
{
    public class ExchangeRateResponse
    {
        public double Amount { get; set; }
        [JsonPropertyName("base")]
        public string Base { get; set; }
        public string Date { get; set; }
        public Dictionary<string, decimal> Rates { get; set; }
    }
}
