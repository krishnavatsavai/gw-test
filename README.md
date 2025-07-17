# gw-test

Azure Event Hub sender application with Application Gateway support.

## Configuration

1. Copy `appsettings.json` to `appsettings.local.json`
2. Update the `ConnectionString` in `appsettings.local.json` with your actual Azure Event Hub connection string
3. The application will use the local configuration file when available

## Features

- Direct Event Hub connection
- App Gateway routing using CustomEndpointAddress
- Configurable through appsettings.json

## Usage

```bash
dotnet run
```

The application will automatically use App Gateway routing if `UseAppGateway` is set to `true` in the configuration.