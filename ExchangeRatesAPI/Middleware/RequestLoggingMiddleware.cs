using System.IdentityModel.Tokens.Jwt;

namespace ExchangeRatesAPI.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew(); // Start timing the request

            try
            {
                // Let the pipeline continue processing the request
                await _next(context);
            }
            finally
            {
                stopwatch.Stop(); // Stop the timer after request processing

                // Extract the required details
                var clientIp = context.Connection.RemoteIpAddress?.ToString();
                var jwtToken = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                var clientId = GetClientIdFromJwt(jwtToken); // Extract clientId from the JWT token

                var method = context.Request.Method;
                var path = context.Request.Path;
                var responseStatusCode = context.Response.StatusCode;
                var responseTime = stopwatch.ElapsedMilliseconds; // Get response time in milliseconds

                // Log the extracted details
                _logger.LogInformation("Request Details: Client IP: {ClientIp}, ClientId: {ClientId}, HTTP Method: {Method}, Endpoint: {Path}, Response Code: {StatusCode}, Response Time: {ResponseTime} ms",
                    clientIp, clientId, method, path, responseStatusCode, responseTime);
                Console.WriteLine($"Request Log => IP: {clientIp}, ClientId: {clientId}, " +
                  $"HTTP Method: {method}, Endpoint: {path}, Response Time: {responseTime} ms, Response Code: {responseStatusCode}");
            }
        }

        // Method to extract ClientId from JWT token (if available)
        private string GetClientIdFromJwt(string jwtToken)
        {
            if (string.IsNullOrEmpty(jwtToken))
                return "N/A"; // Return N/A if the token is not present

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwtToken);

            // Assuming client_id is stored as a claim in the JWT token
            return token.Claims.FirstOrDefault(claim => claim.Type == "client_id")?.Value ?? "N/A";
        }
    }

}
