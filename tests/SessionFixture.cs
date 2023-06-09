namespace NetCash.Tests;

using System;
using System.Linq;

using Xunit;

public class SessionFixture
{
    [Theory]
    [SetupTestingBook]
    public void Should_Return_Error_If_Book_Does_Not_Exist(TestingBook testingBook)
    {
        var exception = Assert.Throws<GnuCashBackendException>(() => Book.OpenRead(testingBook));
        Assert.BackendErrorCode(Bindings.QofBackendError.ERR_FILEIO_FILE_NOT_FOUND, exception);
    }

    [Theory]
    [SetupTestingBook(BookName = "simple", Copy = true)]
    public void Should_Lock_And_Unlock_Properly(TestingBook testingBook)
    {
        Book openBook() => Book.Open(testingBook);

        for (var i = 0; i < 10; ++i)
        {
            using var book = openBook();
            var exception = Assert.Throws<GnuCashBackendException>(openBook);
            Assert.BackendErrorCode(Bindings.QofBackendError.ERR_BACKEND_LOCKED, exception);
        }
    }

    [Theory]
    [SetupTestingBook(BookName = "simple")]
    public void Can_Get_Currently_Opened_Book(TestingBook testingBook)
    {
        Book openBook() => Book.OpenRead(testingBook);

        Assert.Null(Book.Current);
        Assert.False(Bindings.gnc_current_session_exist());

        using (var book1 = openBook())
        {
            Assert.Equal(book1, Book.Current);
            Assert.Equal(Bindings.gnc_get_current_session(), book1.SessionHandle);

            using (var book2 = openBook())
            {
                Assert.Equal(book2, Book.Current);
                Assert.Equal(Bindings.gnc_get_current_session(), book2.SessionHandle);

                using (var book3 = openBook())
                {
                    Assert.Equal(book3, Book.Current);
                    Assert.Equal(Bindings.gnc_get_current_session(), book3.SessionHandle);
                }

                Assert.Equal(book2, Book.Current);
                Assert.Equal(Bindings.gnc_get_current_session(), book2.SessionHandle);
            }

            Assert.Equal(book1, Book.Current);
            Assert.Equal(Bindings.gnc_get_current_session(), book1.SessionHandle);
        }

        Assert.Null(Book.Current);
        Assert.False(Bindings.gnc_current_session_exist());
    }
}
