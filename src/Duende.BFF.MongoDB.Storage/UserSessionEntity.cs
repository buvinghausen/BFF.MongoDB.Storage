namespace Duende.Bff.MongoDB.Storage;

public sealed class UserSessionEntity : UserSession
{
	/// <summary>
	/// Discriminator to allow multiple applications to share the user session table.
	/// </summary>
	public string ApplicationName { get; set; }
}
