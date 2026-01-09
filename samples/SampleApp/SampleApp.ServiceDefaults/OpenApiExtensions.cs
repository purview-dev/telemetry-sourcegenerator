using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.AspNetCore.Builder;

#pragma warning restore IDE0130 // Namespace does not match folder structure

[EditorBrowsable(EditorBrowsableState.Never)]
public static class OpenApiExtensions
{
	public static IApplicationBuilder UseDefaultOpenAPI(
		[NotNull] this WebApplication app,
		bool throwOnMissing = true
	)
	{
		var configuration = app.Configuration;
		var openApiSection = configuration.GetSection("OpenAPI");

		if (!openApiSection.Exists())
		{
			return throwOnMissing
				? throw new InvalidOperationException("OpenAPI configuration section is missing.")
				: app;
		}

		app.MapOpenApi();

		if (app.Environment.IsDevelopment())
		{
			app.MapScalarApiReference();
			app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
		}

		return app;
	}

	public static IHostApplicationBuilder AddDefaultOpenAPI(
		[NotNull] this IHostApplicationBuilder builder,
		IApiVersioningBuilder? apiVersioning = default,
		bool throwOnMissing = true
	)
	{
		var openApiSection = builder.Configuration.GetSection("OpenAPI");
		if (!openApiSection.Exists())
		{
			return throwOnMissing
				? throw new InvalidOperationException("OpenAPI configuration section is missing.")
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
