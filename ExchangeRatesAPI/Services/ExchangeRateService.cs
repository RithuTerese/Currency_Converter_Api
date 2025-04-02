// Services/ExchangeRateService.cs
using ExchangeRatesAPI.Models;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ExchangeRatesAPI.Services
{
    public class ExchangeRateService
    {
        private readonly HttpClient _httpClient;
        private const string API_URL = "https://api.frankfurter.app/latest";

        public ExchangeRateService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ExchangeRateResponse> GetLatestRatesAsync(string baseCurrency)
        {
            // Construct the API endpoint with base currency 
            var requestUrl = $"{API_URL}?base={baseCurrency}";

            // Send the HTTP request and get the response
            var response = await _httpClient.GetAsync(requestUrl);

            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();

            // Deserialize JSON to our model
            var exchangeRateResponse = JsonSerializer.Deserialize<ExchangeRateResponse>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return exchangeRateResponse;
        }

        public async Task<List<HistoricalRate>> GetHistoricalRatesAsync(string baseCurrency, DateTime startDate, DateTime endDate)
        {
            var historicalRates = new List<HistoricalRate>();

            // Format the start and end dates as strings
            var startDateStr = startDate.ToString("yyyy-MM-dd");
            var endDateStr = endDate.ToString("yyyy-MM-dd");

            try
            {
                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    var requestUrl = $"{API_URL}?start_date={startDateStr}&end_date={endDateStr}&base={baseCurrency}";
                    var httpResponse = await _httpClient.GetAsync(requestUrl);
                    if (httpResponse.IsSuccessStatusCode)
                    {
                        var response = await httpResponse.Content.ReadFromJsonAsync<ExchangeRateApiResponse>();

                        if (response?.Rates != null)
                        {
                            historicalRates.Add(new HistoricalRate
                            {
                                Date = date.ToString("yyyy-MM-dd"),
                                Rates = response.Rates
                            });
                        }
                    }
                    else
                    {
                        // Handle API errors 
                        Console.WriteLine($"Error: Unable to fetch exchange rates for base currency '{baseCurrency}' on {date:yyyy-MM-dd}. Status Code: {httpResponse.StatusCode}");
                        break; // Stop further requests if an error occurs
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Request error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }
            return historicalRates;
        }
    }
}
