using MongoDB.Bson;
using MongoDB.Driver;

namespace Duende.Bff.MongoDB.Storage;

public sealed class DatabaseInitializer
{
	public async Task InitializeAsync(MongoUserSessionStoreOptions options, CancellationToken cancellationToken = default)
	{
		var doc = await (await options.Database.ListCollectionsAsync(new ListCollectionsOptions
		{
			Filter = new BsonDocument("name", options.UserSessionCollectionName)
		}, cancellationToken)).SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

		// If a BsonDocument is returned the collection exists to return out
		if (doc is not null) return;

		// Step 1) Create the collection
		await options.Database.CreateCollectionAsync(options.UserSessionCollectionName, new()
		{
			Collation = new Collation("en_US", strength: CollationStrength.Secondary)
		}, cancellationToken).ConfigureAwait(false);

		// Step 2) Create the indexes to match: https://github.com/DuendeSoftware/BFF/blob/main/src/Duende.Bff.EntityFramework/SessionDbContext.cs#L46
		var ixBuilder = Builders<UserSessionEntity>.IndexKeys;
		var indexes = options.Database.GetCollection<UserSessionEntity>(options.UserSessionCollectionName).Indexes;
		await indexes.CreateManyAsync(
			new CreateIndexModel<UserSessionEntity>[]
			{
				new(ixBuilder.Ascending(us => us.ApplicationName).Ascending(us => us.Key), new CreateIndexOptions { Background = true, Name = "ix_key", Unique = true }),
				new(ixBuilder.Ascending(us => us.ApplicationName).Ascending(us => us.SubjectId).Ascending(us => us.SessionId), new CreateIndexOptions { Background = true, Name = "ix_subjectId_sessionId", Unique = true }),
				new(ixBuilder.Ascending(us => us.ApplicationName).Ascending(us => us.SessionId), new CreateIndexOptions { Background = true, Name = "ix_sessionId", Unique = true }),
				new(ixBuilder.Ascending(us => us.Expires), new CreateIndexOptions { Background = true, Name = "ix_expires", Sparse = true }),
			}, cancellationToken).ConfigureAwait(false);
	}
}
