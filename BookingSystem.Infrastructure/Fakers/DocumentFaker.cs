// Backend purpose: Generates development-only sample records for DocumentFaker.
using Bogus;
using BookingSystem.Domain.Entities;

public class DocumentFaker
{
    public Faker<Document> GenerateDocument(IEnumerable<User> users)
    {
        var faker = new Faker<Document>()
            .RuleFor(d => d.FileName, f => f.System.FileName())
            .RuleFor(d => d.Verified, f => f.Random.Bool())
            .RuleFor(d => d.UploadedByUserId, f => f.PickRandom(users).UserId);  // Assign an existing user as the uploader.

        return faker;
    }
}
