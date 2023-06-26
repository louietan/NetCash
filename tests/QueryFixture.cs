namespace NetCash.Tests;

using Xunit;

public class QueryFixture
{
    [Theory]
    [SetupTestingBook(CopyPremade = "common")]
    public void Query_Should_Work(TestingBook testingBook)
    {
        using (var book = Book.Open(testingBook))
        {
            book.NewTransaction(currency: NetCashExtensions.TestingCurrency)
                .SetDescription("test description")
                .AddSplit(account: book.FindAccountByName("Books"), value: 100, memo: "test memo book")
                .AddSplit(account: book.FindAccountByName("Hobbies"), value: 100, memo: "test Memo hobbies")
                .AddSplit(account: book.FindAccountByName("Phone"), value: 100, memo: "test mEmo phone")
                .AddSplit(account: book.FindAccountByName("Groceries"), value: 100, memo: "test meMo groceries")
                .AddSplit(account: book.FindAccountByName("Water"), value: 100, memo: "test memO water")
                .AddBalancingSplit(account: book.FindAccountByName("Credit Card"))
                .Save();
        }

        using (var book = Book.OpenRead(testingBook))
        {
            Assert.AccountsShouldBalance(book);

            var query = new SplitQuery(book);

            var splits = query
                .MatchMemo("memo")
                .Run();

            Assert.Equal(5, splits.Count);

            splits = query
                .NewSubquery()
                .MatchDescription("description")
                .Run();

            Assert.Equal(5, splits.Count);

            splits = query
                .Reset()
                .IncludeBooks(book)
                .MatchDescription("description")
                .Run();

            Assert.Equal(6, splits.Count);

            splits = query
                .Reset()
                .IncludeBooks(book)
                .MatchMemo("water|groceries", useRegex: true)
                .Invert()
                .MatchDescription("test")
                .Run();

            Assert.Equal(4, splits.Count);
        }
    }
}
