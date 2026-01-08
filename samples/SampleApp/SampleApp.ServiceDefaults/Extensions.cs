using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.Hosting;

#pragma warning restore IDE0130 // Namespace does not match folder structure

[EditorBrowsable(EditorBrowsableState.Never)]
public static class Extensions
{
	public static IHostApplicationBuilder AddServiceDefaults(
		[NotNull] this IHostApplicationBuilder builder
	)
	{
		builder.ConfigureOpenTelemetry();

		builder.AddDefaultHealthChecks();

		builder.Services.AddServiceDiscovery();

		builder.Services.ConfigureHttpClientDefaults(http =>
		{
			// Turn on resilience by default
			http.AddStandardResilienceHandler();

			// Turn on service discovery by default
			http.AddServiceDiscovery();
		});

		return builder;
	}

	public static IHostApplicationBuilder ConfigureOpenTelemetry(
		[NotNull] this IHostApplicationBuilder builder
	)
	{
		builder.Logging.AddOpenTelemetry(logging =>
		{
			logging.IncludeFormattedMessage = true;
			logging.IncludeScopes = true;
		});

		builder
			.Services.AddOpenTelemetry()
			.WithMetrics(metrics =>
				metrics
					.AddAspNetCoreInstrumentation()
					.AddHttpClientInstrumentation()
					.AddProcessInstrumentation()
					.AddRuntimeInstrumentation()
					.AddMeter(
						[
							"WeatherServiceTelemetry", // This is the name of the meter
						]
					)
			)
			.WithTracing(tracing =>
			{
				if (builder.Environment.IsDevelopment())
				{
					// We want to view all traces in development
					tracing.SetSampler(new AlwaysOnSampler());
				}

				tracing
					.AddAspNetCoreInstrumentation()
					.AddGrpcClientInstrumentation()
					.AddHttpClientInstrumentation()
					.AddSource(
						[
							"sample-weather-app", // This is the name of the activity source
						]
					);
			});

		builder.AddOpenTelemetryExporters();

		return builder;
	}

	static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
	{
		var useOtlpExporter = !string.IsNullOrWhiteSpace(
			builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
		);

		if (useOtlpExporter)
		{
			builder.Services.Configure<OpenTelemetryLoggerOptions>(logging =>
				logging.AddOtlpExporter()
			);
			builder.Services.ConfigureOpenTelemetryMeterProvider(metrics =>
				metrics.AddOtlpExporter()
			);
			builder.Services.ConfigureOpenTelemetryTracerProvider(tracing =>
				tracing.AddOtlpExporter()
			);
		}

		// Uncomment the following lines to enable the Prometheus exporter (requires the OpenTelemetry.Exporter.Prometheus.AspNetCore package)
		// builder.Services.AddOpenTelemetry()
		//    .WithMetrics(metrics => metrics.AddPrometheusExporter());

		// Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
		//if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
		//{
		//    builder.Services.AddOpenTelemetry()
		//       .UseAzureMonitor();
		//}

		return builder;
	}

	public static IHostApplicationBuilder AddDefaultHealthChecks(
		[NotNull] this IHostApplicationBuilder builder
	)
	{
		builder
			.Services.AddHealthChecks()
			// Add a default liveness check to ensure app is responsive
			.AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

		return builder;
	}

	public static WebApplication MapDefaultEndpoints(this WebApplication app)
	{
		// Uncomment the following line to enable the Prometheus endpoint (requires the OpenTelemetry.Exporter.Prometheus.AspNetCore package)
		// app.MapPrometheusScrapingEndpoint();

		// All health checks must pass for app to be considered ready to accept traffic after starting
		app.MapHealthChecks("/health");

		// Only health checks tagged with the "live" tag must pass for app to be considered alive
		app.MapHealthChecks(
			"/alive",
			new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") }
		);

		return app;
	}
}
