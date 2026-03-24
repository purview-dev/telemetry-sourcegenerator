var builder = DistributedApplication.CreateBuilder(args);

if (Environment.UserInteractive)
	Console.Title = "Aspire: Purview Telemetry Sample App";

// Add the API service
var apiService = builder.AddProject<Projects.SampleApp_APIService>("api-service");

// Add the Blazor Web frontend with reference to the API service
builder.AddProject<Projects.SampleApp_Web>("web").WithReference(apiService).WaitFor(apiService);

var app = builder.Build();

await app.RunAsync();
