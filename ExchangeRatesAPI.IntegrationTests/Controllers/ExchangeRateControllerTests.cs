using ExchangeRatesAPI;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using System.Threading.Tasks;
using Xunit;

public class ExchangeRateControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ExchangeRateControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    //[Fact]
    //public async Task GetExchangeRates_Returns_OK()
    //{
    //    var response = await _client.GetAsync("/api/exchangerates?baseCurrency=EUR");

    //    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    //}

    [Fact]
    public async Task GetExchangeRates_ReturnsSuccessStatusCode()
    {
        // Arrange
        var request = "/api/exchangerates";

        // Act
        var response = await _client.GetAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
    }
}
