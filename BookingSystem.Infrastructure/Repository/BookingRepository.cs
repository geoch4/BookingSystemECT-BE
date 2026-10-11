// Backend purpose: Implements database access for BookingRepository.
using BookingSystem.Domain.Entities;
using BookingSystem.Infrastructure.IRepository;
using Microsoft.EntityFrameworkCore;

namespace BookingSystem.Infrastructure.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly AppDbContext _context;

        public BookingRepository(AppDbContext context)
        {
            _context = context;
        }
        // Retrieve one booking by its identifier.
        public async Task<Booking?> GetByIdAsync(int BookingId)
        {
            return await _context.Bookings
                .Include(b => b.Patient)
                .Include(b => b.CreatedBy)
                .FirstOrDefaultAsync(b => b.BookingId == BookingId);
        }
        // Retrieve every booking.
        public async Task<IEnumerable<Booking>> GetAllAsync()
        {
            return await _context.Bookings
                .Include(b => b.Patient)
                .Include(b => b.CreatedBy)
                .ToListAsync();
        }
        // Add a new booking and persist it in the same repository operation.
        public async Task AddAsync(Booking booking)
        {
            await _context.Bookings.AddAsync(booking);
            await _context.SaveChangesAsync();
        }
        // Persist changes made to an existing tracked or detached booking.
        public async Task UpdateAsync(Booking booking)
        {
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
        }

        // Remove a booking record and commit the deletion.
        public async Task DeleteAsync(Booking booking)
        {
            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
        }
    }
}
