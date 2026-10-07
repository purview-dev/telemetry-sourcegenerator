using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace Microsoft.AspNetCore.Builder;

public static class WebApplicationExtensions
{
	public static WebApplication MapDefaultEndpoints([NotNull] this WebApplication app)
	{
		// Uncomment the following line to enable the Prometheus endpoint (requires the OpenTelemetry.Exporter.Prometheus.AspNetCore package)
		// app.MapPrometheusScrapingEndpoint();

		// All health checks must pass for app to be considered ready to accept traffic after starting
		app.MapHealthChecks("/health");

		// Only health checks tagged with the "live" tag must pass for app to be considered alive
		app.MapHealthChecks("/alive", new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });

		UseDefaultOpenAPI(app);

		return app;
	}

	static void UseDefaultOpenAPI(WebApplication app)
	{
		var configuration = app.Configuration;
		var openApiSection = configuration.GetSection("OpenAPI");

		if (!openApiSection.Exists())
			return;

		app.MapOpenApi("v1");

		if (app.Environment.IsDevelopment())
		{
			app.MapScalarApiReference();
			app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
		}
	}
}
