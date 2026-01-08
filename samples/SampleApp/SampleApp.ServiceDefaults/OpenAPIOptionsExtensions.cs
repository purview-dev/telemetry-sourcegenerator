using System.ComponentModel;
using System.Text;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

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
			(document, context, _) =>
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
}
