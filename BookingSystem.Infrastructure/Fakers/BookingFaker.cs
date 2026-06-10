// Backend purpose: Generates development-only sample records for BookingFaker.
using Bogus;
using BookingSystem.Domain.Entities;
using BookingSystem.Domain.Enums;

namespace BookingSystem.Infrastructure.Fakers
{
    public class BookingFaker
    {
        public Faker<Booking> GenerateBooking(IEnumerable<Patient> patients, IEnumerable<User> users)
        {
            var faker = new Faker<Booking>()
                .RuleFor(b => b.Date, f => f.Date.Future())
                .RuleFor(b => b.PatientId, f => f.PickRandom(patients).PatientId)
                .RuleFor(b => b.TreatmentType, f => f.PickRandom<TreatmentType>())
                .RuleFor(b => b.CreatedByUserId, f => f.PickRandom(users).UserId)
                .RuleFor(b => b.CreatedAt, f => f.Date.Recent())
                .RuleFor(b => b.Priority, f => f.PickRandom<BookingPriority>())
                .RuleFor(b => b.Status, f => f.PickRandom<BookingStatus>());

            return faker;
        }
    }
}
