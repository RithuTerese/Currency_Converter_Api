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
- [License](#license)

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

## Installation

1. **Clone the Repository**:

   ```bash
   git clone https://github.com/yourusername/ExchangeRatesAPI.git

2.Configuration

  App Settings:
  -appsettings.json.
  
  Launch Settings:
  
  The Properties/launchSettings.json file defines multiple environments:
  
  Development: Default environment.
  
  Test: For testing purposes.
  
  Production: For deployment.
  
  Ensure the applicationUrl and environmentVariables are set appropriately for each profile.

