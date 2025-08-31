using System.ComponentModel;
using System.Text;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi.Models;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.Configuration;

#pragma warning restore IDE0130 // Namespace does not match folder structure

[EditorBrowsable(EditorBrowsableState.Always)]
static class OpenApiOptionsExtensions
{
	public static OpenApiOptions ApplyAPIVersionInfo(
		this OpenApiOptions options,
		string title,
		string description
	)
	{
		options.AddDocumentTransformer(
			(document, context, cancellationToken) =>
			{
				var versionedDescriptionProvider =
					context.ApplicationServices.GetService<IApiVersionDescriptionProvider>();
				var apiDescription =
					versionedDescriptionProvider?.ApiVersionDescriptions.SingleOrDefault(
						description => description.GroupName == context.DocumentName
					);

				if (apiDescription is not null)
				{
					document.Info.Version = apiDescription.ApiVersion.ToString();
					document.Info.Title = title;
					document.Info.Description = BuildDescription(apiDescription, description);
				}

				return Task.CompletedTask;
			}
		);

		return options;
	}

	static string BuildDescription(ApiVersionDescription api, string description)
	{
		StringBuilder text = new(description);
		if (api.IsDeprecated)
		{
			if (text.Length > 0)
			{
				if (text[^1] != '.')
					text.Append('.');

				text.Append(' ');
			}

			text.Append("This API version has been deprecated.");
		}

		if (api.SunsetPolicy is { } policy)
		{
			if (policy.Date is { } when)
			{
				if (text.Length > 0)
					text.Append(' ');

				text.Append("The API will be sunset on ")
					.Append(when.Date.ToShortDateString())
					.Append('.');
			}

			if (policy.HasLinks)
			{
				text.AppendLine();

				var rendered = false;

				foreach (var link in policy.Links.Where(l => l.Type == "text/html"))
				{
					if (!rendered)
					{
						text.Append("<h4>Links</h4><ul>");
						rendered = true;
					}

					text.Append("<li><a href=\"");
					text.Append(link.LinkTarget.OriginalString);
					text.Append("\">");
					text.Append(
						StringSegment.IsNullOrEmpty(link.Title)
							? link.LinkTarget.OriginalString
							: link.Title.ToString()
					);
					text.Append("</a></li>");
				}

				if (rendered)
					text.Append("</ul>");
			}
		}

		return text.ToString();
	}

	public static OpenApiOptions ApplySecuritySchemeDefinitions(this OpenApiOptions options) =>
		options.AddDocumentTransformer<SecuritySchemeDefinitionsTransformer>();

	public static OpenApiOptions ApplyAuthorizationChecks(
		this OpenApiOptions options,
		string[] scopes
	)
	{
		options.AddOperationTransformer(
			(operation, context, cancellationToken) =>
			{
				var metadata = context.Description.ActionDescriptor.EndpointMetadata;
				if (!metadata.OfType<IAuthorizeData>().Any())
					return Task.CompletedTask;

				operation.Responses.TryAdd(
					"401",
					new OpenApiResponse { Description = "Unauthorized" }
				);
				operation.Responses.TryAdd(
					"403",
					new OpenApiResponse { Description = "Forbidden" }
				);

				OpenApiSecurityScheme oAuthScheme = new()
				{
					Reference = new() { Type = ReferenceType.SecurityScheme, Id = "oauth2" },
				};

				operation.Security = [new() { [oAuthScheme] = scopes }];

				return Task.CompletedTask;
			}
		);
		return options;
	}

	public static OpenApiOptions ApplyOperationDeprecatedStatus(this OpenApiOptions options)
	{
		options.AddOperationTransformer(
			(operation, context, cancellationToken) =>
			{
				var apiDescription = context.Description;
				operation.Deprecated |= apiDescription.IsDeprecated();

				return Task.CompletedTask;
			}
		);

		return options;
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage(
		"Performance",
		"CA1812:Avoid uninstantiated internal classes",
		Justification = "Instantiated via AddDocumentTransformer<T>"
	)]
	private sealed class SecuritySchemeDefinitionsTransformer(IConfiguration configuration)
		: IOpenApiDocumentTransformer
	{
		public Task TransformAsync(
			OpenApiDocument document,
			OpenApiDocumentTransformerContext context,
			CancellationToken cancellationToken
		)
		{
			var identitySection = configuration.GetSection("Identity");
			if (!identitySection.Exists())
				return Task.CompletedTask;

			var identityUrlExternal = identitySection.GetRequiredValue("Url");
			var scopes = identitySection
				.GetRequiredSection("Scopes")
				.GetChildren()
				.ToDictionary(p => p.Key, p => p.Value);
			OpenApiSecurityScheme securityScheme = new()
			{
				Type = SecuritySchemeType.OAuth2,
				Flows = new()
				{
					// TODO: Change this to use Authorization Code flow with PKCE
					Implicit = new()
					{
						AuthorizationUrl = new Uri($"{identityUrlExternal}/connect/authorize"),
						TokenUrl = new Uri($"{identityUrlExternal}/connect/token"),
						Scopes = scopes,
					},
				},
			};

			document.Components ??= new();
			document.Components.SecuritySchemes.Add("oauth2", securityScheme);

			return Task.CompletedTask;
		}
	}
}
