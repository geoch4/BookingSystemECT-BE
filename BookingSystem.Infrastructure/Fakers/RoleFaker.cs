// Backend purpose: Generates development-only sample records for RoleFaker.
using Bogus;
using BookingSystem.Domain.Entities;
using System.Collections.Generic;
using System.Linq;
namespace BookingSystem.Infrastructure.Fakers
{
    public class RoleFaker
    {
        public Faker<Role> GenerateRole()
        {

            var roleNames = new List<string>
            {
                "Admin",
                "Sjuksköterska",
                "ECTSjuksköterska",
                "Överläkare",
                "Underläkare",
                "Specialistläkare"
            };

            // Cycle through the fixed role definitions instead of generating random role names.
            var faker = new Faker<Role>()
                .RuleFor(r => r.RoleName, (f, r) => roleNames[f.IndexFaker % roleNames.Count])
                .RuleFor(r => r.Users, f => new List<User>());


            return faker;
        }
    }
}