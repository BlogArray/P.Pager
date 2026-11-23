using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace P.Pager.Tests
{
    /// <summary>
    /// Runs P.Pager against a real EF Core provider (SQLite in memory) to prove every query is translated to SQL.
    /// Client evaluation of a Where/OrderBy would throw, so these tests catch untranslatable expressions.
    /// </summary>
    [Collection(nameof(PagerSettingsCollection))]
    public sealed class EfCoreSqliteTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly TestDb _db;
        private readonly List<string> _sql = [];

        public EfCoreSqliteTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            _db = new TestDb(new DbContextOptionsBuilder<TestDb>()
                .UseSqlite(_connection)
                .LogTo(s => _sql.Add(s), [RelationalEventId.CommandExecuted])
                .ConfigureWarnings(w => w.Throw(RelationalEventId.MultipleCollectionIncludeWarning))
                .Options);
            _db.Database.EnsureCreated();
            _db.Members.AddRange(Enumerable.Range(1, 45).Select(i => new Member
            {
                Id = i,
                Name = $"name{i:D2}",
                Key = new Guid(i, 0, 0, new byte[8]),
                Created = new DateTime(2026, 1, 1).AddDays(i),
            }));
            _db.SaveChanges();
            _db.ChangeTracker.Clear();
            _sql.Clear();
        }

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
        }

        private string LastSql => _sql.Last();

        [Fact]
        public void ToPagerList_RunsCountAndPageInSql()
        {
            IPager<Member> page = _db.Members.OrderBy(m => m.Id).ToPagerList(3, 20);

            Assert.Equal(Enumerable.Range(41, 5), page.Select(m => m.Id));
            Assert.Equal(45, page.TotalItemCount);
            Assert.Contains(_sql, s => s.Contains("COUNT(*)"));
            Assert.Contains("LIMIT", LastSql);
            Assert.Contains("OFFSET", LastSql);
        }

        [Fact]
        public void ToPagerList_ClampsHugePage_WithoutNegativeOffset()
        {
            IPager<Member> page = _db.Members.OrderBy(m => m.Id).ToPagerList(int.MaxValue, 20);
            Assert.Equal(3, page.CurrentPageIndex);
            Assert.Equal(5, page.Count());
        }

        [Fact]
        public async Task ToPagerListAsync_WithEfCoreOperators()
        {
            using var cts = new CancellationTokenSource();
            IPager<Member> page = await _db.Members.OrderBy(m => m.Id).ToPagerListAsync(2, 20,
                (q, ct) => q.CountAsync(ct),
                (q, ct) => EntityFrameworkQueryableExtensions.ToListAsync(q, ct),
                cts.Token);

            Assert.Equal(Enumerable.Range(21, 20), page.Select(m => m.Id));
            Assert.Equal(45, page.TotalItemCount);
        }

        [Fact]
        public async Task ToPagerListAsync_LegacyOverload_StillWorks()
        {
            IPager<Member> page = await _db.Members.OrderBy(m => m.Id).ToPagerListAsync(2, 20);
            Assert.Equal(Enumerable.Range(21, 20), page.Select(m => m.Id));
        }

        [Fact]
        public void Keyset_Int_AscendingAndDescending()
        {
            KeysetPage<Member, int> first = _db.Members.ToKeysetPage(m => m.Id, 20);
            KeysetPage<Member, int> second = _db.Members.ToKeysetPage(m => m.Id, first.NextKey, 20);
            KeysetPage<Member, int> third = _db.Members.ToKeysetPage(m => m.Id, second.NextKey, 20);

            Assert.Equal(Enumerable.Range(1, 20), first.Items.Select(m => m.Id));
            Assert.Equal(Enumerable.Range(21, 20), second.Items.Select(m => m.Id));
            Assert.Equal(Enumerable.Range(41, 5), third.Items.Select(m => m.Id));
            Assert.False(third.HasNextPage);
            Assert.Matches(@"WHERE ""m""\.""Id"" > @", LastSql); // seek predicate in SQL, value sent as a parameter

            KeysetPage<Member, int> desc = _db.Members.ToKeysetPage(m => m.Id, 41, 3, descending: true);
            Assert.Equal(new[] { 40, 39, 38 }, desc.Items.Select(m => m.Id));
        }

        [Fact]
        public void Keyset_String_TranslatesCompareTo()
        {
            KeysetPage<Member, string> page = _db.Members.ToKeysetPage(m => m.Name, "name10", 3);
            Assert.Equal(new[] { 11, 12, 13 }, page.Items.Select(m => m.Id));
            Assert.Contains("WHERE", LastSql);
        }

        [Fact]
        public void Keyset_Guid_TranslatesCompareTo()
        {
            KeysetPage<Member, Guid> page = _db.Members.ToKeysetPage(m => m.Key, new Guid(10, 0, 0, new byte[8]), 3);
            Assert.Equal(new[] { 11, 12, 13 }, page.Items.Select(m => m.Id));
            Assert.Contains("WHERE", LastSql);
        }

        [Fact]
        public void Keyset_DateTime()
        {
            KeysetPage<Member, DateTime> page = _db.Members.ToKeysetPage(m => m.Created, new DateTime(2026, 1, 11), 3);
            Assert.Equal(new[] { 11, 12, 13 }, page.Items.Select(m => m.Id));
            Assert.Contains("WHERE", LastSql);
        }

        [Fact]
        public async Task Keyset_Async_WithEfCore()
        {
            KeysetPage<Member, int> page = await _db.Members.ToKeysetPageAsync(m => m.Id, 40, 20, (q, ct) => EntityFrameworkQueryableExtensions.ToListAsync(q, ct));
            Assert.Equal(Enumerable.Range(41, 5), page.Items.Select(m => m.Id));
            Assert.False(page.HasNextPage);
        }

        [Fact]
        public void UnorderedQuery_IsDetected_ForEfCore()
        {
            PagerSettings.UnorderedQueryBehavior = UnorderedQueryBehavior.Throw;
            try
            {
                Assert.Throws<InvalidOperationException>(() => _db.Members.ToPagerList(1, 10));
                Assert.Throws<InvalidOperationException>(() => _db.Members.Where(m => m.Id > 5).ToPagerList(1, 10));
                Assert.Equal(10, _db.Members.OrderBy(m => m.Name).ThenBy(m => m.Id).ToPagerList(1, 10).Count());
            }
            finally
            {
                PagerSettings.UnorderedQueryBehavior = UnorderedQueryBehavior.Ignore;
            }
        }

        [Fact]
        public void ToPagedResult_FromEfCore()
        {
            PagedResult<Member> result = _db.Members.OrderBy(m => m.Id).ToPagerList(1, 10).ToPagedResult();
            Assert.Equal(10, result.Items.Count);
            Assert.Equal(5, result.Metadata.TotalPageCount);
        }

        private static readonly SortMap<Member> MemberSorts = new SortMap<Member>()
            .Add("name", m => m.Name)
            .Add("created", m => m.Created)
            .Default("-created")
            .ThenBy(m => m.Id);

        [Fact]
        public void ApplySort_TranslatesToOrderByInSql()
        {
            IPager<Member> page = _db.Members.ApplySort("-name", MemberSorts).ToPagerList(1, 3);
            Assert.Equal(new[] { 45, 44, 43 }, page.Select(m => m.Id));
            Assert.Matches(@"ORDER BY ""m""\.""Name"" DESC, ""m""\.""Id""", LastSql);
        }

        [Fact]
        public void ApplySort_InvalidField_UsesDefault()
        {
            IPager<Member> page = _db.Members.ApplySort("Secret; DROP TABLE Members", MemberSorts).ToPagerList(1, 2);
            Assert.Equal(new[] { 45, 44 }, page.Select(m => m.Id));
            Assert.Matches(@"ORDER BY ""m""\.""Created"" DESC, ""m""\.""Id""", LastSql);
        }

        [Fact]
        public async Task Slice_RunsNoCount_AndFetchesOneExtraRow()
        {
            ISlice<Member> slice = await _db.Members.OrderBy(m => m.Id).ToSliceAsync(2, 20,
                (q, ct) => EntityFrameworkQueryableExtensions.ToListAsync(q, ct));

            Assert.Equal(Enumerable.Range(21, 20), slice.Select(m => m.Id));
            Assert.True(slice.HasNextPage);
            Assert.DoesNotContain(_sql, s => s.Contains("COUNT("));
            Assert.Single(_sql);
        }

        [Fact]
        public void KnownTotal_RunsNoCount()
        {
            IPager<Member> page = _db.Members.OrderBy(m => m.Id).ToPagerList(3, 20, totalItemCount: 45);
            Assert.Equal(Enumerable.Range(41, 5), page.Select(m => m.Id));
            Assert.DoesNotContain(_sql, s => s.Contains("COUNT("));
        }

        [Fact]
        public async Task PageRequest_WithEfCoreAsync_AndMap()
        {
            var request = new PageRequest { Page = 2, PageSize = 500, Sort = "name" };
            IPager<string> names = (await _db.Members.ApplySort(request.Sort, MemberSorts).ToPagerListAsync(request,
                    (q, ct) => q.CountAsync(ct),
                    (q, ct) => EntityFrameworkQueryableExtensions.ToListAsync(q, ct),
                    maxPageSize: 20))
                .Map(m => m.Name);

            Assert.Equal(20, names.PageSize);
            Assert.Equal("name21", names.First());
            Assert.Equal(3, names.TotalPageCount);
        }

        public sealed class Member
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public Guid Key { get; set; }
            public DateTime Created { get; set; }
        }

        private sealed class TestDb(DbContextOptions<TestDb> options) : DbContext(options)
        {
            public DbSet<Member> Members => Set<Member>();

            protected override void OnModelCreating(ModelBuilder modelBuilder) =>
                modelBuilder.Entity<Member>().Property(m => m.Id).ValueGeneratedNever();
        }
    }
}
