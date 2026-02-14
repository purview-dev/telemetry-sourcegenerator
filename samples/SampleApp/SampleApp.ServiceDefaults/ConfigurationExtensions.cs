using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Configuration;

public static class ConfigurationExtensions
{
	extension([NotNull] IConfiguration configuration)
	{
		public string GetRequiredValue(string name) =>
			configuration[name]
			?? throw new InvalidOperationException(
				$"Configuration missing value for: {(configuration is IConfigurationSection s ? s.Path + ":" + name : name)}"
			);
	}
}
