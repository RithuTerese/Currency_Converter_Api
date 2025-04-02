using ExchangeRatesAPI.Models;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ExchangeRatesAPI.Services
{
    public class FrankfurterProvider : ICurrencyProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _httpClient;
        private readonly ILogger<FrankfurterProvider> _logger;  // Add ILogger

        private const string API_URL = "https://api.frankfurter.app/latest";
        private const string CorrelationIdHeader = "X-Correlation-ID";

        public FrankfurterProvider(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ILogger<FrankfurterProvider> logger)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<ExchangeRateResponse> GetLatestRatesAsync(string baseCurrency)
        {
            // Retrieve correlation ID from the incoming request (if available)
            var correlationId = _httpContextAccessor.HttpContext?.Request.Headers[CorrelationIdHeader].ToString() ?? Guid.NewGuid().ToString();

            // Log the correlation ID using ILogger
            _logger.LogInformation("Making request to Frankfurter API with Correlation ID: {CorrelationId}", correlationId);

            // Construct the API endpoint with base currency 
            var requestUrl = $"{API_URL}?base={baseCurrency}";

            // Prepare the outgoing HTTP request and add the correlation ID as a header
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Add(CorrelationIdHeader, correlationId);

            // Send the HTTP request and handle the response
            var response = await _httpClient.SendAsync(request);

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
            var correlationId = _httpContextAccessor.HttpContext?.Request.Headers[CorrelationIdHeader].ToString() ?? Guid.NewGuid().ToString();
            _logger.LogInformation("Fetching historical exchange rates with Correlation ID: {CorrelationId}", correlationId);

            var historicalRates = new List<HistoricalRate>();

            // Format the start and end dates as strings
            var startDateStr = startDate.ToString("yyyy-MM-dd");
            var endDateStr = endDate.ToString("yyyy-MM-dd");

            try
            {
                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    var requestUrl = $"{API_URL}?start_date={startDateStr}&end_date={endDateStr}&base={baseCurrency}";
                    var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                    request.Headers.Add(CorrelationIdHeader, correlationId);

                    var httpResponse = await _httpClient.SendAsync(request);

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
                        _logger.LogWarning("Unable to fetch exchange rates for {Date}. Status: {StatusCode}. Correlation ID: {CorrelationId}",
                            date.ToString("yyyy-MM-dd"), httpResponse.StatusCode, correlationId);
                        //Optional
                        Console.WriteLine($"Error: Unable to fetch exchange rates for base currency '{baseCurrency}' on {date:yyyy-MM-dd}. Status Code: {httpResponse.StatusCode}");
                        break; // Stop further requests if an error occurs
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Request error while fetching exchange rates. Correlation ID: {CorrelationId}", correlationId);
                Console.WriteLine($"Request error: {ex.Message}. Correlation ID: {correlationId}");//optional
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Request error while fetching exchange rates. Correlation ID: {CorrelationId}", correlationId);
                Console.WriteLine($"Unexpected error: {ex.Message}. Correlation ID: {correlationId}");//optional
            }
            return historicalRates;
        }
    }
}
