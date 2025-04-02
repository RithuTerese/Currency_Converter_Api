namespace ExchangeRatesAPI.Services
{
    public class CurrencyProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;
        public CurrencyProviderFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ICurrencyProvider GetProvider(string providerName)
        {
            return providerName.ToLower() switch
            {
                "frankfurter" => (ICurrencyProvider)_serviceProvider.GetService(typeof(FrankfurterProvider)),
                "mock" => (ICurrencyProvider)_serviceProvider.GetService(typeof(MockCurrencyProvider)),
                _ => throw new ArgumentException($"Invalid provider name: {providerName}")
            };
        }
    }
}
