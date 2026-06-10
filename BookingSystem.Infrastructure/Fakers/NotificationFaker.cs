// Backend purpose: Generates development-only sample records for NotificationFaker.
using Bogus;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Infrastructure.Fakers
{
    public class NotificationFaker
    {
        public Faker<Notification> GenerateNotification(IEnumerable<User> users)
        {
            var faker = new Faker<Notification>()
                .RuleFor(n => n.Message, f => f.Lorem.Sentence()) // Generate a random message.
                .RuleFor(n => n.SentAt, f => f.Date.Recent()) // Generate a recent timestamp.
                .RuleFor(n => n.RecipientId, f => f.PickRandom(users).UserId); // Assign an existing user as the recipient.

            return faker;
        }
    }
}
