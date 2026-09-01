namespace Purview.Aspire.ResourceKit.PipelineCLI.Helpers;

static class TestHelpers
{
	public static string BuildTUnitTreeNodeFilter(
		string? assembly = null,
		string? @namespace = null,
		string? className = null,
		string? testNameQuery = null
	)
	{
		var filter = "/";
		filter += assembly switch
		{
			null => "*",
			_ => assembly,
		};

		filter += @namespace switch
		{
			null => "*",
			_ => @namespace,
		};

		filter += className switch
		{
			null => "*",
			_ => className,
		};

		filter += testNameQuery switch
		{
			null => "*",
			_ => testNameQuery,
		};

		return filter;
	}
}
