using System.Linq.Expressions;
using LinqKit;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Duende.Bff.MongoDB.Storage;

internal sealed class MongoUserSessionStore : IUserSessionStore, IUserSessionStoreCleanup
{
	private readonly string _applicationDiscriminator;
	private readonly IMongoCollection<UserSessionEntity> _collection;

	public MongoUserSessionStore(MongoUserSessionStoreOptions userOptions, IOptions<DataProtectionOptions> dataOptions)
	{
		_applicationDiscriminator = dataOptions.Value.ApplicationDiscriminator;
		_collection = userOptions.Database.GetCollection<UserSessionEntity>(userOptions.UserSessionCollectionName);
	}

	public Task CreateUserSessionAsync(UserSession session, CancellationToken cancellationToken = default)
	{
		var item = new UserSessionEntity
		{
			ApplicationName = _applicationDiscriminator
		};
		session.CopyTo(item);
		return _collection.InsertOneAsync(item, cancellationToken: cancellationToken);
	}

	public Task UpdateUserSessionAsync(string key, UserSessionUpdate session, CancellationToken cancellationToken = default) =>
		_collection.UpdateOneAsync(us => us.ApplicationName == _applicationDiscriminator && us.Key == key, Builders<UserSessionEntity>.Update
				.Set(us => us.Created, session.Created)
				.Set(us => us.Expires, session.Expires)
				.Set(us => us.Renewed, session.Renewed)
				.Set(us => us.SessionId, session.SessionId)
				.Set(us => us.SubjectId, session.SubjectId)
				.Set(us => us.Ticket, session.Ticket),
			cancellationToken: cancellationToken);

	public Task DeleteUserSessionAsync(string key, CancellationToken cancellationToken = default) =>
		_collection.DeleteOneAsync(us => us.ApplicationName == _applicationDiscriminator && us.Key == key, cancellationToken: cancellationToken);

	public Task DeleteExpiredSessionsAsync(CancellationToken cancellationToken = default) =>
		_collection.DeleteManyAsync(us => us.Expires < DateTime.UtcNow, cancellationToken: cancellationToken);

	public Task DeleteUserSessionsAsync(UserSessionsFilter filter, CancellationToken cancellationToken = default) =>
		_collection.DeleteManyAsync(ToPredicate(filter), cancellationToken: cancellationToken);

	public Task<UserSession> GetUserSessionAsync(string key, CancellationToken cancellationToken = default) =>
		_collection
			.AsQueryable()
			.Where(us => us.ApplicationName == _applicationDiscriminator && us.Key == key)
			.Select(GetProjectionExpression())
			.SingleOrDefaultAsync(cancellationToken);
	public async Task<IReadOnlyCollection<UserSession>> GetUserSessionsAsync(UserSessionsFilter filter, CancellationToken cancellationToken = default) =>
		await _collection
			.AsQueryable()
			.Where(ToPredicate(filter))
			.Select(GetProjectionExpression())
			.ToListAsync(cancellationToken);

	private Expression<Func<UserSessionEntity, bool>> ToPredicate(UserSessionsFilter filter)
	{
		// First validate the filter can be used
		filter.Validate();
		Expression<Func<UserSessionEntity, bool>> exp = default;
		var builder = PredicateBuilder.New<UserSessionEntity>(us => us.ApplicationName == _applicationDiscriminator);
		if (!string.IsNullOrWhiteSpace(filter.SubjectId))
			exp = builder.And(us => us.SubjectId == filter.SubjectId);
		if (!string.IsNullOrWhiteSpace(filter.SessionId))
			exp = builder.And(us => us.SessionId == filter.SessionId);
		return exp.Expand();
	}

	private static Expression<Func<UserSessionEntity, UserSession>> GetProjectionExpression() =>
		us => new UserSession
		{
			Created = us.Created,
			Expires = us.Expires,
			Key = us.Key,
			Renewed = us.Renewed,
			SessionId = us.SessionId,
			SubjectId = us.SubjectId
		};
}
