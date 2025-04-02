using ExchangeRatesAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Win32;
using Polly;
using Polly.CircuitBreaker;
using System.Text;
using Microsoft.OpenApi.Models;
using AspNetCoreRateLimit;
using ExchangeRatesAPI.Middleware;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Enrichers.AspNetCore;
using Serilog.Core;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using OpenTelemetry.Exporter;
using Asp.Versioning;

var builder = WebApplication.CreateBuilder(args);

// ---------- Serilog Configuration ----------
builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration
        .MinimumLevel.Information()  // Set minimum logging level to Information
        .Enrich.WithMachineName()    // Enrich logs with machine name
        .Enrich.WithEnvironmentName()  // Add the environment name (Development, Production)
        .WriteTo.Console()           // Log to console for real-time monitoring
        .WriteTo.File("Logs/applog.txt", rollingInterval: RollingInterval.Day)  // Log to file with daily rolling
        .WriteTo.Seq("http://localhost:5341"); // Send logs to Seq (Adjust URL as per your setup)
});

// Load configuration from appsettings.json and appsettings.{Environment}.json
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache(); // Add in-memory caching

// Load IP Rate Limiting configuration from appsettings.json
builder.Services.AddOptions();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Configure JWT authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    }; 
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine("Authentication failed: " + context.Exception.Message);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(); // Register authorization services

// Configure retry policy with exponential backoff
var retryPolicy = Policy<HttpResponseMessage>
    .Handle<HttpRequestException>()
    .OrResult(r => r.StatusCode == System.Net.HttpStatusCode.InternalServerError)
    .WaitAndRetryAsync(
        5, // Retry 5 times
        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
        (outcome, timespan, retryAttempt, context) =>
        {
            Console.WriteLine($"Retry {retryAttempt} encountered an error. Waiting {timespan} before next retry.");
        });

// Configure circuit breaker policy
var circuitBreakerPolicy = Policy<HttpResponseMessage>
    .Handle<HttpRequestException>()
    .OrResult(r => r.StatusCode == System.Net.HttpStatusCode.InternalServerError)
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 3, // Break the circuit after 3 consecutive failures
        durationOfBreak: TimeSpan.FromSeconds(30), // Keep circuit open for 30 seconds before testing again
        onBreak: (result, breakDelay) =>
        {
            Console.WriteLine($"Circuit opened due to: {result.Exception?.Message ?? result.Result.StatusCode.ToString()}. Break duration: {breakDelay.TotalSeconds} seconds.");
        },
        onReset: () => Console.WriteLine("Circuit reset - API is responding again."),
        onHalfOpen: () => Console.WriteLine("Circuit half-open - testing API connectivity.")
    );

// Register the FrankfurterProvider with HttpClient and the policies.
builder.Services.AddHttpClient<FrankfurterProvider>()
    .AddPolicyHandler(retryPolicy)
    .AddPolicyHandler(circuitBreakerPolicy);

// Register the MockCurrencyProvider.
builder.Services.AddTransient<MockCurrencyProvider>();

// Register the CurrencyProviderFactory so that it can resolve providers dynamically.
builder.Services.AddSingleton<CurrencyProviderFactory>();

//defines a default API version and ensures that the API reports its supported versions.?
builder.Services.AddApiVersioning(options =>
{
    options.ReportApiVersions = true;
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
});

//Swagger config
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ExchangeRates API", Version = "v1" });

    // Define the security scheme
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Add a global security requirement which uses the defined scheme
    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

// Add OpenTelemetry Tracing
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .ConfigureResource(resource => resource.AddService("ExchangeRatesAPI"))
            .AddAspNetCoreInstrumentation() // Capture incoming HTTP requests
            .AddHttpClientInstrumentation() // Capture outgoing HTTP calls
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://localhost:4317"); // Jaeger's OTLP endpoint
                options.Protocol = OtlpExportProtocol.Grpc; // Recommended for Jaeger
            })
            .AddZipkinExporter(options => // Export traces to Zipkin
            {
                options.Endpoint = new Uri("http://localhost:9411/api/v2/spans");
            })
            .AddConsoleExporter(); // Export traces to console (for debugging)
    });

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// Register the CorrelationIdMiddleware before other middlewares
app.UseMiddleware<CorrelationIdMiddleware>();
// Add rate limiting middleware before authentication
app.UseIpRateLimiting();//Throttling
// Middleware pipeline starts here
app.UseRouting();  // Enable routing and match the incoming requests to routes

app.UseAuthentication();  // Check authentication status (if implemented)
app.UseAuthorization();   // Apply authorization policies

// Register custom request logging middleware
app.UseMiddleware<RequestLoggingMiddleware>();

// Map endpoints to controller actions or other handlers
app.MapControllers();

app.Run();

