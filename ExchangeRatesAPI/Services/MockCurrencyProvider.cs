using ExchangeRatesAPI.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExchangeRatesAPI.Services
{
    public class MockCurrencyProvider : ICurrencyProvider
    {
        public Task<ExchangeRateResponse> GetLatestRatesAsync(string baseCurrency)
        {
            var mockRates = new ExchangeRateResponse
            {
                Base = baseCurrency,
                Rates = new Dictionary<string, decimal> { { "USD", 1.0m }, { "EUR", 0.85m } },
                Date = DateTime.UtcNow.ToString("yyyy-MM-dd")
            };

            return Task.FromResult(mockRates);
        }

        public Task<List<HistoricalRate>> GetHistoricalRatesAsync(string baseCurrency, DateTime startDate, DateTime endDate)
        {
            var mockHistoricalRates = new List<HistoricalRate>
            {
                new HistoricalRate
                {
                    Date = startDate.ToString("yyyy-MM-dd"),
                    Rates = new Dictionary<string, decimal> { { "USD", 1.2m }, { "EUR", 0.9m } }
                },
                new HistoricalRate
                {
                    Date = endDate.ToString("yyyy-MM-dd"),
                    Rates = new Dictionary<string, decimal> { { "USD", 1.3m }, { "EUR", 0.88m } }
                }
            };

            return Task.FromResult(mockHistoricalRates);
        }
    }
}
