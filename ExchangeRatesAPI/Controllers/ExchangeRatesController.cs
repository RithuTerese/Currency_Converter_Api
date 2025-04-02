using ExchangeRatesAPI.Models;
using ExchangeRatesAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Authorization;
using Polly.CircuitBreaker;
using Asp.Versioning;

namespace ExchangeRatesAPI.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [Authorize]    //Secure
    public class ExchangeRatesController : ControllerBase
    {
        private readonly CurrencyProviderFactory _providerFactory;

        private readonly IMemoryCache _cache; // Injecting IMemoryCache

        //private readonly IConfiguration _iconfig;

        private static readonly string[] ExcludedCurrencies = { "TRY", "PLN", "THB", "MXN" };

        public ExchangeRatesController(CurrencyProviderFactory providerFactory, IMemoryCache memoryCache ) //Adding IConfiguration to take the default provider from jsonsettings
        {
            _providerFactory = providerFactory;
            _cache = memoryCache;
            //_iconfig = _iconfiguration;
        }

        [HttpGet("{providerName}")]
        [Authorize(Roles = "user,admin")]
        public async Task<IActionResult> GetLatestRates(string providerName,[FromQuery] string baseCurrency = "EUR")
        {
            try
            {
                // Get default provider from configuration
                //var provider_default = _iconfig["ExchangeRateProvider:Default"];
                //var provider = _providerFactory.GetProvider(provider_default);

                var provider = _providerFactory.GetProvider(providerName);
                var cacheKey = $"LatestRates_{baseCurrency}";

                if (!_cache.TryGetValue(cacheKey, out ExchangeRateResponse response))
                {
                    // Fetch from the service and store in cache if not present
                    var rates = await provider.GetLatestRatesAsync(baseCurrency);
                    response = new ExchangeRateResponse
                    {
                        Base = rates.Base,
                        Date = rates.Date,
                        Rates = rates.Rates,
                        Amount = 1
                    };

                    // Set cache options and store the data for 5 minutes
                    var cacheEntryOptions = new MemoryCacheEntryOptions()
                        .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                        .SetAbsoluteExpiration(TimeSpan.FromHours(1));
                    _cache.Set(cacheKey, response, cacheEntryOptions);
                }
                return Ok(response);
            }
            catch (BrokenCircuitException)
            {
                // Return this message when the circuit is open
                return StatusCode(503, "The exchange rates service is temporarily unavailable. Please try again later.");
            }
        }

        [HttpGet("{providerName}/convert")]
        [Authorize(Roles = "admin,user")]
        public async Task<IActionResult> ConvertCurrency(string providerName,[FromQuery] string from, [FromQuery] string to, [FromQuery] double amount)
        {
            // Check for excluded currencies
            if (ExcludedCurrencies.Contains(from) || ExcludedCurrencies.Contains(to))
            {
                return BadRequest(new { Error = "Currency conversion involving TRY, PLN, THB, and MXN is not allowed." });
            }

            // Create a unique cache key using from currency
            string cacheKey = $"ExchangeRates_{from}";
            var provider = _providerFactory.GetProvider(providerName);
            ExchangeRateResponse rates;
            if (!_cache.TryGetValue(cacheKey, out rates))
            {
                // Fetch the latest exchange rates 
                rates = await provider.GetLatestRatesAsync(from);
                if (rates == null)
                {
                    return NotFound(new { Error = "Unable to fetch exchange rates at this time." });
                }
                // Cache the rates with sliding and absolute expiration
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                    .SetAbsoluteExpiration(TimeSpan.FromHours(1));

                _cache.Set(cacheKey, rates, cacheEntryOptions);
            }

            // Calculate the converted amount
            if (rates.Rates.ContainsKey(to))
            {
                double conversionRate = (double)rates.Rates[to];
                double convertedAmount = amount * conversionRate;

                // Return the result
                return Ok(new
                {
                    from,
                    to,
                    amount,
                    convertedAmount,
                    conversionRate,
                    date = rates.Date
                });
            }

            // Handle case when to currency is not found
            return NotFound(new { Error = $"Exchange rate for currency '{to}' not found." });
        }

        // Endpoint to retrieve historical exchange rates with pagination
        [HttpGet("{providerName}/historical")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetHistoricalRates(string providerName,
            [FromQuery] string baseCurrency = "EUR",
            [FromQuery] DateTime startDate = default,
            [FromQuery] DateTime endDate = default,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // Validate date range
            if (startDate == default || endDate == default || startDate > endDate)
            {
                return BadRequest(new { Error = "Invalid date range. Provide valid start and end dates." });
            }

            // Unique cache key based on parameters
            string cacheKey = $"HistoricalRates_{baseCurrency}_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";

            var provider = _providerFactory.GetProvider(providerName);

            // Try to get data from cache
            List<HistoricalRate> historicalRates;
            if (!_cache.TryGetValue(cacheKey, out historicalRates))
            {
                // Get all historical rates for the date range
                historicalRates = await provider.GetHistoricalRatesAsync(baseCurrency, startDate, endDate);

                if (!historicalRates.Any())
                {
                    return NotFound(new { Error = $"Currency '{baseCurrency}' not found in historical data." });
                }

                // Cache the historical rates with expiration options
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(10)) // Reset expiration if accessed within 10 minutes
                    .SetAbsoluteExpiration(TimeSpan.FromHours(2));  // Maximum cache lifetime of 2 hours

                _cache.Set(cacheKey, historicalRates, cacheEntryOptions);
            }
                
            //Check if the requested base currency is found in the rates
            if (!historicalRates.Any())// || historicalRates.All(hr => !hr.Rates.ContainsKey(baseCurrency))
            {
                return NotFound(new { Error = $"Currency '{baseCurrency}' not found in historical data." });
            }

            // Apply pagination
            var paginatedRates = historicalRates
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Return paginated results
            return Ok(new
            {
                baseCurrency,
                startDate = startDate.ToString("yyyy-MM-dd"),
                endDate = endDate.ToString("yyyy-MM-dd"),
                page,
                pageSize,
                totalRecords = historicalRates.Count,
                rates = paginatedRates
            });
        }
    }
}
