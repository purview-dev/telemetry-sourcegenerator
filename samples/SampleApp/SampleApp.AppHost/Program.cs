using Scalar.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

Console.Title = "Aspire: Purview Telemetry Sample App";

// Add the API service
var apiService = builder
	.AddProject<Projects.SampleApp_APIService>("api-service")
	.WithExternalHttpEndpoints();

// Add the Blazor Web frontend with reference to the API service
// This enables service discovery - the Web app can call "https+http://api-service"
builder
	.AddProject<Projects.SampleApp_Web>("web")
	.WithExternalHttpEndpoints()
	.WithReference(apiService)
	.WaitFor(apiService);

var scalar = builder.AddScalarApiReference();

scalar.WithApiReference(apiService);

await builder.Build().RunAsync();
