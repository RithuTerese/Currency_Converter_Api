using System;
using System.Collections.Generic;

namespace ExchangeRatesAPI.Models
{
    public class ExchangeRateApiResponse
    {
        public Dictionary<string, decimal> Rates { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Base { get; set; }
        public double Amount { get; set; }
        // API might return some metadata (for pagination)
        public int? Count { get; set; }
    }
}
