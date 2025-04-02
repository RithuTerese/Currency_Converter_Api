# ExchangeRatesAPI

ExchangeRatesAPI is an Currency Converter API in ASP.NET Core that provides following information:

Objective 
Design and implement a robust, scalable, and maintainable currency conversion API using C# and 
ASP.NET Core, ensuring high performance, security, and resilience. 

Endpoints 
1 Retrieve Latest Exchange Rates 
● Fetch the latest exchange rates for a specific base currency (e.g., EUR). 
2 Currency Conversion 
● Convert amounts between different currencies. 
● Exclude TRY, PLN, THB, and MXN from the response and return a bad request if these 
currencies are involved. 
3 Historical Exchange Rates with Pagination 
● Retrieve historical exchange rates for a given period with pagination (e.g., 2020-01-01 to 
2020-01-31, base EUR).
   
## Table of Contents

- [Features](#features)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Configuration](#configuration)
- [Usage](#usage)
- [Testing](#testing)
- [Logging and Monitoring](#logging-and-monitoring)
- [Contributing](#contributing)

## Features

- **ASP.NET Core Web API**: Built with the latest ASP.NET Core framework.
- **Swagger Integration**: Provides interactive API documentation.
- **Serilog**: Comprehensive logging to various outputs including console and file.
- **OpenTelemetry**: Distributed tracing and monitoring.
- **Polly**: Resilience and transient fault-handling with policies like Retry and Circuit Breaker.
- **API Versioning**: Supports multiple API versions.
- **Rate Limiting**: Implements IP rate limiting to prevent abuse.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) or any compatible IDE
- [Seq](https://datalust.co/seq) (optional, for structured log storage)
- [Jaeger](https://www.jaegertracing.io/download/) (for tracing)

## Installation

1. **Clone the Repository**:

   ```bash
   git clone https://github.com/yourusername/ExchangeRatesAPI.git

## Configuration

  App Settings:
  -appsettings.json.
  
  Launch Settings:
  -The Properties/launchSettings.json file defines multiple environments:
  -Development: Default environment.
  -Test: For testing purposes.
  -Production: For deployment.

## Usage
Running the Application:
For the Development environment:
dotnet run --launch-profile "http"

For the Test environment:
dotnet run --launch-profile "Test"

For the Production environment:
dotnet run --launch-profile "https"

Accessing Swagger UI:

Once the application is running, navigate to:
http://localhost:{port}/swagger
Replace {port} with the port number specified in the launch profile.

## Testing
To run tests:

dotnet test
Ensure that the test environment is configured correctly in the launchSettings.json.

Temporary login :
Username : user or admin
Password : password

Api Version : 1.0 
Providername : frankfurter or mock

Logging and Monitoring
Serilog:

Logs are configured to output to the console and a rolling file (Logs/applog.txt). Ensure the Serilog settings in appsettings.json are configured as desired.

OpenTelemetry:

Traces are exported to:

Jaeger: http://localhost:4317

Ensure these services are running and accessible.

## Contributing
1. Implement Health Checks
Implement health checks to monitor the status of dependencies like external APIs,Db etc. We can set alerts if the health is on risk.
2.Connect to Database
In this project I haven't used the entity framework/Db . Username and password is hardcorded for testing purpose.After connecting to db , real scenarios can be executed ​
