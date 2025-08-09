using ClivoxApp.EventSourcingInfrastucture;
using ClivoxApp.Models.Auth;
using ClivoxApp.Models.Auth.Events;
using Marten;
using Microsoft.Extensions.Logging;

namespace ClivoxApp.Models.Auth;

public class UserRepository
{
    private readonly ILogger _logger;
    private readonly IQuerySession _querySession;
    private readonly IDocumentStore _documentStore;

    public UserRepository(IQuerySession querySession, IDocumentStore documentStore, ILogger<UserRepository> logger)
    {
        _logger = logger;
        _querySession = querySession;
        _documentStore = documentStore;
    }

    public async Task<User?> GetUserByIdAsync(Guid id)
    {
        _logger.LogInformation("Fetching user with ID: {UserId}", id);
        return await _querySession.LoadAsync<User>(id);
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        _logger.LogInformation("Fetching user with username: {Username}", username);
        return await _querySession.Query<User>()
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        _logger.LogInformation("Fetching user with email: {Email}", email);
        return await _querySession.Query<User>()
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task CreateUserAsync(User user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));

        _logger.LogInformation("Creating new user: {Username}", user.Username);

        var evt = new UserCreated(
            user.Id,
            user.Username,
            user.Email,
            user.PasswordHash,
            user.Salt,
            user.FirstName,
            user.LastName);

        using var session = _documentStore.LightweightSession();
        session.StoreEvents<User>(null, evt, null);
        await session.SaveChangesAsync();
    }

    public async Task RecordSuccessfulLoginAsync(Guid userId)
    {
        _logger.LogInformation("Recording successful login for user: {UserId}", userId);

        var evt = new UserLoggedIn(userId, DateTime.UtcNow, "local");

        using var session = _documentStore.LightweightSession();
        session.StoreEvents<User>(userId, evt, null);
        await session.SaveChangesAsync();
    }

    public async Task RecordLogoutAsync(Guid userId)
    {
        _logger.LogInformation("Recording logout for user: {UserId}", userId);

        var evt = new UserLoggedOut(userId, DateTime.UtcNow);

        using var session = _documentStore.LightweightSession();
        session.StoreEvents<User>(userId, evt, null);
        await session.SaveChangesAsync();
    }

    public async Task IncrementFailedLoginAttemptsAsync(Guid userId)
    {
        _logger.LogInformation("Incrementing failed login attempts for user: {UserId}", userId);

        var user = await GetUserByIdAsync(userId);
        if (user != null)
        {
            user.IncrementFailedLoginAttempts();

            if (user.IsLocked)
            {
                var evt = new UserAccountLocked(userId, user.LockedUntil!.Value, "Too many failed login attempts");
                using var session = _documentStore.LightweightSession();
                session.StoreEvents<User>(userId, evt, null);
                await session.SaveChangesAsync();
            }
        }
    }

    public async Task UpdatePasswordAsync(Guid userId, string newPassword)
    {
        _logger.LogInformation("Updating password for user: {UserId}", userId);

        var user = await GetUserByIdAsync(userId);
        if (user != null)
        {
            user.UpdatePassword(newPassword);

            var evt = new UserPasswordChanged(userId, user.PasswordHash, user.Salt);
            using var session = _documentStore.LightweightSession();
            session.StoreEvents<User>(userId, evt, null);
            await session.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Creates a default admin user if no users exist (for initial setup)
    /// </summary>
    public async Task EnsureDefaultUserAsync()
    {
        var existingUsers = await _querySession.Query<User>().CountAsync();
        if (existingUsers == 0)
        {
            _logger.LogInformation("Creating default admin user");

            var defaultUser = User.Create(
                "admin",
                "admin@clivox.com",
                "Admin123!",
                "Administrator",
                "User"
            );

            await CreateUserAsync(defaultUser);
        }
    }
}