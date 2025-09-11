using AISmartRecallAPI.Models;
using MongoDB.Bson;

namespace AISmartRecallAPI.Repositories
{
    public interface IUserRepository : IBaseRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUsernameAsync(string username);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> UsernameExistsAsync(string username);
        Task<User?> AuthenticateAsync(string emailOrUsername, string passwordHash);
        Task<bool> UpdatePasswordAsync(ObjectId userId, string newPasswordHash);
        Task<bool> UpdateLastLoginAsync(ObjectId userId, DateTime lastLoginTime);
        Task<IEnumerable<User>> GetUsersByRoleAsync(string role);
        Task<bool> ActivateUserAsync(ObjectId userId);
        Task<bool> DeactivateUserAsync(ObjectId userId);
        Task<IEnumerable<User>> SearchUsersAsync(string searchTerm, int page = 1, int pageSize = 20);
        Task<User?> GetUserWithProfileAsync(ObjectId userId);
    }
}
