using ExchangeRatesAPI.Services;
using Moq;
using Xunit;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using Moq.Protected;
using System.Threading;
using System.Text.Json;

public class FrankfurterProviderTests
{
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<ILogger<FrankfurterProvider>> _mockLogger;
    private readonly HttpClient _httpClient;

    public FrankfurterProviderTests()
    {
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockLogger = new Mock<ILogger<FrankfurterProvider>>();

        // Mock HttpClient response
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"rates\":{\"USD\":1.2},\"base\":\"EUR\"}")
            });

        _httpClient = new HttpClient(handlerMock.Object);
    }

    [Fact]
    public async Task GetLatestRatesAsync_Returns_ValidResponse()
    {
        var provider = new FrankfurterProvider(_httpClient, _mockHttpContextAccessor.Object, _mockLogger.Object);
        var result = await provider.GetLatestRatesAsync("EUR");

        Assert.NotNull(result);
        Assert.Equal("EUR", result.Base);
        Assert.True(result.Rates.ContainsKey("USD"));
    }
}
