using Duende.Bff;
using Duende.Bff.MongoDB.Storage;
using Microsoft.AspNetCore.Builder;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for BffBuilder
/// </summary>
public static class BffBuilderExtensions
{
	/// <summary>
	/// Adds entity framework core support for user session store.
	/// </summary>
	/// <param name="bffBuilder"></param>
	/// <param name="action"></param>
	/// <returns></returns>
	public static BffBuilder AddMongoDbServerSideSessions(this BffBuilder bffBuilder, Action<MongoUserSessionStoreOptions> action)
	{
		var options = new MongoUserSessionStoreOptions();
		action(options);
		if (options.Database is null) throw new ArgumentNullException(nameof(options.Database), "Database must be configured");

		bffBuilder.Services
			.AddSingleton(options)
			.AddTransient<IUserSessionStoreCleanup, MongoUserSessionStore>();
		return bffBuilder.AddServerSideSessions<MongoUserSessionStore>();
	}
}
