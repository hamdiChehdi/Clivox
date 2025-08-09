using ClivoxApp.Models.Auth.Events;
using JasperFx.Events;
using Marten.Events.Aggregation;

namespace ClivoxApp.Models.Auth;

public class UserProjection : SingleStreamProjection<User, Guid>
{
    public void Apply(UserCreated @event, User user)
    {
        user.Id = @event.Id;
        user.Username = @event.Username;
        user.Email = @event.Email;
        user.PasswordHash = @event.PasswordHash;
        user.Salt = @event.Salt;
        user.FirstName = @event.FirstName;
        user.LastName = @event.LastName;
        user.IsActive = true;
        user.FailedLoginAttempts = 0;
    }

    public void Apply(UserLoggedIn @event, User user)
    {
        user.LastLoginAt = @event.LoginTime;
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
    }

    public void Apply(UserPasswordChanged @event, User user)
    {
        user.PasswordHash = @event.NewPasswordHash;
        user.Salt = @event.NewSalt;
    }

    public void Apply(UserAccountLocked @event, User user)
    {
        user.LockedUntil = @event.LockedUntil;
    }

    public void Apply(UserAccountUnlocked @event, User user)
    {
        user.LockedUntil = null;
        user.FailedLoginAttempts = 0;
    }

    public override User ApplyMetadata(User user, IEvent lastEvent)
    {
        if (user.CreatedOn == default)
        {
            user.CreatedOn = lastEvent.Timestamp.UtcDateTime;
        }
        user.ModifiedOn = lastEvent.Timestamp.UtcDateTime;
        return user;
    }
}