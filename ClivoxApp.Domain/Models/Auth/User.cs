using ClivoxApp.EventSourcingInfrastucture;
using System.Security.Cryptography;
using System.Text;

namespace ClivoxApp.Models.Auth;

public class User : IAggregateRoot
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }

    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime LastLoginAt { get; set; }
    public DateTime? LockedUntil { get; set; }
    public int FailedLoginAttempts { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Creates a new user with hashed password
    /// </summary>
    public static User Create(string username, string email, string password, string firstName, string lastName)
    {
        var salt = GenerateSalt();
        var passwordHash = HashPassword(password, salt);

        return new User
        {
            Id = Guid.CreateVersion7(),
            Username = username,
            Email = email,
            PasswordHash = passwordHash,
            Salt = salt,
            FirstName = firstName,
            LastName = lastName,
            IsActive = true,
            CreatedOn = DateTime.UtcNow,
            ModifiedOn = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Verifies if the provided password matches the user's password
    /// </summary>
    public bool VerifyPassword(string password)
    {
        var hashedPassword = HashPassword(password, Salt);
        return hashedPassword == PasswordHash;
    }

    /// <summary>
    /// Updates the user's password
    /// </summary>
    public void UpdatePassword(string newPassword)
    {
        Salt = GenerateSalt();
        PasswordHash = HashPassword(newPassword, Salt);
        ModifiedOn = DateTime.UtcNow;
    }

    /// <summary>
    /// Locks the user account for a specified duration
    /// </summary>
    public void LockAccount(TimeSpan duration)
    {
        LockedUntil = DateTime.UtcNow.Add(duration);
        ModifiedOn = DateTime.UtcNow;
    }

    /// <summary>
    /// Unlocks the user account
    /// </summary>
    public void UnlockAccount()
    {
        LockedUntil = null;
        FailedLoginAttempts = 0;
        ModifiedOn = DateTime.UtcNow;
    }

    /// <summary>
    /// Increments failed login attempts
    /// </summary>
    public void IncrementFailedLoginAttempts()
    {
        FailedLoginAttempts++;
        ModifiedOn = DateTime.UtcNow;

        // Lock account after 5 failed attempts for 15 minutes
        if (FailedLoginAttempts >= 5)
        {
            LockAccount(TimeSpan.FromMinutes(15));
        }
    }

    /// <summary>
    /// Resets failed login attempts and updates last login time
    /// </summary>
    public void RecordSuccessfulLogin()
    {
        FailedLoginAttempts = 0;
        LastLoginAt = DateTime.UtcNow;
        LockedUntil = null;
        ModifiedOn = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the account is currently locked
    /// </summary>
    public bool IsLocked => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;

    /// <summary>
    /// Generates a random salt for password hashing
    /// </summary>
    private static string GenerateSalt()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Hashes a password with the provided salt using PBKDF2
    /// </summary>
    private static string HashPassword(string password, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256);
        var hashBytes = pbkdf2.GetBytes(32);
        return Convert.ToBase64String(hashBytes);
    }
}

/// <summary>
/// Login request model
/// </summary>
public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}

/// <summary>
/// Login result
/// </summary>
public class LoginResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public User? User { get; set; }
    public string? Token { get; set; }
}

/// <summary>
/// Authentication session
/// </summary>
public class AuthSession
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}