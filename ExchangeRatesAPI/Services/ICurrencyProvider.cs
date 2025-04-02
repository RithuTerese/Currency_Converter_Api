using ExchangeRatesAPI.Models;

namespace ExchangeRatesAPI.Services
{
    public interface ICurrencyProvider //common interface
    {
        Task<ExchangeRateResponse> GetLatestRatesAsync(string baseCurrency);

        Task<List<HistoricalRate>> GetHistoricalRatesAsync(string baseCurrency, DateTime startDate, DateTime endDate);
    }
}
