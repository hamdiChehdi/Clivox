using ClivoxApp.Models.Auth;

namespace ClivoxApp.Services;

/// <summary>
/// Authentication service for managing user sessions and login state
/// </summary>
public class AuthenticationService
{
    private readonly UserRepository _userRepository;
    private User? _currentUser;
    private AuthSession? _currentSession;

    public AuthenticationService(UserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <summary>
    /// Event triggered when authentication state changes
    /// </summary>
    public event Action<bool>? AuthenticationStateChanged;

    /// <summary>
    /// Gets the current authenticated user
    /// </summary>
    public User? CurrentUser => _currentUser;

    /// <summary>
    /// Gets the current authenticated user name
    /// </summary>
    public string? CurrentUserName => _currentUser?.FullName;

    /// <summary>
    /// Gets whether a user is currently authenticated
    /// </summary>
    public bool IsAuthenticated => _currentUser != null && _currentSession != null && !_currentSession.IsExpired;

    /// <summary>
    /// Attempts to log in a user with username and password
    /// </summary>
    public async Task<LoginResult> LoginAsync(string username, string password, bool rememberMe = false)
    {
        try
        {
            var user = await _userRepository.GetUserByUsernameAsync(username);

            if (user == null)
            {
                return new LoginResult
                {
                    Success = false,
                    Message = "Invalid username or password."
                };
            }

            if (!user.IsActive)
            {
                return new LoginResult
                {
                    Success = false,
                    Message = "Account is deactivated. Please contact support."
                };
            }

            if (user.IsLocked)
            {
                return new LoginResult
                {
                    Success = false,
                    Message = $"Account is locked until {user.LockedUntil:yyyy-MM-dd HH:mm}."
                };
            }

            if (!user.VerifyPassword(password))
            {
                await _userRepository.IncrementFailedLoginAttemptsAsync(user.Id);
                return new LoginResult
                {
                    Success = false,
                    Message = "Invalid username or password."
                };
            }

            // Create session
            var session = new AuthSession
            {
                UserId = user.Id,
                Token = GenerateSessionToken(),
                ExpiresAt = DateTime.UtcNow.AddHours(rememberMe ? 24 * 7 : 8) // 7 days if remember me, 8 hours otherwise
            };

            await _userRepository.RecordSuccessfulLoginAsync(user.Id);
            await StoreSessionAsync(session, rememberMe);

            _currentUser = user;
            _currentSession = session;

            AuthenticationStateChanged?.Invoke(true);

            return new LoginResult
            {
                Success = true,
                Message = "Login successful.",
                User = user,
                Token = session.Token
            };
        }
        catch (Exception ex)
        {
            return new LoginResult
            {
                Success = false,
                Message = "An error occurred during login. Please try again."
            };
        }
    }

    /// <summary>
    /// Logs out the current user
    /// </summary>
    public async Task LogoutAsync()
    {
        if (_currentUser != null)
        {
            await _userRepository.RecordLogoutAsync(_currentUser.Id);
        }

        await ClearSessionAsync();

        _currentUser = null;
        _currentSession = null;

        AuthenticationStateChanged?.Invoke(false);
    }

    /// <summary>
    /// Attempts to restore session from secure storage
    /// </summary>
    public async Task<bool> TryRestoreSessionAsync()
    {
        try
        {
            var token = await SecureStorage.Default.GetAsync("auth_token");
            var userIdStr = await SecureStorage.Default.GetAsync("user_id");
            var expiresAtStr = await SecureStorage.Default.GetAsync("session_expires");

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userIdStr) || string.IsNullOrEmpty(expiresAtStr))
            {
                return false;
            }

            if (!Guid.TryParse(userIdStr, out var userId) || !DateTime.TryParse(expiresAtStr, out var expiresAt))
            {
                await ClearSessionAsync();
                return false;
            }

            if (DateTime.UtcNow > expiresAt)
            {
                await ClearSessionAsync();
                return false;
            }

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null || !user.IsActive || user.IsLocked)
            {
                await ClearSessionAsync();
                return false;
            }

            _currentUser = user;
            _currentSession = new AuthSession
            {
                UserId = userId,
                Token = token,
                ExpiresAt = expiresAt
            };

            AuthenticationStateChanged?.Invoke(true);
            return true;
        }
        catch
        {
            await ClearSessionAsync();
            return false;
        }
    }

    /// <summary>
    /// Changes the current user's password
    /// </summary>
    public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        if (_currentUser == null)
            return false;

        if (!_currentUser.VerifyPassword(currentPassword))
            return false;

        await _userRepository.UpdatePasswordAsync(_currentUser.Id, newPassword);
        return true;
    }

    /// <summary>
    /// Locks the current session (logs out but maintains user state for quick re-authentication)
    /// </summary>
    public async Task LockSessionAsync()
    {
        await SecureStorage.Default.SetAsync("session_locked", "true");
        AuthenticationStateChanged?.Invoke(false);
    }

    /// <summary>
    /// Checks if session is locked
    /// </summary>
    public async Task<bool> IsSessionLockedAsync()
    {
        var locked = await SecureStorage.Default.GetAsync("session_locked");
        return locked == "true";
    }

    /// <summary>
    /// Unlocks the session with password verification
    /// </summary>
    public async Task<bool> UnlockSessionAsync(string password)
    {
        if (_currentUser == null || !_currentUser.VerifyPassword(password))
            return false;

        SecureStorage.Default.Remove("session_locked");
        AuthenticationStateChanged?.Invoke(true);
        return true;
    }

    /// <summary>
    /// Stores session information securely
    /// </summary>
    private async Task StoreSessionAsync(AuthSession session, bool persistent)
    {
        await SecureStorage.Default.SetAsync("auth_token", session.Token);
        await SecureStorage.Default.SetAsync("user_id", session.UserId.ToString());
        await SecureStorage.Default.SetAsync("session_expires", session.ExpiresAt.ToString("O"));
        await SecureStorage.Default.SetAsync("session_persistent", persistent.ToString());
    }

    /// <summary>
    /// Clears stored session information
    /// </summary>
    private async Task ClearSessionAsync()
    {
        SecureStorage.Default.Remove("auth_token");
        SecureStorage.Default.Remove("user_id");
        SecureStorage.Default.Remove("session_expires");
        SecureStorage.Default.Remove("session_persistent");
        SecureStorage.Default.Remove("session_locked");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Generates a secure session token
    /// </summary>
    private string GenerateSessionToken()
    {
        var bytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}