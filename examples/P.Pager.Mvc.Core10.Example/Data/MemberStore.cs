namespace P.Pager.Mvc.Core10.Example.Data;

/// <summary>A member shown in the demo tables.</summary>
public sealed record Member(int Id, string Name, string Email, string City, DateOnly Joined);

/// <summary>
/// In-memory sample data. In a real app this would be a database query, for example an EF Core <c>DbSet</c>:
/// the paging calls are the same because they work on any <see cref="IQueryable{T}"/>.
/// </summary>
public sealed class MemberStore
{
    private static readonly string[] FirstNames = ["Ava", "Liam", "Noah", "Emma", "Mia", "Arjun", "Priya", "Kenji", "Sofia", "Lucas", "Zara", "Omar", "Elena", "Ravi", "Chloe"];
    private static readonly string[] LastNames = ["Smith", "Garcia", "Patel", "Kim", "Müller", "Rossi", "Silva", "Khan", "Nguyen", "Brown", "Ito", "Reddy"];
    private static readonly string[] Cities = ["London", "Hyderabad", "Berlin", "Tokyo", "São Paulo", "Toronto", "Sydney", "Madrid", "Chennai", "Seattle"];

    private readonly List<Member> _members;

    public MemberStore()
    {
        var random = new Random(42); // fixed seed: the same data on every run
        var start = new DateOnly(2020, 1, 1);
        _members = Enumerable.Range(1, 237).Select(id =>
        {
            string first = FirstNames[random.Next(FirstNames.Length)];
            string last = LastNames[random.Next(LastNames.Length)];
            return new Member(id, $"{first} {last}", $"{first}.{last}{id}@example.com".ToLowerInvariant(),
                Cities[random.Next(Cities.Length)], start.AddDays(random.Next(0, 2000)));
        }).ToList();
    }

    /// <summary>All members as a query, like a database table.</summary>
    public IQueryable<Member> Members => _members.AsQueryable();
}
