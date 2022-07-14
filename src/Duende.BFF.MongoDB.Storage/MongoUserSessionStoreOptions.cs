using MongoDB.Driver;

namespace Duende.Bff.MongoDB.Storage;

public sealed class MongoUserSessionStoreOptions
{
	public string UserSessionCollectionName { get; set; } = "UserSessions";

	public IMongoDatabase Database { get; set; }
}
