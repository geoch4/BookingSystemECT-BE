// Backend purpose: Generates development-only sample records for UserFaker.
using Bogus;
using BookingSystem.Domain.Entities;

namespace BookingSystem.Infrastructure.Fakers
{
    public class UserFaker
    {
        public Faker<User> GenerateUser()
        {
            var faker = new Faker<User>()
                .RuleFor(u => u.Fullname, f => f.Name.FullName())
                .RuleFor(u => u.Email, f => f.Internet.Email())
                .RuleFor(u => u.PasswordHash, f => f.Internet.Password())
                .RuleFor(u => u.RoleId, f => f.PickRandom(1, 2, 3, 4, 5, 6));  // Select one of the seeded role identifiers.

            return faker;
        }
    }
}
