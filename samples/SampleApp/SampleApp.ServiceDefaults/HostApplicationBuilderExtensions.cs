using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Asp.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class HostApplicationBuilderExtensions
{
	extension([NotNull] IHostApplicationBuilder builder)
	{
		public IHostApplicationBuilder AddServiceDefaults()
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

		public IHostApplicationBuilder ConfigureOpenTelemetry()
		{
			builder.Logging.AddOpenTelemetry(logging =>
			{
				logging.IncludeFormattedMessage = true;
				logging.IncludeScopes = true;
			});

			var metricsAssembly = Assembly.GetEntryAssembly()!.GetName().Name!;
			builder
				.Services.AddOpenTelemetry()
				.WithMetrics(metrics =>
					metrics
						.AddAspNetCoreInstrumentation()
						.AddHttpClientInstrumentation()
						.AddProcessInstrumentation()
						.AddRuntimeInstrumentation()
						.AddMeter([metricsAssembly])
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
						.AddSource([
							// These are set in the AssemblyInfo.cs	files for the API and Web projects.
							"sample-weather-app-api",
							"sample-weather-app-web",
						]);
				});

			builder.AddOpenTelemetryExporters();

			return builder;
		}

		public IHostApplicationBuilder AddDefaultHealthChecks()
		{
			builder
				.Services.AddHealthChecks()
				// Add a default liveness check to ensure app is responsive
				.AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

			return builder;
		}

		public IHostApplicationBuilder AddDefaultOpenAPI(
			IApiVersioningBuilder? apiVersioning = default,
			bool throwOnMissing = true
		)
		{
			var openApiSection = builder.Configuration.GetSection("OpenAPI");
			if (!openApiSection.Exists())
			{
				return throwOnMissing
					? throw new InvalidOperationException(
						"OpenAPI configuration section is missing."
					)
					: builder;
			}

			// the default format will just be ApiVersion.ToString(); for example, 1.0.
			// this will format the version as "'v'major[.minor][-status]"
			var versioned = apiVersioning?.AddApiExplorer(options =>
				options.GroupNameFormat = "'v'VVV"
			);
			string[] versions = ["v1"];
			foreach (var description in versions)
			{
				builder.Services.AddOpenApi(
					description,
					options =>
					{
						options.ApplyAPIVersionInfo(
							openApiSection.GetRequiredValue("Document:Title"),
							openApiSection.GetRequiredValue("Document:Description")
						);

						// Clear out the default servers so we can fallback to
						// whatever ports have been allocated for the service by Aspire
						options.AddDocumentTransformer(
							(document, _, _) =>
							{
								document.Servers = [];
								return Task.CompletedTask;
							}
						);
					}
				);
			}

			return builder;
		}
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
}
