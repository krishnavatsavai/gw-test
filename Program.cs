using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Configuration;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // Load configuration from appsettings.json
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        string connectionString = configuration["EventHub:ConnectionString"] ?? 
            throw new InvalidOperationException("EventHub:ConnectionString not found in configuration");
        string eventHubName = configuration["EventHub:EventHubName"] ?? 
            throw new InvalidOperationException("EventHub:EventHubName not found in configuration");
        
        bool useAppGateway = bool.Parse(configuration["EventHub:UseAppGateway"] ?? "false");
        string? appGatewayEndpoint = configuration["EventHub:AppGatewayEndpoint"];
        int appGatewayPort = int.Parse(configuration["EventHub:AppGatewayPort"] ?? "5672");

        Console.WriteLine("Starting Event Hub sender...");
        Console.WriteLine($"Event Hub Name: {eventHubName}");
        Console.WriteLine($"Using App Gateway: {useAppGateway}");

        try
        {
            if (useAppGateway && !string.IsNullOrEmpty(appGatewayEndpoint))
            {
                // Modify connection string to use App Gateway endpoint
                string modifiedConnectionString = ModifyConnectionStringForAppGateway(connectionString, appGatewayEndpoint, appGatewayPort);
                Console.WriteLine($"App Gateway Endpoint: {appGatewayEndpoint}:{appGatewayPort}");
                await SendEventsViaAppGateway(modifiedConnectionString, eventHubName);
            }
            else
            {
                await SendEventsDirectly(connectionString, eventHubName);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    static string ModifyConnectionStringForAppGateway(string originalConnectionString, string appGatewayEndpoint, int port)
    {
        // Parse the original connection string and replace the endpoint with App Gateway
        var match = Regex.Match(originalConnectionString, @"Endpoint=sb://([^/;]+)/");
        if (!match.Success)
        {
            throw new InvalidOperationException("Could not parse the Event Hub connection string endpoint");
        }

        string originalEndpoint = match.Groups[1].Value;
        string newEndpoint = $"{appGatewayEndpoint}:{port}";
        
        // Replace the endpoint in the connection string
        string modifiedConnectionString = originalConnectionString.Replace(
            $"Endpoint=sb://{originalEndpoint}/", 
            $"Endpoint=sb://{newEndpoint}/"
        );

        Console.WriteLine($"Original endpoint: {originalEndpoint}");
        Console.WriteLine($"App Gateway endpoint: {newEndpoint}");
        
        return modifiedConnectionString;
    }

    static async Task SendEventsDirectly(string connectionString, string eventHubName)
    {
        await using (var producerClient = new EventHubProducerClient(connectionString, eventHubName))
        {
            // Create a batch of events
            using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();

            // Add events to the batch
            var event1 = new EventData(Encoding.UTF8.GetBytes($"Event 1 - Timestamp: {DateTime.UtcNow}"));
            var event2 = new EventData(Encoding.UTF8.GetBytes($"Event 2 - Random value: {new Random().Next(1, 1000)}"));
            var event3 = new EventData(Encoding.UTF8.GetBytes($"Event 3 - Message: Hello from C# Event Hub sender!"));

            if (!eventBatch.TryAdd(event1))
            {
                throw new Exception("Event 1 is too large for the batch and cannot be sent.");
            }

            if (!eventBatch.TryAdd(event2))
            {
                throw new Exception("Event 2 is too large for the batch and cannot be sent.");
            }

            if (!eventBatch.TryAdd(event3))
            {
                throw new Exception("Event 3 is too large for the batch and cannot be sent.");
            }

            // Send the batch of events to the event hub
            await producerClient.SendAsync(eventBatch);
            Console.WriteLine($"Successfully sent {eventBatch.Count} events to Event Hub: {eventHubName} (Direct connection)");
        }
    }

    static async Task SendEventsViaAppGateway(string modifiedConnectionString, string eventHubName)
    {
        await using (var producerClient = new EventHubProducerClient(modifiedConnectionString, eventHubName))
        {
            // Create a batch of events
            using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();

            // Add events to the batch
            var event1 = new EventData(Encoding.UTF8.GetBytes($"Event 1 via App Gateway - Timestamp: {DateTime.UtcNow}"));
            var event2 = new EventData(Encoding.UTF8.GetBytes($"Event 2 via App Gateway - Random value: {new Random().Next(1, 1000)}"));
            var event3 = new EventData(Encoding.UTF8.GetBytes($"Event 3 via App Gateway - Message: Hello from C# Event Hub sender via App Gateway!"));

            if (!eventBatch.TryAdd(event1))
            {
                throw new Exception("Event 1 is too large for the batch and cannot be sent.");
            }

            if (!eventBatch.TryAdd(event2))
            {
                throw new Exception("Event 2 is too large for the batch and cannot be sent.");
            }

            if (!eventBatch.TryAdd(event3))
            {
                throw new Exception("Event 3 is too large for the batch and cannot be sent.");
            }

            // Send the batch of events to the event hub through App Gateway
            await producerClient.SendAsync(eventBatch);
            Console.WriteLine($"Successfully sent {eventBatch.Count} events to Event Hub: {eventHubName} (via App Gateway on port 5671)");
        }
    }
}
