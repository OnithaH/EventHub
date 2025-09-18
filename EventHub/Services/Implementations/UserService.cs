using EventHub.Data;
using EventHub.Models.Entities;
using EventHub.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BC = BCrypt.Net.BCrypt;

namespace EventHub.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserService> _logger;

        // BCrypt work factor (cost) - increase for higher security but slower hashing
        // 12 = 4,096 iterations (recommended for 2025)
        private const int BCryptWorkFactor = 12;

        public UserService(ApplicationDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return null;

                return await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email: {Email}", email);
                return null;
            }
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.OrganizedEvents.Where(e => e.IsActive))
                    .Include(u => u.Bookings.Where(b => b.Status != BookingStatus.Cancelled))
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", id);
                return null;
            }
        }

        public async Task<User> CreateUserAsync(User user)
        {
            try
            {
                if (user == null)
                    throw new ArgumentNullException(nameof(user));

                // Validate required fields
                if (string.IsNullOrWhiteSpace(user.Email))
                    throw new ArgumentException("Email is required", nameof(user));

                if (string.IsNullOrWhiteSpace(user.Password))
                    throw new ArgumentException("Password is required", nameof(user));

                if (string.IsNullOrWhiteSpace(user.Name))
                    throw new ArgumentException("Name is required", nameof(user));

                // Check if email already exists
                var existingUser = await GetUserByEmailAsync(user.Email);
                if (existingUser != null)
                    throw new InvalidOperationException("A user with this email already exists");

                // Validate and hash password
                if (!IsValidPassword(user.Password))
                    throw new ArgumentException("Password does not meet security requirements", nameof(user));

                user.Password = HashPassword(user.Password);
                user.Email = user.Email.Trim().ToLowerInvariant();
                user.Name = user.Name.Trim();
                user.Company = user.Company?.Trim();
                user.Phone = user.Phone?.Trim();
                user.CreatedAt = DateTime.UtcNow;
                user.IsActive = true;

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User created successfully: {UserId} - {Email}", user.Id, user.Email);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user with email: {Email}", user?.Email);
                throw;
            }
        }

        public async Task<bool> ValidateUserAsync(string email, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                    return false;

                var user = await GetUserByEmailAsync(email);
                if (user == null || !user.IsActive)
                    return false;

                // Verify password using BCrypt
                bool isValid = VerifyPassword(password, user.Password);

                if (isValid)
                {
                    _logger.LogInformation("Password validation successful for user: {UserId}", user.Id);

                    // Update last login time (optional)
                    await UpdateLastLoginAsync(user.Id);
                }
                else
                {
                    _logger.LogWarning("Password validation failed for user: {UserId}", user.Id);
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user: {Email}", email);
                return false;
            }
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            try
            {
                return await _context.Users
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.Name)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return Enumerable.Empty<User>();
            }
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                if (user == null)
                    return false;

                var existingUser = await _context.Users.FindAsync(user.Id);
                if (existingUser == null || !existingUser.IsActive)
                    return false;

                // Update only allowed fields (don't allow password update here)
                existingUser.Name = user.Name?.Trim() ?? existingUser.Name;
                existingUser.Phone = user.Phone?.Trim();
                existingUser.Company = user.Company?.Trim();
                existingUser.LoyaltyPoints = user.LoyaltyPoints;

                _context.Users.Update(existingUser);
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    _logger.LogInformation("User updated successfully: {UserId}", user.Id);
                }

                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", user?.Id);
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                    return false;

                // Soft delete - set IsActive to false
                user.IsActive = false;

                _context.Users.Update(user);
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    _logger.LogInformation("User soft deleted: {UserId}", id);
                }

                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", id);
                return false;
            }
        }

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role)
        {
            try
            {
                return await _context.Users
                    .Where(u => u.Role == role && u.IsActive)
                    .OrderBy(u => u.Name)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users by role: {Role}", role);
                return Enumerable.Empty<User>();
            }
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || !user.IsActive)
                    return false;

                // Verify current password
                if (!VerifyPassword(currentPassword, user.Password))
                    return false;

                // Validate new password
                if (!IsValidPassword(newPassword))
                    throw new ArgumentException("New password does not meet security requirements");

                // Hash and update password
                user.Password = HashPassword(newPassword);

                _context.Users.Update(user);
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
                }

                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> UpdateLoyaltyPointsAsync(int userId, int points)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null || !user.IsActive)
                    return false;

                user.LoyaltyPoints = Math.Max(0, user.LoyaltyPoints + points);

                _context.Users.Update(user);
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    _logger.LogInformation("Loyalty points updated for user {UserId}: {Points}", userId, points);
                }

                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating loyalty points for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> IsEmailAvailableAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return false;

                var existingUser = await GetUserByEmailAsync(email);
                return existingUser == null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email availability: {Email}", email);
                return false;
            }
        }

        // Private helper methods
        private static string HashPassword(string password)
        {
            try
            {
                // Generate salt and hash password using BCrypt
                return BC.HashPassword(password, BCryptWorkFactor);
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Failed to hash password");
            }
        }

        private static bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                // Verify password against hash using BCrypt
                return BC.Verify(password, hashedPassword);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            // Minimum security requirements for 2025:
            // - At least 6 characters (basic requirement)
            // - Contains at least one letter
            // - Contains at least one digit
            // For enhanced security, you could add:
            // - Special characters requirement
            // - Mixed case requirement
            // - Length requirement (8+ chars)

            if (password.Length < 6)
                return false;

            bool hasLetter = password.Any(char.IsLetter);
            bool hasDigit = password.Any(char.IsDigit);

            return hasLetter && hasDigit;
        }

        private async Task UpdateLastLoginAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    // Add LastLoginAt property to User model if needed
                    // user.LastLoginAt = DateTime.UtcNow;
                    // await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last login for user: {UserId}", userId);
                // Don't throw - this is not critical for user operations
            }
        }
    }
}