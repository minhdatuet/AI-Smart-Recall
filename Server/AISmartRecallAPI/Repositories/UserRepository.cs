using AISmartRecallAPI.Data;
using AISmartRecallAPI.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AISmartRecallAPI.Repositories
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(MongoDBContext context) : base(context, "users")
        {
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await FindOneAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await FindOneAsync(u => u.Username.ToLower() == username.ToLower());
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await ExistsAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<bool> UsernameExistsAsync(string username)
        {
            return await ExistsAsync(u => u.Username.ToLower() == username.ToLower());
        }

        public async Task<User?> AuthenticateAsync(string emailOrUsername, string passwordHash)
        {
            var filter = Builders<User>.Filter.Or(
                Builders<User>.Filter.Eq(u => u.Email, emailOrUsername.ToLower()),
                Builders<User>.Filter.Eq(u => u.Username, emailOrUsername.ToLower())
            );

            var user = await _collection.Find(filter).FirstOrDefaultAsync();
            
            if (user != null && user.PasswordHash == passwordHash)
            {
                return user;
            }

            return null;
        }

        public async Task<bool> UpdatePasswordAsync(ObjectId userId, string newPasswordHash)
        {
            var update = Builders<User>.Update.Set(u => u.PasswordHash, newPasswordHash);
            var result = await _collection.UpdateOneAsync(u => u.Id == userId, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateLastLoginAsync(ObjectId userId, DateTime lastLoginTime)
        {
            var update = Builders<User>.Update.Set(u => u.LastActive, lastLoginTime);
            var result = await _collection.UpdateOneAsync(u => u.Id == userId, update);
            return result.ModifiedCount > 0;
        }

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(string role)
        {
            // Role not defined in User model currently. Return empty set.
            return new List<User>();
        }

        public async Task<bool> ActivateUserAsync(ObjectId userId)
        {
            // No IsActive field. Update LastActive as a noop indicator.
            var update = Builders<User>.Update.Set(u => u.LastActive, DateTime.UtcNow);
            var result = await _collection.UpdateOneAsync(u => u.Id == userId, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeactivateUserAsync(ObjectId userId)
        {
            // No IsActive field. Perform no-op and return true.
            return true;
        }

        public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm, int page = 1, int pageSize = 20)
        {
            var filter = Builders<User>.Filter.Or(
                Builders<User>.Filter.Regex(u => u.Username, new BsonRegularExpression(searchTerm, "i")),
                Builders<User>.Filter.Regex(u => u.Email, new BsonRegularExpression(searchTerm, "i")),
                Builders<User>.Filter.Regex("profile.displayName", new BsonRegularExpression(searchTerm, "i"))
            );

            return await _collection
                .Find(filter)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .SortBy(u => u.Username)
                .ToListAsync();
        }

        public async Task<User?> GetUserWithProfileAsync(ObjectId userId)
        {
            // For now, just return the user. In future, you might want to include related profile data
            return await GetByIdAsync(userId);
        }
    }
}
