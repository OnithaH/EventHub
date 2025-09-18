using EventHub.Models.Entities;

namespace EventHub.Services.Interfaces
{
    /// <summary>
    /// Interface for user management operations with enhanced security features
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Retrieves a user by their email address
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <returns>User object if found and active, null otherwise</returns>
        Task<User?> GetUserByEmailAsync(string email);

        /// <summary>
        /// Retrieves a user by their ID with related data
        /// </summary>
        /// <param name="id">User's unique identifier</param>
        /// <returns>User object with navigation properties if found and active</returns>
        Task<User?> GetUserByIdAsync(int id);

        /// <summary>
        /// Creates a new user with secure password hashing
        /// </summary>
        /// <param name="user">User object to create</param>
        /// <returns>Created user object with assigned ID</returns>
        /// <exception cref="ArgumentNullException">When user is null</exception>
        /// <exception cref="ArgumentException">When required fields are missing or invalid</exception>
        /// <exception cref="InvalidOperationException">When email already exists</exception>
        Task<User> CreateUserAsync(User user);

        /// <summary>
        /// Validates user credentials using secure password verification
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <param name="password">Plain text password to verify</param>
        /// <returns>True if credentials are valid and user is active</returns>
        Task<bool> ValidateUserAsync(string email, string password);

        /// <summary>
        /// Retrieves all active users
        /// </summary>
        /// <returns>Collection of active users ordered by name</returns>
        Task<IEnumerable<User>> GetAllUsersAsync();

        /// <summary>
        /// Updates user information (excluding password and email)
        /// </summary>
        /// <param name="user">User object with updated information</param>
        /// <returns>True if update was successful</returns>
        Task<bool> UpdateUserAsync(User user);

        /// <summary>
        /// Soft deletes a user by setting IsActive to false
        /// </summary>
        /// <param name="id">User's unique identifier</param>
        /// <returns>True if deletion was successful</returns>
        Task<bool> DeleteUserAsync(int id);

        /// <summary>
        /// Retrieves users by their role
        /// </summary>
        /// <param name="role">User role to filter by</param>
        /// <returns>Collection of users with the specified role</returns>
        Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role);

        /// <summary>
        /// Changes a user's password after verifying current password
        /// </summary>
        /// <param name="userId">User's unique identifier</param>
        /// <param name="currentPassword">Current password for verification</param>
        /// <param name="newPassword">New password to set</param>
        /// <returns>True if password change was successful</returns>
        /// <exception cref="ArgumentException">When new password doesn't meet requirements</exception>
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);

        /// <summary>
        /// Updates user's loyalty points balance
        /// </summary>
        /// <param name="userId">User's unique identifier</param>
        /// <param name="points">Points to add (positive) or subtract (negative)</param>
        /// <returns>True if update was successful</returns>
        Task<bool> UpdateLoyaltyPointsAsync(int userId, int points);

        /// <summary>
        /// Checks if an email address is available for registration
        /// </summary>
        /// <param name="email">Email address to check</param>
        /// <returns>True if email is available</returns>
        Task<bool> IsEmailAvailableAsync(string email);
    }
}