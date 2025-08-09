using ClivoxApp.EventSourcingInfrastucture;

namespace ClivoxApp.Models.Auth.Events;

/// <summary>
/// Event fired when a user is created
/// </summary>
public record UserCreated(
    Guid Id,
    string Username,
    string Email,
    string PasswordHash,
    string Salt,
    string FirstName,
    string LastName
) : DomainEvent;

/// <summary>
/// Event fired when a user logs in successfully
/// </summary>
public record UserLoggedIn(
    Guid UserId,
    DateTime LoginTime,
    string IpAddress
) : DomainEvent;

/// <summary>
/// Event fired when a user logs out
/// </summary>
public record UserLoggedOut(
    Guid UserId,
    DateTime LogoutTime
) : DomainEvent;

/// <summary>
/// Event fired when a user's password is changed
/// </summary>
public record UserPasswordChanged(
    Guid UserId,
    string NewPasswordHash,
    string NewSalt
) : DomainEvent;

/// <summary>
/// Event fired when a user account is locked
/// </summary>
public record UserAccountLocked(
    Guid UserId,
    DateTime LockedUntil,
    string Reason
) : DomainEvent;

/// <summary>
/// Event fired when a user account is unlocked
/// </summary>
public record UserAccountUnlocked(
    Guid UserId
) : DomainEvent;