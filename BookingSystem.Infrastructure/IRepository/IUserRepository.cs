// Backend purpose: Defines the persistence contract for IUserRepository.
using BookingSystem.Domain.Entities;

namespace BookingSystem.Infrastructure.IRepository
{
    public interface IUserRepository
    {
        Task AddAsync(User user);
        Task<User?> GetByIdAsync(int id);
        Task<IEnumerable<User>> GetAllAsync();
// Persistence operations specific to users.
        Task<User?> GetByEmailAsync(string email);
        Task UpdateAsync(User user);  // Update an existing user.
        Task DeleteAsync(User user);  // Remove an existing user.
    }
}
