namespace Purview.Telemetry.SourceGenerator;

partial class TelemetrySourceGeneratorTests
{
	[Test]
	public async Task Generate_GivenICacheServiceProviderTelemetry_GeneratesTelemetry(
		CancellationToken cancellationToken
	)
	{
		// Arrange
		const string basicTelemetry = """

using System.Diagnostics;

namespace Purview.Interfaces.ApplicationServices.Caching;

[ActivitySource]
[Logger]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate")]
public interface ICacheServiceProviderTelemetry
{
	[Log]
	void FailedToDeserializePayload(int dataLength, Exception ex);

	[Log]
	void FailedToGetFromCache(string key, Exception ex);

	[Log]
	void FailedToRefresh(string cacheKey, Exception ex);

	[Log]
	void FailedToRemove(string key, Exception ex);

	[Log]
	void FailedToSerializePayload(string? fullName, Exception ex);

	[Log]
	void FailedToSetValueInCache(string key, Exception ex);

	[Log]
	void UsingDistributedCache(string? fullName, bool isNullCache);

	[Activity(ActivityKind.Client)]
	Activity? GetFromCache();

	[Event]
	void NoValueProvided();

	[Activity(ActivityKind.Internal)]
	Activity? SerializePayload();

	[Context]
	void SerializePayloadResult(int payloadStringLength);

	[Activity(ActivityKind.Client)]
	Activity? SetInCache();

	[Context]
	void SetDefaultTags(string distributedCacheType, string cacheKey, string? entityType);

	[Event]
	void ValueCached();

	[Event]
	void RequestingValueFromCache();

	[Event]
	void CacheHit(int? dataLength);

	[Event]
	void CacheMiss();

	[Activity(ActivityKind.Internal)]
	Activity? DeserializePayload();

	[Activity(ActivityKind.Client)]
	Activity? Refresh();

	[Activity(ActivityKind.Client)]
	Activity? Remove();
}

""";

		// Act
		var generationResult = await GenerateAsync(
			basicTelemetry,
			GenerateDependencyInjection(),
			cancellationToken: cancellationToken
		);

		// Assert
		await Assert.That(generationResult).HasNoErrorDiagnostics();

		var query = generationResult.Generated();
		await Assert
			.That(query.HasMethod("GetFromCache"))
			.IsTrue()
			.Because("the generated class must contain the activity method");
		await Assert
			.That(query.HasMethod("SerializePayload"))
			.IsTrue()
			.Because("the generated class must contain the serialize-payload activity method");
		await Assert
			.That(query.HasMethod("FailedToGetFromCache"))
			.IsTrue()
			.Because("the generated class must contain the log method");
		await Assert
			.That(query.HasMethod("CacheHit"))
			.IsTrue()
			.Because("the generated class must contain the cache-hit event method");
	}
}
